using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ApiContabsv.Services
{
    public class PayPalService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _secret;
        private readonly string _baseUrl;
        private string _accessToken;
        private DateTime _tokenExpiry;

        public PayPalService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _clientId = configuration["PayPal:ClientId"];
            _secret = configuration["PayPal:Secret"];

            // Sandbox o Live
            var mode = configuration["PayPal:Mode"] ?? "sandbox";
            _baseUrl = mode == "live"
                ? "https://api-m.paypal.com"
                : "https://api-m.sandbox.paypal.com";
        }

        /// <summary>
        /// Obtener token de acceso de PayPal
        /// </summary>
        private async Task<string> GetAccessToken()
        {
            // Reutilizar token si aún es válido
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_secret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

            _accessToken = tokenData.GetProperty("access_token").GetString();
            var expiresIn = tokenData.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Renovar 1 min antes

            return _accessToken;
        }

        /// <summary>
        /// Crear una orden de pago en PayPal
        /// </summary>
        /// <param name="monto">Monto total a cobrar</param>
        /// <param name="descripcion">Descripción del pago</param>
        /// <param name="referencia">Referencia interna (idSuscripcion)</param>
        /// <param name="returnUrl">URL de retorno cuando el pago es aprobado</param>
        /// <param name="cancelUrl">URL de retorno cuando el pago es cancelado</param>
        /// <returns>Order ID y Approval URL</returns>
        public async Task<PayPalOrderResult> CreateOrder(decimal monto, string descripcion, string referencia, string returnUrl, string cancelUrl)
        {
            var token = await GetAccessToken();

            var orderBody = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = referencia,
                        description = descripcion,
                        amount = new
                        {
                            currency_code = "USD",
                            value = monto.ToString("F2")
                        }
                    }
                },
                payment_source = new
                {
                    paypal = new
                    {
                        experience_context = new
                        {
                            payment_method_preference = "UNRESTRICTED",
                            brand_name = "ContabSV",
                            locale = "es-SV",
                            landing_page = "LOGIN",
                            user_action = "PAY_NOW",
                            return_url = returnUrl,
                            cancel_url = cancelUrl
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(orderBody);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new PayPalOrderResult
                {
                    Success = false,
                    Error = $"PayPal Error: {response.StatusCode} - {responseJson}"
                };
            }

            var orderData = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var orderId = orderData.GetProperty("id").GetString();

            // Buscar la URL de aprobación
            string approvalUrl = null;
            var links = orderData.GetProperty("links");
            foreach (var link in links.EnumerateArray())
            {
                if (link.GetProperty("rel").GetString() == "payer-action")
                {
                    approvalUrl = link.GetProperty("href").GetString();
                    break;
                }
            }

            return new PayPalOrderResult
            {
                Success = true,
                OrderId = orderId,
                ApprovalUrl = approvalUrl
            };
        }

        /// <summary>
        /// Capturar el pago después de que el usuario aprueba
        /// </summary>
        /// <param name="orderId">ID de la orden de PayPal</param>
        /// <returns>Resultado de la captura</returns>
        public async Task<PayPalCaptureResult> CaptureOrder(string orderId)
        {
            var token = await GetAccessToken();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders/{orderId}/capture");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new PayPalCaptureResult
                {
                    Success = false,
                    Error = $"PayPal Capture Error: {response.StatusCode} - {responseJson}"
                };
            }

            var captureData = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var status = captureData.GetProperty("status").GetString();

            string paymentId = null;
            string payerId = null;

            try
            {
                var captures = captureData.GetProperty("purchase_units")[0]
                    .GetProperty("payments")
                    .GetProperty("captures")[0];
                paymentId = captures.GetProperty("id").GetString();

                var payer = captureData.GetProperty("payer");
                payerId = payer.GetProperty("payer_id").GetString();
            }
            catch { }

            return new PayPalCaptureResult
            {
                Success = status == "COMPLETED",
                Status = status,
                PaymentId = paymentId,
                PayerId = payerId,
                RawResponse = responseJson
            };
        }

        /// <summary>
        /// Verificar estado de una orden
        /// </summary>
        public async Task<string> GetOrderStatus(string orderId)
        {
            var token = await GetAccessToken();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v2/checkout/orders/{orderId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var orderData = JsonSerializer.Deserialize<JsonElement>(responseJson);
            return orderData.GetProperty("status").GetString();
        }
    }

    // Modelos de resultado
    public class PayPalOrderResult
    {
        public bool Success { get; set; }
        public string OrderId { get; set; }
        public string ApprovalUrl { get; set; }
        public string Error { get; set; }
    }

    public class PayPalCaptureResult
    {
        public bool Success { get; set; }
        public string Status { get; set; }
        public string PaymentId { get; set; }
        public string PayerId { get; set; }
        public string RawResponse { get; set; }
        public string Error { get; set; }
    }
}
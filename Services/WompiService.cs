using ApiContabsv.DTO.DB_ContabsvDTO;
using System.Text;
using System.Text.Json;

namespace ApiContabsv.Services
{
    public class WompiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl;
        private string _accessToken;
        private DateTime _tokenExpiration;

        public WompiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _clientId = configuration["Wompi:AppId"];
            _clientSecret = configuration["Wompi:ApiSecret"];
            _baseUrl = configuration["Wompi:BaseUrl"] ?? "https://api.wompi.sv";
        }

        /// <summary>
        /// Obtener access token de OAuth 2.0
        /// </summary>
        private async Task<string> GetAccessToken()
        {
            // Si ya tenemos token válido, retornarlo
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.Now < _tokenExpiration)
            {
                Console.WriteLine("Usando token en caché");
                return _accessToken;
            }

            try
            {
                Console.WriteLine(" Obteniendo nuevo token de Wompi...");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://id.wompi.sv/connect/token");

                var formData = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "audience", "wompi_api" }
                };

                request.Content = new FormUrlEncodedContent(formData);

                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error obteniendo token: {response.StatusCode} - {responseJson}");
                }

                var data = JsonSerializer.Deserialize<JsonElement>(responseJson);
                _accessToken = data.GetProperty("access_token").GetString();
                var expiresIn = data.GetProperty("expires_in").GetInt32();
                _tokenExpiration = DateTime.Now.AddSeconds(expiresIn - 60); // Renovar 60 segundos antes
                return _accessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error obteniendo token: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crear enlace de pago en Wompi
        /// </summary>
        public async Task<WompiTransactionResult> CreateTransaction(decimal amount, string description, string customerEmail, string redirectUrl)
        {
            try
            {
                // Obtener token de autenticación
                var token = await GetAccessToken();

                var requestBody = new
                {
                    identificadorEnlaceComercio = $"CONTABSV-{DateTime.Now.Ticks}",
                    monto = amount,
                    nombreProducto = description,
                    formaPago = new
                    {
                        permitirTarjetaCreditoDebido = true,
                        permitirPagoConPuntoAgricola = false,
                        permitirPagoEnCuotasAgricola = false
                    },
                    configuracion = new
                    {
                        urlRedirect = $"{redirectUrl}?idEnlace={{idEnlace}}", 
                        esMontoEditable = false,
                        esCantidadEditable = false,
                        cantidadPorDefecto = 1,
                        notificarTransaccionCliente = true,
                        emailsNotificacion = "corenadeveloper@gmail.com",
                        urlWebhook = "https://api.contabsv.com/DBContabsv_Wompi/Webhook"
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/EnlacePago");
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new WompiTransactionResult
                    {
                        Success = false,
                        Error = $"Wompi Error: {response.StatusCode} - {responseJson}"
                    };
                }

                var data = JsonSerializer.Deserialize<JsonElement>(responseJson);

                return new WompiTransactionResult
                {
                    Success = true,
                    TransactionId = data.GetProperty("idEnlace").GetInt32().ToString(),
                    CheckoutUrl = data.GetProperty("urlEnlace").GetString()
                };
            }
            catch (Exception ex)
            {
                return new WompiTransactionResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Verificar estado de transacción en Wompi
        /// </summary>
        public async Task<WompiTransactionStatus> GetTransactionStatus(string transactionId)
        {
            try
            {
                var token = await GetAccessToken();

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/EnlacePago/{transactionId}");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new WompiTransactionStatus
                    {
                        Success = false,
                        Status = "ERROR"
                    };
                }

                var data = JsonSerializer.Deserialize<JsonElement>(responseJson);
                var status = data.TryGetProperty("estaProductivo", out var prod) && prod.GetBoolean() ? "APPROVED" : "PENDING";

                return new WompiTransactionStatus
                {
                    Success = true,
                    Status = status,
                    TransactionId = transactionId,
                    RawResponse = responseJson
                };
            }
            catch (Exception ex)
            {
                return new WompiTransactionStatus
                {
                    Success = false,
                    Status = "ERROR"
                };
            }
        }
    }
}
using ApiContabsv.DTO.DB_ContabsvDTO;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ApiContabsv.Services
{
    public class WompiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _appId;
        private readonly string _apiSecret;
        private readonly string _baseUrl;

        public WompiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _appId = configuration["Wompi:AppId"];
            _apiSecret = configuration["Wompi:ApiSecret"];
            _baseUrl = configuration["Wompi:BaseUrl"] ?? "https://api.wompi.sv";
        }

        /// <summary>
        /// Crear transacción en Wompi
        /// </summary>
        public async Task<WompiTransactionResult> CreateTransaction(decimal amount, string description, string customerEmail, string redirectUrl)
        {
            try
            {
                var requestBody = new
                {
                    appId = _appId,
                    amount = amount.ToString("F2"),
                    currency = "USD",
                    description = description,
                    customer = new
                    {
                        email = customerEmail
                    },
                    redirectUrl = redirectUrl
                };

                var json = JsonSerializer.Serialize(requestBody);
                Console.WriteLine("📤 Request a Wompi:");
                Console.WriteLine(json);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/transactions");
                request.Headers.Add("Authorization", $"Bearer {_apiSecret}");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                Console.WriteLine("📥 Response de Wompi:");
                Console.WriteLine(responseJson);

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
                    TransactionId = data.GetProperty("data").GetProperty("id").GetString(),
                    CheckoutUrl = data.GetProperty("data").GetProperty("url").GetString()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en Wompi: {ex.Message}");
                return new WompiTransactionResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Verificar estado de transacción
        /// </summary>
        public async Task<WompiTransactionStatus> GetTransactionStatus(string transactionId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v1/transactions/{transactionId}");
                request.Headers.Add("Authorization", $"Bearer {_apiSecret}");

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
                var status = data.GetProperty("data").GetProperty("status").GetString();

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
                Console.WriteLine($"❌ Error verificando estado: {ex.Message}");
                return new WompiTransactionStatus
                {
                    Success = false,
                    Status = "ERROR"
                };
            }
        }
    }


}
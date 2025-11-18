using System.Text;
using System.Text.Json;

namespace ApiContabsv.Services
{
    public interface IHaciendaService
    {
        Task<HaciendaTransmissionResult> TransmitDocument(string signedJWT, string userNit, string ambiente);
        Task<HaciendaAuthResult> AuthenticateUser(string userNit, string userPassword, string ambiente);
    }

    public class HaciendaService : IHaciendaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HaciendaService> _logger;

        public HaciendaService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<HaciendaService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HaciendaTransmissionResult> TransmitDocument(string signedJWT, string userNit, string ambiente)
        {
            try
            {
                // 1. OBTENER TOKEN DE HACIENDA (necesitas implementar autenticación)
                var authResult = await AuthenticateUser(userNit, "", ambiente); // Password desde BD
                if (!authResult.Success)
                {
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Error = "Error de autenticación con Hacienda",
                        ErrorDetails = authResult.Error
                    };
                }

                // 2. ENVIAR DOCUMENTO FIRMADO A HACIENDA
                var receptionUrl = GetHaciendaUrl("ReceptionUrl", ambiente);
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // 3. PREPARAR REQUEST PARA HACIENDA
                var haciendaRequest = new
                {
                    ambiente = ambiente,
                    idEnvio = 1,
                    version = 1,
                    tipoDte = "01", // Extraer del JWT si es necesario
                    documento = signedJWT
                };

                var jsonContent = JsonSerializer.Serialize(haciendaRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 4. AGREGAR HEADERS DE AUTORIZACIÓN
                httpClient.DefaultRequestHeaders.Add("Authorization", authResult.Token);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "API-DTE-SV/1.0");

                // 5. ENVIAR A HACIENDA
                var response = await httpClient.PostAsync(receptionUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Respuesta de Hacienda: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var haciendaResponse = JsonSerializer.Deserialize<HaciendaResponse>(responseContent);

                    return new HaciendaTransmissionResult
                    {
                        Success = true,
                        Status = haciendaResponse?.Estado ?? "PROCESADO",
                        ReceptionStamp = haciendaResponse?.SelloRecibido,
                        ResponseCode = haciendaResponse?.CodigoMsg,
                        Message = haciendaResponse?.Descripcion,
                        RawResponse = responseContent
                    };
                }
                else
                {
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Error = $"Error HTTP {response.StatusCode}",
                        ErrorDetails = responseContent
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transmitiendo documento a Hacienda para NIT: {NIT}", userNit);

                return new HaciendaTransmissionResult
                {
                    Success = false,
                    Error = "Error interno de transmisión",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<HaciendaAuthResult> AuthenticateUser(string userNit, string userPassword, string ambiente)
        {
            try
            {
                var authUrl = GetHaciendaUrl("AuthUrl", ambiente);
                var httpClient = _httpClientFactory.CreateClient();

                var authRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("user", userNit),
                    new KeyValuePair<string, string>("pwd", userPassword)
                });

                var response = await httpClient.PostAsync(authUrl, authRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<HaciendaAuthResponse>(responseContent);

                    return new HaciendaAuthResult
                    {
                        Success = true,
                        Token = authResponse?.Body?.Token ?? "",
                        TokenType = authResponse?.Body?.TokenType ?? ""
                    };
                }

                return new HaciendaAuthResult
                {
                    Success = false,
                    Error = $"Error de autenticación: {response.StatusCode}",
                    ErrorDetails = responseContent
                };
            }
            catch (Exception ex)
            {
                return new HaciendaAuthResult
                {
                    Success = false,
                    Error = "Error interno de autenticación",
                    ErrorDetails = ex.Message
                };
            }
        }

        private string GetHaciendaUrl(string endpoint, string ambiente)
        {
            var haciendaSettings = _configuration.GetSection("HaciendaSettings");
            var urlSection = ambiente == "00" ? "TestingUrls" : "ProductionUrls";

            return haciendaSettings.GetValue<string>($"{urlSection}:{endpoint}") ?? "";
        }
    }

    // DTOs para respuestas
    public class HaciendaTransmissionResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? ReceptionStamp { get; set; }
        public string? ResponseCode { get; set; }
        public string? Message { get; set; }
        public string? RawResponse { get; set; }
        public string? Error { get; set; }
        public string? ErrorDetails { get; set; }
    }

    public class HaciendaAuthResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? TokenType { get; set; }
        public string? Error { get; set; }
        public string? ErrorDetails { get; set; }
    }

    // Modelos para respuestas de Hacienda
    public class HaciendaResponse
    {
        public string? Estado { get; set; }
        public string? SelloRecibido { get; set; }
        public string? CodigoMsg { get; set; }
        public string? Descripcion { get; set; }
    }

    public class HaciendaAuthResponse
    {
        public string? Status { get; set; }
        public HaciendaAuthBody? Body { get; set; }
    }

    public class HaciendaAuthBody
    {
        public string? Token { get; set; }
        public string? TokenType { get; set; }
    }
}
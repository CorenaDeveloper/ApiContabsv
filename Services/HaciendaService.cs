using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Dte;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiContabsv.Services
{
    public interface IHaciendaService
    {
        Task<HaciendaTransmissionResult> TransmitDocument(string signedJWT, string userNit, string ambiente, string documentType = "01");
        Task<HaciendaAuthResult> AuthenticateUser(string userHacienda, string userPassword, string ambiente);
    }

    public class HaciendaService : IHaciendaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HaciendaService> _logger;
        private readonly dteContext _context;

        public HaciendaService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<HaciendaService> logger, dteContext context)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        private async Task<HaciendaUserCredentials?> GetUserCredentials(string userNit)
        {
            try
            {
         
                var user = await _context.Users
                    .Where(u => u.Nit == userNit)
                    .Select(u => new HaciendaUserCredentials
                    {
                        UserHacienda = u.Nit,
                        PassHacienda = u.JwtSecret // El passHacienda es la contraseña de la api generada 
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return null;
                }

                var hasCredentials = !string.IsNullOrEmpty(user.UserHacienda) && !string.IsNullOrEmpty(user.PassHacienda);

                return user;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<HaciendaTransmissionResult> TransmitDocument(string signedJWT, string userNit, string ambiente, string documentType)
        {
            try
            {
                // 1. OBTENER CREDENCIALES DEL USUARIO DESDE BD
                var user = await GetUserCredentials(userNit);
                if (user == null || string.IsNullOrEmpty(user.UserHacienda) || string.IsNullOrEmpty(user.PassHacienda))
                {
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Error = "Usuario no encontrado o sin credenciales de Hacienda",
                        ErrorDetails = $"NIT: {userNit} no tiene user_hacienda/pass_hacienda configurados"
                    };
                }

                // 2. OBTENER TOKEN DE HACIENDA
                var token = await GetOrRefreshHaciendaToken(userNit, ambiente);
                if (string.IsNullOrEmpty(token))
                {
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Error = "No se pudo obtener token de Hacienda"
                    };
                }

                // 3. ENVIAR DOCUMENTO FIRMADO A HACIENDA
                var receptionUrl = GetHaciendaUrl("ReceptionUrl", ambiente);

                if (string.IsNullOrEmpty(receptionUrl))
                {
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Error = "URL de recepción no configurada",
                        ErrorDetails = $"Ambiente: {ambiente}"
                    };
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // 4. PREPARAR REQUEST PARA HACIENDA (FORMATO IGUAL A GO)
                var haciendaRequest = new
                {
                    ambiente = ambiente,
                    idEnvio = 1,
                    version = 1,
                    tipoDte = documentType,
                    documento = signedJWT
                };

                var jsonContent = JsonSerializer.Serialize(haciendaRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 5. AGREGAR HEADERS DE AUTORIZACIÓN
                httpClient.DefaultRequestHeaders.Add("Authorization", token);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "API-DTE-SV/1.0");

                // 6. ENVIAR A HACIENDA
                var response = await httpClient.PostAsync(receptionUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Intentar parsear la respuesta JSON independientemente del código HTTP
                HaciendaResponse? haciendaResponse = null;
                try
                {
                    haciendaResponse = JsonSerializer.Deserialize<HaciendaResponse>(responseContent);
                }
                catch (JsonException)
                {
                    // Si no se puede parsear como JSON, es un error de transmisión
                }

                // LÓGICA CORREGIDA BASADA EN GO:
                if (haciendaResponse != null && !string.IsNullOrEmpty(haciendaResponse.Estado))
                {
                    // Tenemos una respuesta válida de Hacienda, procesarla según el estado
                    bool isProcessed = haciendaResponse.Estado == "PROCESADO";
                    bool isRejected = haciendaResponse.Estado == "RECHAZADO";

                    return new HaciendaTransmissionResult
                    {
                        Success = isProcessed, 
                        Status = haciendaResponse.Estado, 
                        ReceptionStamp = haciendaResponse.SelloRecibido, 
                        ResponseCode = haciendaResponse.CodigoMsg, 
                        Message = haciendaResponse.Descripcion,
                        Error = isRejected ? $"Documento rechazado por Hacienda: {haciendaResponse.Descripcion}" : null,
                        ErrorDetails = isRejected ? string.Join("; ", haciendaResponse.Observaciones ?? Array.Empty<string>()) : null,
                        RawResponse = responseContent
                    };
                }
                else
                {
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Status = null, 
                        Error = response.IsSuccessStatusCode ? "Respuesta inválida de Hacienda" : $"Error HTTP {response.StatusCode}",
                        ErrorDetails = responseContent,
                        RawResponse = responseContent
                    };
                }
            }
            catch (HttpRequestException httpEx)
            {
                return new HaciendaTransmissionResult
                {
                    Success = false,
                    Error = "Error de conexión con Hacienda",
                    ErrorDetails = httpEx.Message
                };
            }
            catch (TaskCanceledException timeoutEx)
            {
                return new HaciendaTransmissionResult
                {
                    Success = false,
                    Error = "Timeout conectando con Hacienda",
                    ErrorDetails = timeoutEx.Message
                };
            }
            catch (Exception ex)
            {
                return new HaciendaTransmissionResult
                {
                    Success = false,
                    Error = "Error interno de transmisión",
                    ErrorDetails = ex.Message
                };
            }
        }


        public async Task<HaciendaAuthResult> AuthenticateUser(string userHacienda, string userPassword, string ambiente)
        {
            try
            {
                var authUrl = GetHaciendaUrl("AuthUrl", ambiente);
                var httpClient = _httpClientFactory.CreateClient();

                var authRequest = new FormUrlEncodedContent(new[]
                {
                   new KeyValuePair<string, string>("user", userHacienda),
                   new KeyValuePair<string, string>("pwd", userPassword)
                });

                httpClient.DefaultRequestHeaders.Add("User-Agent", "HaciendaApp/1.0");

                var response = await httpClient.PostAsync(authUrl, authRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {

                    using var jsonDoc = JsonDocument.Parse(responseContent);

                    if (jsonDoc.RootElement.TryGetProperty("status", out var statusElement) &&
                        statusElement.GetString() == "OK" &&
                        jsonDoc.RootElement.TryGetProperty("body", out var bodyElement) &&
                        bodyElement.TryGetProperty("token", out var tokenElement))
                    {
                        var token = tokenElement.GetString();
                        var tokenType = bodyElement.TryGetProperty("tokenType", out var typeElement)
                            ? typeElement.GetString() : "Bearer";

                        return new HaciendaAuthResult
                        {
                            Success = true,
                            Token = token,
                            TokenType = tokenType
                        };
                    }
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
            try
            {
                var haciendaSettings = _configuration.GetSection("HaciendaSettings");
                var urlSection = ambiente == "00" ? "TestingUrls" : "ProductionUrls";
                var url = haciendaSettings.GetValue<string>($"{urlSection}:{endpoint}");

                return url ?? "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }



        private async Task<string?> GetOrRefreshHaciendaToken(string userNit, string ambiente)
        {
            try
            {
                // 1. BUSCAR TOKEN EXISTENTE
                var user = await _context.Users
                    .Where(u => u.Nit == userNit)
                    .FirstOrDefaultAsync();

                if (user == null) return null;

                // 2. VERIFICAR SI TOKEN ESTÁ VIGENTE
                if (!string.IsNullOrEmpty(user.HaciendaToken) &&  user.TokenExpiresAt.HasValue && user.TokenExpiresAt > DateTime.Now)
                {
                    return user.HaciendaToken;
                }

                var authResult = await AuthenticateUser(user.Nit, user.JwtSecret, ambiente); 

                if (authResult.Success && !string.IsNullOrEmpty(authResult.Token))
                {
                    // 4. GUARDAR NUEVO TOKEN CON FORMATO CORRECTO
                    var tokenToSave = authResult.Token.StartsWith("Bearer ") ?
                        authResult.Token : $"Bearer {authResult.Token}";

                    user.HaciendaToken = tokenToSave;
                    user.TokenExpiresAt = DateTime.Now.AddHours(4); // Tokens de MH duran ~4 horas

                    await _context.SaveChangesAsync();
                    return tokenToSave;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }

   
}
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
        Task<HaciendaTransmissionResult> TransmitDocument(string signedJWT, string userNit, string ambiente, string documentType , int version );
        Task<HaciendaAuthResult> AuthenticateUser(string userHacienda, string userPassword, string ambiente);
        Task<HaciendaTransmissionResult?> TransmitInvalidation(string signedJWT, string userNit, string ambiente, string invalidacionId);
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

        public async Task<HaciendaTransmissionResult> TransmitDocument(string signedJWT, string userNit, string ambiente, string documentType, int version)
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

                // 4. PREPARAR REQUEST PARA HACIENDA - VERSION VIENE DESDE EL CONTROLADOR
                var haciendaRequest = new
                {
                    ambiente = ambiente,
                    idEnvio = 1,
                    version = version,
                    tipoDte = documentType,
                    documento = signedJWT
                };

                var jsonContent = JsonSerializer.Serialize(haciendaRequest, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // *** ÚNICO LOGGING: ESTRUCTURA DEL JSON ***
                _logger.LogInformation("JSON Request: {JsonContent}", jsonContent);

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


        public async Task<HaciendaTransmissionResult?> TransmitInvalidation(string signedJWT, string userNit, string ambiente, string invalidacionId)
        {
            try
            {
                // 1. OBTENER TOKEN DE HACIENDA
                var token = await GetOrRefreshHaciendaToken(userNit, ambiente);
                if (string.IsNullOrEmpty(token))
                {
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Status = "ERROR_AUTH",
                        Error = "No se pudo obtener token de autenticación",
                        ResponseCode = "AUTH_ERROR"
                    };
                }

                // 2. PREPARAR REQUEST PARA HACIENDA (formato específico de invalidación)
                var haciendaRequest = new
                {
                    ambiente = ambiente,
                    idEnvio = 1,
                    version = 2,
                    documento = signedJWT
                };

                var jsonContent = JsonSerializer.Serialize(haciendaRequest);

                // 3. CONFIGURAR HTTP CLIENT
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(45);

                // 4. OBTENER URL DE INVALIDACIÓN
                var nullifyUrl = GetHaciendaUrl("NullifyUrl", ambiente);
                if (string.IsNullOrEmpty(nullifyUrl))
                {
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Status = "ERROR_CONFIG",
                        Error = "URL de invalidación no configurada",
                        ResponseCode = "CONFIG_ERROR"
                    };
                }

                // 5. CONFIGURAR HEADERS
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", token);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "ApiContabsv/1.0");

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 6. ENVIAR A HACIENDA
                var response = await httpClient.PostAsync(nullifyUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Respuesta invalidación Hacienda - Status: {response.StatusCode}, Content: {responseContent}");

                // 7. PROCESAR RESPUESTA (MISMA LÓGICA QUE TransmitDocument)
                HaciendaResponse? haciendaResponse = null;
                try
                {
                    haciendaResponse = JsonSerializer.Deserialize<HaciendaResponse>(responseContent);
                }
                catch (JsonException)
                {
                    // Si no se puede parsear como JSON, es un error de transmisión
                }

                // LÓGICA IGUAL QUE TransmitDocument:
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
                        Error = isRejected ? $"Invalidación rechazada por Hacienda: {haciendaResponse.Descripcion}" : null,
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
                _logger.LogError(httpEx, "Error HTTP enviando invalidación {InvalidacionId} a Hacienda", invalidacionId);

                return new HaciendaTransmissionResult
                {
                    Success = false,
                    Status = "ERROR_CONNECTION",
                    Error = "Error de conexión con Hacienda",
                    ErrorDetails = httpEx.Message,
                    ResponseCode = "HTTP_ERROR"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno enviando invalidación {InvalidacionId} a Hacienda", invalidacionId);

                return new HaciendaTransmissionResult
                {
                    Success = false,
                    Status = "ERROR_INTERNAL",
                    Error = "Error interno procesando invalidación",
                    ErrorDetails = ex.Message,
                    ResponseCode = "INTERNAL_ERROR"
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
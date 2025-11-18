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
                _logger.LogInformation("Buscando credenciales para NIT: {NIT}", userNit);

                var user = await _context.Users
                    .Where(u => u.Nit == userNit)
                    .Select(u => new HaciendaUserCredentials
                    {
                        UserHacienda = u.Nit,
                        PassHacienda = u.PasswordPri
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("No se encontró usuario con NIT: {NIT}", userNit);
                    return null;
                }

                var hasCredentials = !string.IsNullOrEmpty(user.UserHacienda) && !string.IsNullOrEmpty(user.PassHacienda);
                _logger.LogInformation("Usuario encontrado. Tiene credenciales: {HasCredentials}", hasCredentials);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo credenciales de Hacienda para NIT: {NIT}", userNit);
                return null;
            }
        }

        public async Task<HaciendaTransmissionResult> TransmitDocument(string signedJWT, string userNit, string ambiente, string documentType = "01")
        {
            try
            {
                _logger.LogInformation("=== INICIANDO TRANSMISIÓN A HACIENDA ===");
                _logger.LogInformation("NIT: {NIT}, Ambiente: {Ambiente}, Tipo: {Tipo}", userNit, ambiente, documentType);

                // 1. OBTENER CREDENCIALES DEL USUARIO DESDE BD
                var user = await GetUserCredentials(userNit);
                if (user == null || string.IsNullOrEmpty(user.UserHacienda) || string.IsNullOrEmpty(user.PassHacienda))
                {
                    _logger.LogError("Usuario no encontrado o sin credenciales de Hacienda para NIT: {NIT}", userNit);
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Error = "Usuario no encontrado o sin credenciales de Hacienda",
                        ErrorDetails = $"NIT: {userNit} no tiene user_hacienda/pass_hacienda configurados"
                    };
                }

                _logger.LogInformation("Credenciales encontradas. User Hacienda: {UserHacienda}", user.UserHacienda);

                // 2. OBTENER TOKEN DE HACIENDA
                _logger.LogInformation("Iniciando autenticación con Hacienda...");
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
                _logger.LogInformation("URL de recepción: {URL}", receptionUrl);

                if (string.IsNullOrEmpty(receptionUrl))
                {
                    _logger.LogError("URL de recepción no configurada para ambiente: {Ambiente}", ambiente);
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

                _logger.LogInformation("Request a Hacienda preparado:");
                _logger.LogInformation("  ambiente: {ambiente}", ambiente);
                _logger.LogInformation("  tipoDte: {tipoDte}", documentType);
                _logger.LogInformation("  JWT length: {length}", signedJWT?.Length ?? 0);

                // 5. AGREGAR HEADERS DE AUTORIZACIÓN
                httpClient.DefaultRequestHeaders.Add("Authorization", token);  // ✅ USAR token, NO authResult.Token
                httpClient.DefaultRequestHeaders.Add("User-Agent", "API-DTE-SV/1.0");


                // 6. ENVIAR A HACIENDA
                var response = await httpClient.PostAsync(receptionUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("=== RESPUESTA DE HACIENDA ===");
                _logger.LogInformation("Status Code: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Success: {IsSuccess}", response.IsSuccessStatusCode);
                _logger.LogInformation("Response Length: {Length}", responseContent?.Length ?? 0);
                _logger.LogInformation("Response Content: {Content}", responseContent);

                // Log headers de respuesta
                foreach (var header in response.Headers)
                {
                    _logger.LogInformation("Header: {Key} = {Value}", header.Key, string.Join(", ", header.Value));
                }

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Respuesta exitosa de Hacienda. Parseando...");

                    try
                    {
                        var haciendaResponse = JsonSerializer.Deserialize<HaciendaResponse>(responseContent);

                        _logger.LogInformation("Respuesta parseada exitosamente:");
                        _logger.LogInformation("  Estado: {Estado}", haciendaResponse?.Estado);
                        _logger.LogInformation("  SelloRecibido: {Sello}", haciendaResponse?.SelloRecibido);
                        _logger.LogInformation("  CodigoMsg: {Codigo}", haciendaResponse?.CodigoMsg);

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
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error parseando respuesta JSON de Hacienda");
                        return new HaciendaTransmissionResult
                        {
                            Success = false,
                            Error = "Error parseando respuesta de Hacienda",
                            ErrorDetails = jsonEx.Message,
                            RawResponse = responseContent
                        };
                    }
                }
                else
                {
                    _logger.LogError("Error HTTP de Hacienda: {StatusCode}", response.StatusCode);
                    return new HaciendaTransmissionResult
                    {
                        Success = false,
                        Error = $"Error HTTP {response.StatusCode}",
                        ErrorDetails = responseContent,
                        RawResponse = responseContent
                    };
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error de conexión con Hacienda para NIT: {NIT}", userNit);
                return new HaciendaTransmissionResult
                {
                    Success = false,
                    Error = "Error de conexión con Hacienda",
                    ErrorDetails = httpEx.Message
                };
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger.LogError(timeoutEx, "Timeout conectando con Hacienda para NIT: {NIT}", userNit);
                return new HaciendaTransmissionResult
                {
                    Success = false,
                    Error = "Timeout conectando con Hacienda",
                    ErrorDetails = timeoutEx.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno transmitiendo documento a Hacienda para NIT: {NIT}", userNit);
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
                    // ✅ PARSING FLEXIBLE PARA CUALQUIER ESTRUCTURA
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

                _logger.LogInformation("URL obtenida para {Endpoint} en ambiente {Ambiente}: {URL}", endpoint, ambiente, url);

                return url ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo URL para {Endpoint} en ambiente {Ambiente}", endpoint, ambiente);
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
                if (!string.IsNullOrEmpty(user.HaciendaToken) &&
                    user.TokenExpiresAt.HasValue &&
                    user.TokenExpiresAt > DateTime.Now)
                {
                    _logger.LogInformation("Token reutilizado para NIT: {NIT}", userNit);
                    return user.HaciendaToken;
                }

                // 3. OBTENER NUEVO TOKEN
                _logger.LogInformation("Obteniendo nuevo token para NIT: {NIT}", userNit);
                var authResult = await AuthenticateUser(user.Nit, user.PasswordPri, ambiente);

                if (authResult.Success && !string.IsNullOrEmpty(authResult.Token))
                {
                    // 4. GUARDAR NUEVO TOKEN
                    user.HaciendaToken = authResult.Token;
                    user.TokenExpiresAt = DateTime.Now.AddDays(user.TokenLifetimeDays ?? 30);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Nuevo token guardado para NIT: {NIT}", userNit);
                    return authResult.Token;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error gestionando token para NIT: {NIT}", userNit);
                return null;
            }
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
        [JsonPropertyName("user")]  
        public string? User { get; set; }

        [JsonPropertyName("token")]  
        public string? Token { get; set; }

        [JsonPropertyName("tokenType")]  
        public string? TokenType { get; set; }
    }

    public class HaciendaUserCredentials
    {
        public string? UserHacienda { get; set; }
        public string? PassHacienda { get; set; }
    }
}
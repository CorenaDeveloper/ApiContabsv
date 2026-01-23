using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Dte;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_InvalidacionController : ControllerBase
    {
        private readonly dteContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHaciendaService _haciendaService;
        private readonly IDTEDocumentService _documentService;
        private readonly ILogger<DBDte_InvalidacionController> _logger;

        public DBDte_InvalidacionController(
            dteContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHaciendaService haciendaService,
            IDTEDocumentService documentService,
            ILogger<DBDte_InvalidacionController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _haciendaService = haciendaService;
            _documentService = documentService;
            _logger = logger;
        }



        /// <summary>
        /// OBTENER DOCUMENTO POR DTE ID
        /// </summary>
        [HttpGet("Confirmacion/{dteId}")]
        public async Task<ActionResult<DTEDocumentResponse>> GetDocument(string dteId)
        {
            try
            {
                var document = await _documentService.GetDocument(dteId);
                if (document == null)
                    return NotFound("Documento no encontrado");

                return Ok(document);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error interno obteniendo documento");
            }
        }
        /// <summary>
        /// INVALIDAR DOCUMENTO DTE
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> InvalidateDocument([FromBody] InvalidacionDocumentoDto request)
        {
            string? invalidacionId = null;

            try
            {
                // 1. VALIDAR ENTRADA
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // 2. OBTENER DATOS DEL USER
                var user = await _context.Users.FindAsync(request.ClienteId);
                if (user == null)
                    return BadRequest($"Usuario con ID {request.ClienteId} no encontrado");

                // 3. OBTENER FIRMADOR ÓPTIMO (siguiendo tu patrón)
                var optimalSigner = await GetOptimalSignerForUser(request.UserId);
                if (optimalSigner == null)
                    return BadRequest("No hay firmadores disponibles para el usuario");

                // 4. BUSCAR DOCUMENTO ORIGINAL
                var documento = await _context.DteDocuments
                    .FirstOrDefaultAsync(d => d.DteId == request.GenerationCode);

                if (documento == null)
                    return NotFound("Documento no encontrado");

                // 5. BUSCAR DETALLES PARA CONTROL_NUMBER
                var detalles = await _context.DteDetails
                    .FirstOrDefaultAsync(d => d.DteId == request.GenerationCode);

                if (detalles == null)
                    return NotFound("Detalles del documento no encontrados");

                // 6. GENERAR INVALIDACION ID
                invalidacionId = Guid.NewGuid().ToString().ToUpper();

                // 7. CONSTRUIR DOCUMENTO DE INVALIDACIÓN
                var invalidacionDocument = await BuildInvalidacionDocument(user, request, documento, detalles, invalidacionId);

                // 8. FIRMAR DOCUMENTO (usando tu patrón)
                var signResult = await SignDocument(user, invalidacionDocument, optimalSigner);
                if (!signResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error al firmar documento de invalidación",
                        error = signResult.Error
                    });
                }

                // 9. EXTRAER JWT FIRMADO
                var signedJWT = ExtractJWTFromSignerResponse(signResult.Response);
                if (string.IsNullOrEmpty(signedJWT))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error extrayendo JWT firmado"
                    });
                }

                // 10. ENVIAR A HACIENDA (usando tu HaciendaService)
                var transmissionResult = await _haciendaService.TransmitInvalidation(
                    signedJWT,
                    user.Nit,
                    request.Environment ?? "00",
                    invalidacionId
                );

                // 11. PROCESAR RESPUESTA
                string finalStatus = "FIRMADO";

                if (transmissionResult != null)
                {
                    if (transmissionResult.Success)
                    {
                        finalStatus = transmissionResult.Status ?? "PROCESADO";
                        documento.Status = "INVALIDADO";
                        documento.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    else if (transmissionResult.Status == "RECHAZADO")
                    {
                        finalStatus = "RECHAZADO";
                    }
                    else
                    {
                        finalStatus = "ERROR_TRANSMISION";
                    }
                }

                // 12. RESPUESTA
                var response = new
                {
                    success = true,
                    message = "Invalidación procesada exitosamente",
                    data = new
                    {
                        invalidacionId = invalidacionId,
                        codigoGeneracion = invalidacionId,
                        documentoOriginal = request.GenerationCode,
                        signer = optimalSigner.SignerName,
                        signedJWT = signedJWT,
                        hacienda = new
                        {
                            sent = transmissionResult != null,
                            success = transmissionResult?.Success ?? false,
                            status = transmissionResult?.Status,
                            receptionStamp = transmissionResult?.ReceptionStamp,
                            responseCode = transmissionResult?.ResponseCode,
                            message = transmissionResult?.Message,
                            error = transmissionResult?.Error,
                            errorDetails = transmissionResult?.ErrorDetails
                        },
                        invalidacion = new
                        {
                            status = finalStatus,
                            type = request.Reason.Type,
                            createdAt = DateTime.Now
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno procesando invalidación",
                    error = ex.Message,
                    invalidacionId = invalidacionId
                });
            }
        }

        #region Helper Methods (igual que Invoice)

        private async Task<object> BuildInvalidacionDocument(User user, InvalidacionDocumentoDto request, DteDocument documento, DteDetail detalles, string invalidacionId)
        {
            // OBTENER DATOS REALES DEL RECEPTOR
            var receptorData = await GetOriginalDocumentReceptor(documento.JsonContent);

            return new
            {
                identificacion = new
                {
                    version = 2,
                    ambiente = request.Environment ?? "00",
                    codigoGeneracion = invalidacionId,
                    fecAnula = DateTime.Now.ToString("yyyy-MM-dd"),
                    horAnula = DateTime.Now.ToString("HH:mm:ss")
                },
                emisor = new
                {
                    nit = user.Nit,
                    nombre = user.BusinessName ?? user.CommercialName,
                    tipoEstablecimiento = "02",
                    telefono = "61032136",
                    correo = "corenadeveloper@gmail.com",
                    codEstable = "M001",
                    codPuntoVenta = "P000",
                    nomEstablecimiento = user.CommercialName
                },
                documento = new
                {
                    tipoDte = documento.DocumentType,
                    codigoGeneracion = documento.DteId,
                    selloRecibido = documento.ReceptionStamp ?? "",
                    numeroControl = detalles.ControlNumber,
                    fecEmi = documento.CreatedAt?.ToString("yyyy-MM-dd"),
                    montoIva = receptorData.MontoIva,
                    codigoGeneracionR = request.Reason.Type == 2 ? null : request.ReplacementGenerationCode,
                    tipoDocumento = receptorData.TipoDocumento,
                    numDocumento = receptorData.NumDocumento,
                    nombre = receptorData.Nombre,
                    telefono = receptorData.Telefono,
                    correo = receptorData.Correo
                },
                motivo = new
                {
                    tipoAnulacion = request.Reason.Type,
                    motivoAnulacion = request.Reason.Type == 3 ? request.Reason.Reason : null,
                    nombreResponsable = request.Reason.ResponsibleName,
                    tipDocResponsable = request.Reason.ResponsibleDocType,
                    numDocResponsable = request.Reason.ResponsibleNumDoc,
                    nombreSolicita = request.Reason.RequestorName,
                    tipDocSolicita = request.Reason.RequestorDocType,
                    numDocSolicita = request.Reason.RequestorNumDoc
                }
            };
        }

        private async Task<ReceptorData> GetOriginalDocumentReceptor(string? jsonContent)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonContent))
                {
                    return new ReceptorData
                    {
                        TipoDocumento = "13",
                        NumDocumento = "00000000-0",
                        Nombre = "CONSUMIDOR FINAL",
                        MontoIva = 0.0m,
                        Telefono = null,
                        Correo = null
                    };
                }

                var jsonDoc = JsonDocument.Parse(jsonContent);

                string? tipoDocumento = null;
                string? numDocumento = null;
                string? nombre = null;
                string? telefono = null;
                string? correo = null;
                decimal montoIva = 0.0m;

                if (jsonDoc.RootElement.TryGetProperty("receptor", out var receptor))
                {
                    if (receptor.TryGetProperty("tipoDocumento", out var tipoDoc))
                        tipoDocumento = tipoDoc.GetString();

                    if (receptor.TryGetProperty("numDocumento", out var numDoc))
                        numDocumento = numDoc.GetString();

                    if (receptor.TryGetProperty("nombre", out var nombreProp))
                        nombre = nombreProp.GetString();

                    if (receptor.TryGetProperty("telefono", out var telefonoProp))
                        telefono = telefonoProp.GetString();

                    if (receptor.TryGetProperty("correo", out var correoProp))
                        correo = correoProp.GetString();
                }

                if (jsonDoc.RootElement.TryGetProperty("resumen", out var resumen))
                {
                    if (resumen.TryGetProperty("totalIva", out var totalIva))
                    {
                        if (totalIva.TryGetDecimal(out var ivaValue))
                            montoIva = ivaValue;
                    }
                }

                return new ReceptorData
                {
                    TipoDocumento = tipoDocumento ?? "13",
                    NumDocumento = numDocumento ?? "00000000-0",
                    Nombre = nombre ?? "CONSUMIDOR FINAL",
                    MontoIva = montoIva,
                    Telefono = telefono,
                    Correo = correo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parseando JSON del documento original");

                return new ReceptorData
                {
                    TipoDocumento = "13",
                    NumDocumento = "00000000-0",
                    Nombre = "CONSUMIDOR FINAL",
                    MontoIva = 0.0m,
                    Telefono = null,
                    Correo = null
                };
            }
        }

        private async Task<SigningResult> SignDocument(User user, object invalidacionDocument, SignerResponseDTO signer)
        {
            try
            {
                var firmingRequest = new
                {
                    nit = user.Nit,
                    activo = true,
                    passwordPri = user.PasswordPri,
                    dteJson = invalidacionDocument
                };

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                if (!string.IsNullOrEmpty(user.JwtSecret))
                {
                    var jwtToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.JwtSecret));
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");
                }

                var jsonContent = JsonSerializer.Serialize(firmingRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(signer.EndpointUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return new SigningResult
                {
                    Success = response.IsSuccessStatusCode,
                    Response = responseContent,
                    Error = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new SigningResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private string? ExtractJWTFromSignerResponse(string signerResponse)
        {
            try
            {
                var responseObj = JsonSerializer.Deserialize<JsonElement>(signerResponse);

                if (responseObj.TryGetProperty("status", out var status) &&
                    status.GetString() == "OK" &&
                    responseObj.TryGetProperty("body", out var body))
                {
                    return body.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo JWT de respuesta del firmador");
            }

            return null;
        }

        private async Task<SignerResponseDTO?> GetOptimalSignerForUser(int userId)
        {
            var userAssignments = await _context.SignerAssignments
                .Include(sa => sa.Signer)
                .Where(sa => sa.UserId == userId && sa.Signer.IsActive && sa.Signer.HealthStatus == "Healthy")
                .ToListAsync();

            if (userAssignments.Any())
            {
                var optimalAssignment = userAssignments
                    .Where(sa => sa.Signer.CurrentLoad < sa.Signer.MaxConcurrentSigns)
                    .OrderByDescending(sa => sa.IsPrimary)
                    .ThenBy(sa => sa.Signer.CurrentLoad)
                    .FirstOrDefault();

                if (optimalAssignment != null)
                {
                    return new SignerResponseDTO
                    {
                        Id = optimalAssignment.Signer.Id,
                        SignerName = optimalAssignment.Signer.SignerName,
                        CertificatePath = optimalAssignment.Signer.CertificatePath,
                        EndpointUrl = optimalAssignment.Signer.EndpointUrl
                    };
                }
            }

            return null;
        }

        private class ReceptorData
        {
            public string TipoDocumento { get; set; } = string.Empty;
            public string NumDocumento { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public decimal MontoIva { get; set; }
            public string? Telefono { get; set; }
            public string? Correo { get; set; }
        }

        #endregion
    }
}
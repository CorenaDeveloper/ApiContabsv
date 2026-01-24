using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Dte;
using ApiContabsv.Services;
using CloudinaryDotNet.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using XAct;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_CreditNoteController : ControllerBase
    {
        private readonly dteContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHaciendaService _haciendaService;
        private readonly IDTEDocumentService _documentService;
        private readonly ILogger<DBDte_CreditNoteController> _logger;

        public DBDte_CreditNoteController(
            dteContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHaciendaService haciendaService,
            IDTEDocumentService documentService,
            ILogger<DBDte_CreditNoteController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _haciendaService = haciendaService;
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// CREAR NOTA DE CRÉDITO ELECTRÓNICA
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateCreditNote([FromBody] CreateCreditNoteRequestDTO request)
        {
            string? dteId = null;

            try
            {
                // 1. OBTENER DATOS DEL USER (EMISOR)
                var user = await _context.Users.FindAsync(request.ClientId);
                if (user == null)
                    return BadRequest($"Usuario con ID {request.ClientId} no encontrado");

                // 2. OBTENER FIRMADOR ÓPTIMO
                var optimalSigner = await GetOptimalSignerForUser(request.UserId);
                if (optimalSigner == null)
                    return BadRequest("No hay firmadores disponibles para el usuario");

                // 3. VERIFICAR CERTIFICADO
                var certificatePath = Path.Combine(optimalSigner.CertificatePath, $"{user.Nit}.crt");
                if (!System.IO.File.Exists(certificatePath))
                    return BadRequest($"Certificado no encontrado para NIT: {user.Nit}");

                // 4. GENERAR NÚMERO DE CONTROL ÚNICO (tipo 05 para Nota de Crédito)
                var sequenceNumber = await _documentService.GetNextSequenceNumber(
                    user.Id, "05", "M001", "P000");

                var controlNumber = $"DTE-05-M001P000-{DateTime.Now.Year}{sequenceNumber:00000000000}";

                // 5. GENERAR DTE ID ÚNICO
                dteId = Guid.NewGuid().ToString().ToUpper();

                // 6. CONSTRUIR DOCUMENTO DTE
                var dteDocument = BuildCreditNoteDocument(user, request, controlNumber, dteId);

                // 7. FIRMAR DOCUMENTO
                var signResult = await SignDocument(user, dteDocument, optimalSigner);
                if (!signResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error al firmar documento",
                        error = signResult.Error
                    });
                }

                // 8. EXTRAER JWT FIRMADO
                var signedJWT = ExtractJWTFromSignerResponse(signResult.Response);
                if (string.IsNullOrEmpty(signedJWT))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error extrayendo JWT firmado"
                    });
                }

                // 9. GUARDAR EN BASE DE DATOS (ANTES DE ENVIAR)
                var saveRequest = new SaveDocumentRequest
                {
                    DteId = dteId,
                    UserId = user.Id,
                    DocumentType = "05",
                    GenerationType = 1,
                    ControlNumber = controlNumber,
                    TotalAmount = request.Summary?.TotalToPay ?? 0,
                    Status = "FIRMADO",
                    JsonContent = JsonSerializer.Serialize(dteDocument),
                    EstablishmentCode = "M001",
                    PosCode = "P000"
                };

                var documentId = await _documentService.SaveDocument(saveRequest);

                // 10. ENVIAR A HACIENDA
                HaciendaTransmissionResult? transmissionResult = null;
                string finalDocumentStatus = "FIRMADO";

                if (request.SendToHacienda != false)
                {
                    transmissionResult = await _haciendaService.TransmitDocument(signedJWT,
                        user.Nit,
                        request.Environment ?? "00",
                        "05",
                        1
                    );

                    if (transmissionResult != null)
                    {
                        string errorMessage = null;
                        string errorDetails = null;
                        string responseCode = null;

                        if (transmissionResult.Success)
                        {
                            finalDocumentStatus = transmissionResult.Status ?? "PROCESADO";
                            await _documentService.UpdateDocumentStatus(dteId, finalDocumentStatus, transmissionResult.ReceptionStamp);
                        }
                        else if (transmissionResult.Status == "RECHAZADO")
                        {
                            finalDocumentStatus = "RECHAZADO";
                            errorMessage = transmissionResult.Error;
                            errorDetails = transmissionResult.ErrorDetails;
                            responseCode = transmissionResult.ResponseCode;

                            await _documentService.UpdateDocumentStatus(dteId, finalDocumentStatus, null, errorMessage, errorDetails, null, responseCode);
                        }
                        else
                        {
                            finalDocumentStatus = "ERROR_TRANSMISION";
                            await _documentService.UpdateDocumentStatus(dteId, finalDocumentStatus);
                        }

                        _logger.LogInformation($"Nota de Crédito {dteId} procesada con estado: {finalDocumentStatus}");
                    }
                }

                // 11. RESPUESTA EXITOSA ESTRUCTURADA
                var response = new
                {
                    success = true,
                    message = "Nota de Crédito procesada exitosamente",
                    data = new
                    {
                        dteId = dteId,
                        codigoGeneracion = dteId,
                        controlNumber = controlNumber,
                        documentType = "05",
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
                        document = new
                        {
                            status = finalDocumentStatus,
                            createdAt = DateTime.Now,
                            totalAmount = saveRequest.TotalAmount
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(dteId))
                {
                    try
                    {
                        await _documentService.UpdateDocumentStatus(dteId, "ERROR");
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, "Error actualizando estado a ERROR");
                    }
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno procesando Nota de Crédito",
                    error = ex.Message,
                    dteId = dteId
                });
            }
        }

        /// <summary>
        /// OBTENER NOTA DE CRÉDITO POR ID
        /// </summary>
        [HttpGet("{dteId}")]
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
                _logger.LogError(ex, "Error obteniendo documento {DteId}", dteId);
                return StatusCode(500, "Error interno obteniendo documento");
            }
        }

        /// <summary>
        /// LISTAR NOTAS DE CRÉDITO DE UN USUARIO
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<DTEDocumentResponse>>> GetUserDocuments(int userId, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out DateTime start))
                {
                    parsedStartDate = start;
                }

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out DateTime end))
                {
                    parsedEndDate = end;
                }

                var documents = await _documentService.GetDocumentsByUser(userId, parsedStartDate, parsedEndDate, "05");
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo documentos del usuario {UserId}", userId);
                return StatusCode(500, "Error interno obteniendo documentos");
            }
        }

        #region Helper Methods (igual que Invoice)

        private object BuildCreditNoteDocument(User user, CreateCreditNoteRequestDTO request, string controlNumber, string dteId)
        {
            return new
            {
                // SECCIÓN IDENTIFICACION
                identificacion = new
                {
                    version = 1,
                    ambiente = request.Environment ?? "00",
                    tipoDte = "05", // ✅ Nota de Crédito
                    numeroControl = controlNumber,
                    codigoGeneracion = dteId,
                    tipoModelo = request.ModelType ?? 1,
                    tipoOperacion = 1,
                    tipoContingencia = (int?)null,
                    motivoContin = (string?)null,
                    fecEmi = DateTime.Now.ToString("yyyy-MM-dd"),
                    horEmi = DateTime.Now.ToString("HH:mm:ss"),
                    tipoMoneda = "USD"
                },

                // SECCIONES PRINCIPALES
                emisor = MapEmisorFromUser(user),
                receptor = MapReceptor(request.Receiver),
                cuerpoDocumento = MapCreditNoteItems(request.Items),
                resumen = MapCreditNoteSummary(request.Summary),

                // DOCUMENTOS RELACIONADOS (opcional para Credit Note)
                documentoRelacionado = request.RelatedDocs != null && request.RelatedDocs.Any()
                    ? MapRelatedDocs(request.RelatedDocs)
                    : null,

                // CAMPOS OPCIONALES
                otrosDocumentos = (object?)null,
                ventaTercero = request.ThirdPartySale != null ? new
                {
                    nit = request.ThirdPartySale.Nit,
                    nombre = request.ThirdPartySale.Name
                } : null,
                extension = (object?)null,
                apendice = (object?)null
            };
        }

        private object MapEmisorFromUser(User user)
        {
            return new
            {
                nit = user.Nit,
                nrc = user.Nrc,
                nombre = user.BusinessName ?? user.CommercialName,
                codActividad = user.EconomicActivity,
                descActividad = user.EconomicActivityDesc,
                nombreComercial = user.CommercialName,
                tipoEstablecimiento = "02",
                codEstable = "M001",
                codPuntoVenta = "P000",
                direccion = new
                {
                    departamento = "03",
                    municipio = "18",
                    complemento = "Barrio el Ángel, calle el Ángel, casa 26 Sonsonate"
                },
                telefono = "61032136",
                correo = "corenadeveloper@gmail.com",
                codEstableMH = "M001",
                codPuntoVentaMH = "P000"
            };
        }

        private object? MapReceptor(CreditNoteReceiverRequestDTO? receiver)
        {
            if (receiver == null) return null;

            return new
            {
                nombre = receiver.Name,
                tipoDocumento = receiver.DocumentType,
                numDocumento = receiver.DocumentNumber,
                nrc = receiver.Nrc,
                codActividad = receiver.ActivityCode,
                descActividad = receiver.ActivityDescription,
                direccion = receiver.Address != null ? new
                {
                    departamento = receiver.Address.Department,
                    municipio = receiver.Address.Municipality,
                    complemento = receiver.Address.Complement
                } : null,
                telefono = receiver.Phone,
                correo = receiver.Email
            };
        }

        private object[] MapCreditNoteItems(List<CreditNoteItemRequestDTO> items)
        {
            return items.Select((item, index) => new
            {
                numItem = index + 1,
                tipoItem = item.Type,
                numeroDocumento = (string?)null,
                codigo = item.Code,
                codTributo = (string?)null,
                descripcion = item.Description,
                cantidad = item.Quantity,
                uniMedida = item.UnitMeasure,
                precioUni = item.UnitPrice,
                montoDescu = item.Discount,
                ventaNoSuj = item.NonSubjectSale,
                ventaExenta = item.ExemptSale,
                ventaGravada = item.TaxedSale,
                tributos = item.Taxes?.ToArray(),
                psv = item.SuggestedPrice,
                noGravado = item.NonTaxed
            }).ToArray();
        }

        private object? MapCreditNoteSummary(CreditNoteSummaryRequestDTO? summary)
        {
            if (summary == null) return null;

            return new
            {
                totalNoSuj = summary.TotalNonSubject,
                totalExenta = summary.TotalExempt,
                totalGravada = summary.TotalTaxed,
                subTotalVentas = summary.SubTotal,
                descuNoSuj = summary.NonSubjectDiscount,
                descuExenta = summary.ExemptDiscount,
                descuGravada = summary.TaxedDiscount,
                porcentajeDescuento = summary.DiscountPercentage,
                totalDescu = summary.TotalDiscount,
                subTotal = summary.SubTotalSales,
                montoTotalOperacion = summary.TotalOperation,
                totalPagar = summary.TotalToPay,
                condicionOperacion = summary.OperationCondition,
                ivaRete1 = summary.IvaRetention,
                ivaPerci1 = summary.IvaPerception,
                reteRenta = summary.IncomeRetention,
                saldoFavor = summary.BalanceInFavor,
                totalIva = summary.TotalIva,
                tributos = (object[]?)null,
                totalLetras = "CERO CON 00/100 DOLARES",
                numPagoElectronico = (string?)null
            };
        }

        private object[]? MapRelatedDocs(List<CreditNoteRelatedDocRequestDTO>? relatedDocs)
        {
            if (relatedDocs == null || !relatedDocs.Any()) return null;

            return relatedDocs.Select(doc => new
            {
                tipoDocumento = doc.DocumentType,
                tipoGeneracion = doc.GenerationType,
                numeroDocumento = doc.DocumentNumber,
                fechaEmision = doc.EmissionDate.ToString("yyyy-MM-dd")
            }).ToArray();
        }

        private async Task<SigningResult> SignDocument(User user, object dteDocument, SignerResponseDTO signer)
        {
            try
            {
                var firmingRequest = new
                {
                    nit = user.Nit,
                    activo = true,
                    passwordPri = user.PasswordPri,
                    dteJson = dteDocument
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

        #endregion
    }
}
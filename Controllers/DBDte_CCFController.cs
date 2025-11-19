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
    public class DBDte_CCFController : ControllerBase
    {
        private readonly dteContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHaciendaService _haciendaService;
        private readonly IDTEDocumentService _documentService;
        private readonly ILogger<DBDte_CCFController> _logger;

        public DBDte_CCFController(
            dteContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHaciendaService haciendaService,
            IDTEDocumentService documentService,
            ILogger<DBDte_CCFController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _haciendaService = haciendaService;
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// CREAR COMPROBANTE DE CRÉDITO FISCAL (CCF)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateCCF([FromBody] CreateCCFRequestDTO request)
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

                // 4. GENERAR NÚMERO DE CONTROL ÚNICO (tipo 03 para CCF)
                var sequenceNumber = await _documentService.GetNextSequenceNumber(
                                     user.Id, "03", "M001", "P000");  // tipo "03" 
                var controlNumber = $"DTE-03-M001P000-{DateTime.Now.Year}{sequenceNumber:00000000000}";


                // 5. GENERAR DTE ID ÚNICO
                dteId = Guid.NewGuid().ToString().ToUpper();

                // 6. CONSTRUIR DOCUMENTO CCF
                var dteDocument = BuildCCFDocument(user, request, controlNumber, dteId);

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

                // 9. GUARDAR EN BASE DE DATOS
                var saveRequest = new SaveDocumentRequest
                {
                    DteId = dteId,
                    UserId = user.Id,
                    DocumentType = "03", // CCF
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

                if (request.SendToHacienda != false)
                {
                    transmissionResult = await _haciendaService.TransmitDocument(
                        signedJWT, user.Nit, request.Environment ?? "00", "03");

                    if (transmissionResult.Success)
                    {
                        await _documentService.UpdateDocumentStatus(
                            dteId,
                            transmissionResult.Status ?? "PROCESADO",
                            transmissionResult.ReceptionStamp);
                    }
                    else
                    {
                        _logger.LogWarning("Error transmitiendo CCF a Hacienda: {Error}",
                            transmissionResult.Error);
                    }
                }

                // 11. RESPUESTA EXITOSA
                var haciendaInfo = new
                {
                    sent = transmissionResult != null,
                    success = transmissionResult?.Success ?? false,
                    status = transmissionResult?.Status,
                    receptionStamp = transmissionResult?.ReceptionStamp,
                    error = transmissionResult?.Error,
                    errorDetails = transmissionResult?.ErrorDetails,
                    fullResponse = transmissionResult?.RawResponse
                };

                var response = new
                {
                    success = true,
                    message = "CCF procesado exitosamente",
                    data = new
                    {
                        documentId = documentId,
                        dteId = dteId,
                        numeroControl = controlNumber,
                        codigoGeneracion = dteId,
                        signer = optimalSigner.SignerName,
                        signedJWT = signedJWT,
                        hacienda = haciendaInfo,
                        document = new
                        {
                            status = transmissionResult?.Success == true ?
                                transmissionResult.Status : "FIRMADO",
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
                    message = "Error interno procesando CCF",
                    error = ex.Message,
                    dteId = dteId
                });
            }
        }

        /// <summary>
        /// OBTENER DOCUMENTO POR DTE ID
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
        /// LISTAR DOCUMENTOS CCF DE UN USUARIO
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<DTEDocumentResponse>>> GetUserDocuments(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var documents = await _documentService.GetDocumentsByUser(userId, page, pageSize);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo documentos del usuario {UserId}", userId);
                return StatusCode(500, "Error interno obteniendo documentos");
            }
        }

        #region Helper Methods

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

        private object BuildCCFDocument(User user, CreateCCFRequestDTO request, string controlNumber, string dteId)
        {
            return new
            {
                // ✅ IDENTIFICACION
                identificacion = new
                {
                    version = 1, // CCF usa version 3
                    ambiente = request.Environment ?? "00",
                    tipoDte = "03", // CCF
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

                // ✅ EMISOR
                emisor = MapEmisorFromUser(user),

                // ✅ RECEPTOR (con NRC obligatorio)
                receptor = MapCCFReceptor(request.Receiver),

                // ✅ CUERPO DOCUMENTO
                cuerpoDocumento = MapCCFItems(request.Items),

                // ✅ RESUMEN
                resumen = MapCCFResumen(request.Summary),

                // ✅ DOCUMENTOS RELACIONADOS (opcional)
                documentoRelacionado = request.RelatedDocs != null && request.RelatedDocs.Any()
                    ? MapRelatedDocs(request.RelatedDocs)
                    : null,

                // ✅ VENTA TERCERO (opcional)
                ventaTercero = request.ThirdPartySale != null ? new
                {
                    nit = request.ThirdPartySale.Nit,
                    nombre = request.ThirdPartySale.Name
                } : null,

                // ✅ CAMPOS OPCIONALES
                otrosDocumentos = (object?)null,
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
                tipoEstablecimiento = "02", // Casa Matriz
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

        private object MapCCFReceptor(CCFReceiverRequestDTO? receiver)
        {
            if (receiver == null) return null;

            return new
            {
                nombre = receiver.Name,
                tipoDocumento = receiver.DocumentType,
                numDocumento = receiver.DocumentNumber,
                nrc = receiver.Nrc, // ✅ OBLIGATORIO para CCF
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

        private object MapCCFItems(List<CCFItemRequestDTO> items)
        {
            return items.Select((item, index) => new
            {
                numItem = index + 1,
                tipoItem = item.Type,
                numeroDocumento = item.RelatedDoc,
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
                tributos = item.Taxes?.ToArray(), // ✅ Usar el array del request
                psv = item.SuggestedPrice,
                noGravado = item.NonTaxed
                // ❌ QUITAR: ivaItem = item.IvaItem
            }).ToArray();
        }

        private object MapCCFResumen(CCFSummaryRequestDTO? summary)
        {
            if (summary == null) return null;

            return new
            {
                totalNoSuj = summary.TotalNonSubject,
                totalExenta = summary.TotalExempt,
                totalGravada = summary.TotalTaxed,
                subTotalVentas = summary.SubTotalSales,
                descuNoSuj = summary.NonSubjectDiscount,
                descuExenta = summary.ExemptDiscount,
                descuGravada = summary.TaxedDiscount,
                porcentajeDescuento = summary.DiscountPercentage,
                totalDescu = summary.TotalDiscount,
                // ✅ Mapear el array de taxes del request
                tributos = summary.Taxes?.Select(t => new
                {
                    codigo = t.Code,
                    descripcion = t.Description,
                    valor = t.Value
                }).ToArray(),
                subTotal = summary.SubTotal,
                ivaRete1 = summary.IvaRetention,
                reteRenta = summary.IncomeRetention,
                ivaPerci1 = summary.IvaPerception,
                montoTotalOperacion = summary.TotalOperation,
                totalNoGravado = summary.TotalNonTaxed,
                totalPagar = summary.TotalToPay,
                totalLetras = NumberToWords(summary.TotalToPay),
                saldoFavor = summary.BalanceInFavor,
                condicionOperacion = summary.OperationCondition,
                pagos = summary.PaymentTypes?.Select(p => new
                {
                    codigo = p.Code,
                    montoPago = p.Amount,
                    referencia = p.Reference ?? "REF001",
                    periodo = p.Term ?? 1,
                    plazo = p.Period ?? "01"
                }).ToArray(),
                numPagoElectronico = (string?)null
            };
        }

        private object? MapRelatedDocs(List<RelatedDocRequestDTO> relatedDocs)
        {
            return relatedDocs.Select(rd => new
            {
                tipoDocumento = rd.DocumentType,
                tipoGeneracion = rd.GenerationType,
                numeroDocumento = rd.DocumentNumber,
                fechaEmision = rd.EmissionDate.ToString("yyyy-MM-dd")
            }).ToArray();
        }

        private string NumberToWords(decimal amount)
        {
            // Implementación simple - puedes mejorarla
            var intPart = (int)Math.Floor(amount);
            var decPart = (int)((amount - intPart) * 100);
            return $"{intPart:N0} CON {decPart:00}/100 DOLARES".Replace(",", " ");
        }

        #endregion
    }
}
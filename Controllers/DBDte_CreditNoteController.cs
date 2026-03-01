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
    public class DBDte_CreditNoteController : ControllerBase
    {
        private readonly dteContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHaciendaService _haciendaService;
        private readonly IDTEDocumentService _documentService;
        private readonly IContingencyService _contingencyService;
        private readonly ILogger<DBDte_CreditNoteController> _logger;

        public DBDte_CreditNoteController(
            dteContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHaciendaService haciendaService,
            IDTEDocumentService documentService,
            IContingencyService contingencyService,
            ILogger<DBDte_CreditNoteController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _haciendaService = haciendaService;
            _documentService = documentService;
            _contingencyService = contingencyService;
            _logger = logger;
        }

        /// <summary>
        /// CREAR NOTA DE CRÉDITO ELECTRÓNICA
    ////        / {
    ////          "clientId": 5,
    ////          "userId": 6,
    ////          "branchOfficeId": 1002,
    ////          "items": [
    ////            {
    ////              "type": 1,
    ////              "description": "Unidad de transporte",
    ////              "quantity": 1,
    ////              "unit_measure": 59,
    ////              "unit_price": 132.74,
    ////              "discount": 0,
    ////              "code": "PRD20260114094053",
    ////              "non_subject_sale": 0,
    ////              "exempt_sale": 0,
    ////              "taxed_sale": 132.74,
    ////              "taxes": [
    ////                "20"
    ////              ],
    ////              "related_document_number": "E84889AB-6562-4492-81E9-2E28833D4149"
    ////            }
    ////          ],
    ////          "receiver": {
    ////            "name": "Distribuidora Salvadoreña, S.A. de C.V",
    ////            "nit": "06142501071049",
    ////            "nrc": "1774110",
    ////            "activity_code": "11049",
    ////            "activity_description": "Elaboración de bebidas no alcohólicas",
    ////            "address": {
    ////              "department": "05",
    ////              "municipality": "25",
    ////              "complement": "Final Avenida San Martin, # 4-7 Entre calle 6 y 8 calle oriente, Santa Tecla"
    ////            },
    ////            "phone": "20222090",
    ////            "email": "ItCompras@gmail.com"
    ////          },
    ////          "modelType": 1,
    ////          "summary": {
    ////"total_non_subject": 0,
    ////            "total_exempt": 0,
    ////            "total_taxed": 132.74,
    ////            "sub_total": 132.74,
    ////            "non_subject_discount": 0,
    ////            "exempt_discount": 0,
    ////            "discount_percentage": 0,
    ////            "total_discount": 0,
    ////            "sub_total_sales": 132.74,
    ////            "total_operation": 148.67,
    ////            "total_to_pay": 148.67,
    ////            "operation_condition": 1,
    ////            "taxed_discount": 0,
    ////            "iva_perception": 0,
    ////            "iva_retention": 1.33,
    ////            "income_retention": 0,
    ////            "balance_in_favor": 0,
    ////            "total_iva": 17.26
    ////          },
    ////          "environment": "00",
    ////          "sendToHacienda": true,
    ////          "related_docs": [
    ////            {
    ////              "document_type": "03",
    ////              "generation_type": 1,
    ////              "document_number": "E84889AB-6562-4492-81E9-2E28833D4149",
    ////              "emission_date": "2026-02-03T00:00:00"
    ////            }
    ////          ]
    ////        }
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateCreditNote([FromBody] CreateCreditNoteRequestDTO request)
        {
            string? dteId = null;

            try
            {
                // 1. Obtenemos los datos del usuario emisor
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                    return BadRequest($"Usuario con ID {request.UserId} no encontrado");

                // 2. Validar que la sucursal pertenezca al usuario
                var branchOffice = await _context.BranchOffices
                                   .Include(b => b.Addresses)
                                   .FirstOrDefaultAsync(b => b.Id == request.BranchOfficeId
                                    && b.UserId == user.Id
                                    && b.IsActive);

                if (branchOffice == null)
                    return BadRequest($"Sucursal con ID {request.BranchOfficeId} no encontrada o inactiva");

                // 3. Obtener el firmador optimo para el usuario
                var optimalSigner = await GetOptimalSignerForUser(request.UserId);
                if (optimalSigner == null)
                    return BadRequest("No hay firmadores disponibles para el usuario");

                // 4. Verificamos que el certificado exista
                var certificatePath = Path.Combine(optimalSigner.CertificatePath, $"{user.Nit}.crt");
                if (!System.IO.File.Exists(certificatePath))
                    return BadRequest($"Certificado no encontrado para NIT: {user.Nit}");

                // 5. Generamos un numero de control interno valido
                var establishmentCode = branchOffice.EstablishmentCode ?? "";
                var posCode = branchOffice.PosCode ?? "";

                // 6. Obtener el siguiente número de secuencia para tipo "05" (Nota de Crédito)
                var sequenceNumber = await _documentService.GetNextSequenceNumber(
                    user.Id, "05", establishmentCode, posCode, request.Environment ?? "00");

                var controlNumber = $"DTE-05-{establishmentCode}{posCode}-{DateTime.Now.Year}{sequenceNumber:00000000000}";

                // 7. Generamos un DTE unico
                dteId = Guid.NewGuid().ToString().ToUpper();

                // 8. Construir documento DTE
                var dteDocument = BuildCreditNoteDocument(user, branchOffice, request, controlNumber, dteId, "05");

                // 9. Firmar documento
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

                // 10. Extraer JWT firmado
                var signedJWT = ExtractJWTFromSignerResponse(signResult.Response);
                if (string.IsNullOrEmpty(signedJWT))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error extrayendo JWT firmado"
                    });
                }

                // 11. Guardamos en BD antes de enviar a Hacienda
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
                    EstablishmentCode = establishmentCode,
                    PosCode = posCode,
                    Ambiente = request.Environment ?? ""
                };

                var documentId = await _documentService.SaveDocument(saveRequest);

                // 12. Transmisión a Hacienda con manejo de contingencia
                HaciendaTransmissionResult? transmissionResult = null;
                string finalDocumentStatus = "FIRMADO";

                if (request.SendToHacienda != false)
                {
                    transmissionResult = await _haciendaService.TransmitDocument(
                        signedJWT,
                        user.Nit,
                        request.Environment ?? "00",
                        "05",
                        3
                    );

                    if (transmissionResult != null)
                    {
                        if (transmissionResult.Success)
                        {
                            // ✅ Procesado exitosamente por Hacienda
                            finalDocumentStatus = transmissionResult.Status ?? "PROCESADO";

                            await _documentService.UpdateDocumentStatus(
                                dteId,
                                finalDocumentStatus,
                                transmissionResult.ReceptionStamp,
                                null,
                                null,
                                transmissionResult.RawResponse,
                                transmissionResult.ResponseCode);
                        }
                        else if (transmissionResult.Status == "RECHAZADO")
                        {
                            // ❌ Rechazado por Hacienda — NO va a contingencia, es definitivo
                            finalDocumentStatus = "RECHAZADO";

                            await _documentService.UpdateDocumentStatus(
                                dteId,
                                finalDocumentStatus,
                                transmissionResult.ReceptionStamp,
                                transmissionResult.Error,
                                transmissionResult.ErrorDetails,
                                transmissionResult.RawResponse,
                                transmissionResult.ResponseCode);
                        }
                        else if (_contingencyService.ShouldGoToContingency(transmissionResult))
                        {
                            // ⚠️ Error de red / MH caído → guardar en contingencia para reintentar
                            finalDocumentStatus = "CONTINGENCIA";

                            await _contingencyService.StoreInContingency(
                                dteId: dteId,
                                userId: user.Id,
                                documentType: "05",
                                signedJWT: signedJWT,
                                userNit: user.Nit,
                                ambiente: request.Environment ?? "00",
                                version: 3,
                                failedResult: transmissionResult);
                        }
                        else
                        {
                            // Error de negocio que no aplica contingencia
                            finalDocumentStatus = "ERROR_TRANSMISION";

                            await _documentService.UpdateDocumentStatus(
                                dteId,
                                finalDocumentStatus,
                                transmissionResult.ReceptionStamp,
                                transmissionResult.Error,
                                transmissionResult.ErrorDetails,
                                transmissionResult.RawResponse,
                                transmissionResult.ResponseCode);
                        }
                    }
                    else
                    {
                        // transmissionResult == null → problema de comunicación → contingencia
                        if (_contingencyService.ShouldGoToContingency(null))
                        {
                            finalDocumentStatus = "CONTINGENCIA";

                            await _contingencyService.StoreInContingency(
                                dteId: dteId,
                                userId: user.Id,
                                documentType: "05",
                                signedJWT: signedJWT,
                                userNit: user.Nit,
                                ambiente: request.Environment ?? "00",
                                version: 3);
                        }
                        else
                        {
                            finalDocumentStatus = "ERROR_TRANSMISION";

                            await _documentService.UpdateDocumentStatus(
                                dteId,
                                finalDocumentStatus,
                                null,
                                "Error interno: transmissionResult es null",
                                null,
                                null,
                                null);
                        }
                    }
                }

                // 13. Respuesta exitosa
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
                    try { await _documentService.UpdateDocumentStatus(dteId, "ERROR"); }
                    catch (Exception saveEx) { _logger.LogError(saveEx, "Error actualizando estado a ERROR"); }
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
        public async Task<ActionResult<DTEDocumentResponse>> GetDocument(
            string dteId,
            int userdte,
            string ambiente)
        {
            try
            {
                var document = await _documentService.GetDocument(dteId, userdte, ambiente);
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
        /// LISTAR NOTAS DE CRÉDITO DE UN USUARIO
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<DTEDocumentResponse>>> GetUserDocuments(
             int userId,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            string ambiente = "")
        {
            try
            {
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out DateTime start))
                    parsedStartDate = start;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out DateTime end))
                    parsedEndDate = end;

                var documents = await _documentService.GetDocumentsByUser(userId, parsedStartDate, parsedEndDate, "05", ambiente);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error interno obteniendo documentos");
            }
        }

        #region Helper Methods

        private object BuildCreditNoteDocument(User user, BranchOffice branchOffice, CreateCreditNoteRequestDTO request, string controlNumber, string dteId, string tipoDocumento)
        {
            return new
            {
                identificacion = new
                {
                    version = 3,
                    ambiente = request.Environment ?? "00",
                    tipoDte = tipoDocumento,
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
                emisor = MapEmisorFromUser(user, branchOffice),
                receptor = MapReceptor(request.Receiver),
                cuerpoDocumento = MapCreditNoteItems(request.Items),
                resumen = MapCreditNoteSummary(request.Summary),
                documentoRelacionado = MapRelatedDocs(request.RelatedDocs) ?? new object[0],
                ventaTercero = request.ThirdPartySale != null ? new
                {
                    nit = request.ThirdPartySale.Nit,
                    nombre = request.ThirdPartySale.Name
                } : null,
                extension = (object?)null,
                apendice = (object?)null
            };
        }

        private object MapEmisorFromUser(User user, BranchOffice branchOffice)
        {
            var address = branchOffice.Addresses?.FirstOrDefault();
            if (address == null)
                throw new InvalidOperationException($"La sucursal {branchOffice.Id} no tiene dirección configurada");
            if (string.IsNullOrEmpty(branchOffice.EstablishmentCode))
                throw new InvalidOperationException($"La sucursal {branchOffice.Id} no tiene código de establecimiento configurado");
            if (string.IsNullOrEmpty(branchOffice.PosCode))
                throw new InvalidOperationException($"La sucursal {branchOffice.Id} no tiene código de punto de venta configurado");

            return new
            {
                nit = user.Nit,
                nrc = user.Nrc,
                nombre = user.BusinessName ?? user.CommercialName,
                codActividad = user.EconomicActivity,
                descActividad = user.EconomicActivityDesc,
                nombreComercial = user.CommercialName,
                tipoEstablecimiento = branchOffice.EstablishmentType,
                direccion = new
                {
                    departamento = address.Department,
                    municipio = address.Municipality,
                    complemento = address.Address1 + (string.IsNullOrEmpty(address.Complement) ? "" : ", " + address.Complement)
                },
                telefono = branchOffice.Phone ?? user.Phone,
                correo = branchOffice.Email ?? user.Email,
            };
        }

        private object? MapReceptor(CreditNoteReceiverRequestDTO? receiver)
        {
            if (receiver == null) return null;

            return new
            {
                nit = receiver.DocumentNumber,
                nrc = receiver.Nrc,
                nombre = receiver.Name,
                nombreComercial = receiver.Name,
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
                numeroDocumento = item.RelatedDocumentNumber,
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
                tributos = item.Taxes?.ToArray()
            }).ToArray();
        }

        private object? MapCreditNoteSummary(CreditNoteSummaryRequestDTO? summary)
        {
            if (summary == null) return null;

            var tributos = new List<object>();
            if (summary.TotalTaxed > 0)
            {
                tributos.Add(new
                {
                    codigo = "20",
                    descripcion = "Impuesto al Valor Agregado 13%",
                    valor = Math.Round(summary.TotalTaxed * 0.13m, 2)
                });
            }

            return new
            {
                totalNoSuj = summary.TotalNonSubject,
                totalExenta = summary.TotalExempt,
                totalGravada = summary.TotalTaxed,
                subTotalVentas = summary.SubTotalSales,
                descuNoSuj = summary.NonSubjectDiscount,
                descuExenta = summary.ExemptDiscount,
                descuGravada = summary.TaxedDiscount,
                totalDescu = summary.TotalDiscount,
                tributos = tributos.ToArray(),
                subTotal = summary.SubTotal,
                ivaRete1 = summary.IvaRetention,
                ivaPerci1 = summary.IvaPerception,
                reteRenta = summary.IncomeRetention,
                montoTotalOperacion = summary.TotalOperation,
                totalLetras = ValorLetras.Convertir(summary.TotalToPay),
                condicionOperacion = summary.OperationCondition
            };
        }

        private object[]? MapRelatedDocs(List<CreditNoteRelatedDocRequestDTO>? relatedDocs)
        {
            if (relatedDocs == null || !relatedDocs.Any())
                return new object[0];

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
                return new SigningResult { Success = false, Error = ex.Message };
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
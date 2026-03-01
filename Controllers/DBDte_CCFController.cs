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
        private readonly IContingencyService _contingencyService;

        public DBDte_CCFController(
            dteContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHaciendaService haciendaService,
            IContingencyService contingencyService,
            IDTEDocumentService documentService,
            ILogger<DBDte_CCFController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _haciendaService = haciendaService;
            _contingencyService = contingencyService;
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// CREAR COMPROBANTE DE CRÉDITO FISCAL (CCF)
        /// Ejemplo de json para el body:
    ////    {
    ////        "clientId": 6,
    ////      "userId": 5,
    ////      "items": [
    ////        {
    ////            "type": 1,
    ////          "description": "Venta gravada",
    ////          "quantity": 1,
    ////          "unit_measure": 59,
    ////          "unit_price": 2000.00,
    ////          "taxed_sale": 2000.00,
    ////          "exempt_sale": 0,
    ////          "non_subject_sale": 0,
    ////          "taxes": [
    ////            "20"
    ////          ]
    ////}
    ////      ],
    ////      "receiver": {
    ////            "nrc": "3625871",
    ////        "nit": "06140912941505",
    ////        "name": "CLIENTE DE PRUEBA",
    ////        "commercial_name": "EJEMPLO S.A de S.V",
    ////        "activity_code": "62010",
    ////        "activity_description": "Programacion informatica",
    ////        "address": {
    ////                "department": "06",
    ////          "municipality": "22",
    ////          "complement": "Dirección de Prueba 1, N° 1234"
    ////        },
    ////        "phone": "21212828",
    ////        "email": "cliente@gmail.com"
    ////      },
    ////      "summary": {
    ////"operation_condition": 1,
    ////        "total_taxed": 2000.00,
    ////        "total_exempt": 0,
    ////        "total_non_taxed": 0,
    ////        "total_non_subject": 0,
    ////        "sub_total_sales": 2000.00,
    ////        "sub_total": 2000.00,
    ////        "iva_perception": 0,
    ////        "iva_retention": 0,
    ////        "income_retention": 0,
    ////        "total_operation": 2260.00,
    ////        "total_to_pay": 2260.00,
    ////        "taxes": [
    ////          {
    ////    "code": "20",
    ////            "description": "IVA 13%",
    ////            "value": 260.00
    ////          }
    ////        ],
    ////        "payment_types": [
    ////          {
    ////    "code": "01",
    ////            "amount": 2260.00
    ////          }
    ////        ]
    ////      },
    ////      "environment": "00",
    ////      "sendToHacienda": true
    ////    }
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateCCF([FromBody] CreateCCFRequestDTO request)
        {
            string? dteId = null;

            try
            {
                // 1. Obtenemos los datos del usuario emisor
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                    return BadRequest($"Usuario con ID {request.UserId} no encontrado");

                // 2. validar que la sucursal pertenezca al usuario
                var branchOffice = await _context.BranchOffices
                                   .Include(b => b.Addresses)  // Si tienes relación con direcciones
                                   .FirstOrDefaultAsync(b => b.Id == request.BranchOfficeId
                                    && b.UserId == user.Id
                                    && b.IsActive);

                if (branchOffice == null)
                    return BadRequest($"Sucursal con ID {request.BranchOfficeId} no encontrada o inactiva");

                // 3. obtener el firmador optimo para el usuario
                var optimalSigner = await GetOptimalSignerForUser(request.UserId);
                if (optimalSigner == null)
                    return BadRequest("No hay firmadores disponibles para el usuario");

                // 4. verificamos que el certificado exista
                var certificatePath = Path.Combine(optimalSigner.CertificatePath, $"{user.Nit}.crt");
                if (!System.IO.File.Exists(certificatePath))
                    return BadRequest($"Certificado no encontrado para NIT: {user.Nit}");

                // 5. generamos un numero de control interno valido
                var establishmentCode = branchOffice.EstablishmentCode ?? "";
                var posCode = branchOffice.PosCode ?? "";

                // 6. Obtener el siguiente número de secuencia
                // para el tipo de documento 01 (Factura)
                var sequenceNumber = await _documentService.GetNextSequenceNumber(
                    user.Id, "03", establishmentCode, posCode, request.Environment ?? "00");

                var controlNumber = $"DTE-03-{establishmentCode}{posCode}-{DateTime.Now.Year}{sequenceNumber:00000000000}";

                // 7. Generamos un DTE unico
                dteId = Guid.NewGuid().ToString().ToUpper();

                // 8.Contruir documento dete con datos de la sucursal, usuario y request
                // Crear el documento DTE como factura consumidor final
                var dteDocument = BuildCCFDocument(user, branchOffice, request, controlNumber, dteId, "03");

                // 9. Firmador de documento
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

                // 10. extraer JWT firmador
                var signedJWT = ExtractJWTFromSignerResponse(signResult.Response);
                if (string.IsNullOrEmpty(signedJWT))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error extrayendo JWT firmado"
                    });
                }

                // 11. Guardamos en la base de datatos datos antes de enviar 
                var saveRequest = new SaveDocumentRequest
                {
                    DteId = dteId,
                    UserId = user.Id,
                    DocumentType = "03", 
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

                // 12. Enviamos hacienda (Transmision) y actualizamos estado segun la repuesta
                HaciendaTransmissionResult? transmissionResult = null;
                string finalDocumentStatus = "FIRMADO";

                if (request.SendToHacienda != false)
                {
                    transmissionResult = await _haciendaService.TransmitDocument(
                        signedJWT, 
                        user.Nit, 
                        request.Environment ?? "00", 
                        "03",
                        3
                        );

                    if (transmissionResult != null)
                    {
                        if (transmissionResult.Success)
                        {
                            finalDocumentStatus = transmissionResult.Status ?? "PROCESADO";

                            await _documentService.UpdateDocumentStatus(
                                dteId, finalDocumentStatus, transmissionResult.ReceptionStamp,
                                null, null, transmissionResult.RawResponse, transmissionResult.ResponseCode);
                        }
                        else if (transmissionResult.Status == "RECHAZADO")
                        {
                            // Rechazado definitivo — no va a contingencia
                            finalDocumentStatus = "RECHAZADO";

                            await _documentService.UpdateDocumentStatus(
                                dteId, finalDocumentStatus, null,
                                transmissionResult.Error, transmissionResult.ErrorDetails,
                                transmissionResult.RawResponse, transmissionResult.ResponseCode);
                        }
                        else if (_contingencyService.ShouldGoToContingency(transmissionResult))
                        {
                           
                            finalDocumentStatus = "CONTINGENCIA";

                            await _contingencyService.StoreInContingency(
                                dteId: dteId,
                                userId: user.Id,
                                documentType: "03",          
                                signedJWT: signedJWT,
                                userNit: user.Nit,
                                ambiente: request.Environment ?? "",
                                version: 3,             
                                failedResult: transmissionResult);
                        }
                        else
                        {
                            
                            finalDocumentStatus = "ERROR_TRANSMISION";

                            await _documentService.UpdateDocumentStatus(
                                dteId, finalDocumentStatus, null,
                                transmissionResult.Error, transmissionResult.ErrorDetails,
                                transmissionResult.RawResponse, transmissionResult.ResponseCode);
                        }
                    }
                    else
                    {
                        
                        if (_contingencyService.ShouldGoToContingency(null))
                        {
                            finalDocumentStatus = "CONTINGENCIA";

                            await _contingencyService.StoreInContingency(
                                dteId, user.Id, "03", signedJWT,
                                user.Nit, request.Environment ?? "00", 3);
                        }
                        else
                        {
                            finalDocumentStatus = "ERROR_TRANSMISION";

                            await _documentService.UpdateDocumentStatus(
                                dteId, finalDocumentStatus, null,
                                "Error interno: transmissionResult es null", null, null, null);
                        }
                    }

                }

                // 13. Respuesta exitosa
                var response = new
                {
                    success = true,
                    message = "Factura procesada exitosamente",
                    data = new
                    {
                        documentId = documentId,
                        dteId = dteId,
                        numeroControl = controlNumber,
                        codigoGeneracion = dteId,
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
        /// LISTAR DOCUMENTOS CCF DE UN USUARIO
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
                {
                    parsedStartDate = start;
                }

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out DateTime end))
                {
                    parsedEndDate = end;
                }

                var documents = await _documentService.GetDocumentsByUser(userId, parsedStartDate, parsedEndDate, "03", ambiente);
                return Ok(documents);
            }
            catch (Exception ex)
            {
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

        private object BuildCCFDocument(User user, BranchOffice branchOffice, CreateCCFRequestDTO request, string controlNumber, string dteId, string tipoDocumento)
        {
            return new
            {
                //  IDENTIFICACION
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

                emisor = MapEmisorFromBranchOffice(user, branchOffice),
                receptor = MapCCFReceptor(request.Receiver),
                cuerpoDocumento = MapItems(request.Items),
                resumen = MapCCFResumen(request.Summary),

                documentoRelacionado = request.RelatedDocs != null && request.RelatedDocs.Any()
                    ? MapRelatedDocs(request.RelatedDocs)
                    : null,
                ventaTercero = request.ThirdPartySale != null ? new
                {
                    nit = request.ThirdPartySale.Nit,
                    nombre = request.ThirdPartySale.Name
                } : null,
                otrosDocumentos = (object?)null,
                extension = (object?)null,
                apendice = (object?)null
            };
        }

        private object MapEmisorFromBranchOffice(User user, BranchOffice branchOffice)
        {
            var address = branchOffice.Addresses?.FirstOrDefault();
            if (address == null)
            {
                throw new InvalidOperationException($"La sucursal {branchOffice.Id} no tiene dirección configurada");
            }
            if (string.IsNullOrEmpty(branchOffice.EstablishmentCode))
            {
                throw new InvalidOperationException($"La sucursal {branchOffice.Id} no tiene código de establecimiento configurado");
            }

            if (string.IsNullOrEmpty(branchOffice.PosCode))
            {
                throw new InvalidOperationException($"La sucursal {branchOffice.Id} no tiene código de punto de venta configurado");
            }

            return new
            {
                nit = user.Nit,
                nrc = user.Nrc,
                nombre = user.BusinessName ?? user.CommercialName,
                codActividad = user.EconomicActivity,
                descActividad = user.EconomicActivityDesc,
                nombreComercial = user.CommercialName,
                tipoEstablecimiento = branchOffice.EstablishmentType,
                codEstable = branchOffice.EstablishmentCode,
                codPuntoVenta = branchOffice.PosCode,
                direccion = new
                {
                    departamento = address.Department,
                    municipio = address.Municipality,
                    complemento = address.Address1 + (string.IsNullOrEmpty(address.Complement) ? "" : ", " + address.Complement)
                },

                telefono = branchOffice.Phone ?? user.Phone,
                correo = branchOffice.Email ?? user.Email,
                codEstableMH = branchOffice.EstablishmentCodeMh ?? branchOffice.EstablishmentCode,
                codPuntoVentaMH = branchOffice.PosCodeMh ?? branchOffice.PosCode
            };
        }

        private object MapCCFReceptor(CCFReceiverRequestDTO? receiver)
        {
            if (receiver == null) return null;

            return new
            {
                nombre = receiver.Name,
                nit = receiver.Nit,
                nrc = receiver.Nrc,
                codActividad = receiver.ActivityCode,
                descActividad = receiver.ActivityDescription,
                nombreComercial = receiver.Name,
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

        private object[] MapItems(List<CCFItemRequestDTO> items)
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
                totalLetras = ValorLetras.Convertir(summary.TotalToPay),
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

        #endregion
    }
}
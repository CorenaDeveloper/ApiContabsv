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
    public class DBDte_InvoiceController : ControllerBase
    {
        private readonly dteContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHaciendaService _haciendaService;
        private readonly IDTEDocumentService _documentService;
        private readonly ILogger<DBDte_InvoiceController> _logger;

        public DBDte_InvoiceController(
            dteContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHaciendaService haciendaService,
            IDTEDocumentService documentService,
            ILogger<DBDte_InvoiceController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _haciendaService = haciendaService;
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// CREAR FACTURA ELECTRONICA (PROCESO COMPLETO)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateInvoice([FromBody] CreateInvoiceRequestDTO request)
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

                // 4. GENERAR NÚMERO DE CONTROL ÚNICO
                var sequenceNumber = await _documentService.GetNextSequenceNumber(
                    user.Id, "01", "0001", "001");
                var controlNumber = $"DTE-01-{user.Nit}-{sequenceNumber:000000000}";

                // 5. GENERAR DTE ID ÚNICO
                dteId = Guid.NewGuid().ToString().ToUpper();

                // 6. CONSTRUIR DOCUMENTO DTE
                var dteDocument = new
                {
                    version = 1,
                    ambiente = request.Environment ?? "00", // Desde request o default pruebas
                    tipoDte = "01", // factura
                    numeroControl = controlNumber,
                    codigoGeneracion = dteId,
                    tipoModelo = request.ModelType ?? 1,
                    tipoOperacion = 1,
                    fecEmi = DateTime.Now.ToString("yyyy-MM-dd"),
                    horEmi = DateTime.Now.ToString("HH:mm:ss"),
                    tipoMoneda = "USD",
                    emisor = MapEmisorFromUser(user),
                    receptor = MapReceptor(request.Receiver),
                    cuerpoDocumento = MapItems(request.Items),
                    resumen = MapResumen(request.Summary),
                    ventaTercero = request.ThirdPartySale,
                    documentoRelacionado = request.RelatedDocs,
                    otrosDocumentos = request.OtherDocs,
                    apendice = request.Appendixes
                };

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
                    DocumentType = "01",
                    GenerationType = 1,
                    ControlNumber = controlNumber,
                    TotalAmount = request.Summary?.TotalToPay ?? 0,
                    Status = "FIRMADO",
                    JsonContent = JsonSerializer.Serialize(dteDocument),
                    EstablishmentCode = "0001",
                    PosCode = "001"
                };

                var documentId = await _documentService.SaveDocument(saveRequest);

                // 10. ENVIAR A HACIENDA (opcional, puede fallar sin afectar el guardado)
                HaciendaTransmissionResult? transmissionResult = null;

                if (request.SendToHacienda != false) // Por default enviar
                {
                    transmissionResult = await _haciendaService.TransmitDocument(
                        signedJWT, user.Nit, dteDocument.ambiente.ToString(), "01"); // "01" = Factura

                    if (transmissionResult.Success)
                    {
                        // Actualizar estado en BD
                        await _documentService.UpdateDocumentStatus(
                            dteId,
                            transmissionResult.Status ?? "PROCESADO",
                            transmissionResult.ReceptionStamp);
                    }
                    else
                    {
                        // Log error pero no fallar toda la operación
                        _logger.LogWarning("Error transmitiendo a Hacienda: {Error}", transmissionResult.Error);
                    }
                }

                // 11. RESPUESTA EXITOSA
                var haciendaInfo = new
                {
                    sent = transmissionResult != null,
                    success = transmissionResult?.Success ?? false,
                    status = transmissionResult?.Status,
                    receptionStamp = transmissionResult?.ReceptionStamp,
                    error = transmissionResult?.Error
                };

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
                        hacienda = haciendaInfo,
                        document = new
                        {
                            status = transmissionResult?.Success == true ? transmissionResult.Status : "FIRMADO",
                            createdAt = DateTime.Now,
                            totalAmount = saveRequest.TotalAmount
                        }
                    }
                };

                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando factura para usuario {UserId}", request.ClientId);

                // Si tenemos un dteId y falló después del guardado, marcar como error
                if (!string.IsNullOrEmpty(dteId))
                {
                    try
                    {
                        await _documentService.UpdateDocumentStatus(dteId, "ERROR");
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, "Error actualizando estado de error para documento {DteId}", dteId);
                    }
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno procesando factura",
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
        /// LISTAR DOCUMENTOS DE UN USUARIO
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

                // ✅ USAR JWT_SECRET DEL USUARIO
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
                direccion = new
                {
                    departamento = "06",
                    municipio = "20",
                    complemento = "San Salvador"
                },
                telefono = user.Phone,
                correo = user.Email,
                codEstableMH = "0001",
                codPuntoVentaMH = "001"
            };
        }

        private object MapReceptor(ReceiverRequestDTO? receiver)
        {
            if (receiver == null) return null;

            return new
            {
                nombre = receiver.Name,
                tipoDocumento = receiver.DocumentType,
                numDocumento = receiver.DocumentNumber,
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

        private object MapItems(List<InvoiceItemRequestDTO> items)
        {
            return items.Select((item, index) => new
            {
                numItem = index + 1,
                tipoItem = item.Type,
                descripcion = item.Description,
                cantidad = item.Quantity,
                uniMedida = item.UnitMeasure,
                precioUni = item.UnitPrice,
                montoDescu = item.Discount,
                codigo = item.Code,
                ventaNoSuj = item.NonSubjectSale,
                ventaExenta = item.ExemptSale,
                ventaGravada = item.TaxedSale,
                psv = item.SuggestedPrice,
                noGravado = item.NonTaxed,
                ivaItem = item.IvaItem
            }).ToArray();
        }

        private object MapResumen(InvoiceSummaryRequestDTO? summary)
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
                totalNoGravado = summary.TotalNonTaxed,
                totalPagar = summary.TotalToPay,
                condicionOperacion = summary.OperationCondition,
                ivaRete1 = summary.IvaRetention,
                totalIva = summary.TotalIva,
                pagos = summary.PaymentTypes?.Select(p => new
                {
                    codigo = p.Code,
                    montoPago = p.Amount
                }).ToArray()
            };
        }

        #endregion
    }

    // DTOs adicionales
    public class CreateInvoiceRequestDTO
    {
        public int ClientId { get; set; }
        public int UserId { get; set; }
        public List<InvoiceItemRequestDTO> Items { get; set; } = new();
        public ReceiverRequestDTO? Receiver { get; set; }
        public int? ModelType { get; set; }
        public InvoiceSummaryRequestDTO? Summary { get; set; }
        public string? CertificatePassword { get; set; }
        public string? Environment { get; set; } // "00" = pruebas, "01" = producción
        public bool? SendToHacienda { get; set; } = true;

        public object? ThirdPartySale { get; set; }
        public object[]? RelatedDocs { get; set; }
        public object[]? OtherDocs { get; set; }
        public object[]? Appendixes { get; set; }
    }

    public class SigningResult
    {
        public bool Success { get; set; }
        public string Response { get; set; } = "";
        public string? Error { get; set; }
    }

    // Resto de DTOs existentes...
    public class InvoiceItemRequestDTO
    {
        public int Type { get; set; }
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public int UnitMeasure { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public string? Code { get; set; }
        public decimal NonSubjectSale { get; set; }
        public decimal ExemptSale { get; set; }
        public decimal TaxedSale { get; set; }
        public decimal SuggestedPrice { get; set; }
        public decimal NonTaxed { get; set; }
        public decimal IvaItem { get; set; }
    }

    public class ReceiverRequestDTO
    {
        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public string? Name { get; set; }
        public AddressRequestDTO? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class AddressRequestDTO
    {
        public string Department { get; set; } = "";
        public string Municipality { get; set; } = "";
        public string Complement { get; set; } = "";
    }

    public class InvoiceSummaryRequestDTO
    {
        public decimal TotalNonSubject { get; set; }
        public decimal TotalExempt { get; set; }
        public decimal TotalTaxed { get; set; }
        public decimal SubTotal { get; set; }
        public decimal NonSubjectDiscount { get; set; }
        public decimal ExemptDiscount { get; set; }
        public decimal TaxedDiscount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal SubTotalSales { get; set; }
        public decimal TotalOperation { get; set; }
        public decimal TotalNonTaxed { get; set; }
        public decimal TotalToPay { get; set; }
        public int OperationCondition { get; set; }
        public decimal IvaRetention { get; set; }
        public decimal TotalIva { get; set; }
        public List<PaymentTypeRequestDTO>? PaymentTypes { get; set; }
    }

    public class PaymentTypeRequestDTO
    {
        public string Code { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
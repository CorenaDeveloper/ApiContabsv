using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Dte;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_InvoiceController : ControllerBase
    {
        private readonly dteContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public DBDte_InvoiceController(dteContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// CREAR FACTURA ELECTRONICA (ESTRUCTURA GO API)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> CreateInvoice([FromBody] CreateInvoiceRequestDTO request)
        {
            try
            {
                // 1. OBTENER DATOS DEL USER (EMISOR) POR client_id
                var user = await _context.Users.FindAsync(request.ClientId);
                if (user == null)
                    return BadRequest($"Usuario con ID {request.ClientId} no encontrado");

                // 2. OBTENER FIRMADOR ÓPTIMO PARA EL USUARIO
                var optimalSigner = await GetOptimalSignerForUser(request.UserId);
                if (optimalSigner == null)
                    return BadRequest("No hay firmadores disponibles para el usuario");

                // 3. BUSCAR CERTIFICADO DEL USER/EMISOR POR NIT
                var certificatePath = Path.Combine(optimalSigner.CertificatePath, $"{user.Nit}.crt");
                if (!System.IO.File.Exists(certificatePath))
                    return BadRequest($"Certificado no encontrado para NIT: {user.Nit}");

                // 4. CONSTRUIR DOCUMENTO DTE (IGUAL QUE GO API)
                var dteDocument = new
                {
                    version = 1,
                    ambiente = "00", // producción 01 - pruebas 00
                    tipoDte = "01", // factura
                    numeroControl = GenerateControlNumber(user.Nit),
                    codigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                    tipoModelo = request.ModelType ?? 1,
                    tipoOperacion = 1,
                    fecEmi = DateTime.Now.ToString("yyyy-MM-dd"),
                    horEmi = DateTime.Now.ToString("HH:mm:ss"),
                    tipoMoneda = "USD",
                    emisor = MapEmisorFromUser(user),
                    receptor = MapReceptor(request.Receiver),
                    cuerpoDocumento = MapItems(request.Items),
                    resumen = MapResumen(request.Summary),
                    // Campos opcionales
                    ventaTercero = request.ThirdPartySale,
                    documentoRelacionado = request.RelatedDocs,
                    otrosDocumentos = request.OtherDocs,
                    apendice = request.Appendixes
                };

                // 5. ENVIAR AL FIRMADOR (FORMATO EXACTO COMO TEST-SIGNING QUE FUNCIONA)
                var firmingRequest = new
                {
                    nit = user.Nit,
                    activo = true,              // ✅ Correcto
                    passwordPri = user.PasswordPri,  // ✅ Correcto  
                    dteJson = dteDocument       // ✅ CORREGIR: era "jsonDTE" debe ser "dteJson"
                };

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(firmingRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(optimalSigner.EndpointUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // 6. MANEJAR RESPUESTA
                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Factura firmada exitosamente",
                        numeroControl = dteDocument.numeroControl,
                        codigoGeneracion = dteDocument.codigoGeneracion,
                        signer = optimalSigner.SignerName,
                        signedDocument = responseContent
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error al firmar factura",
                        error = responseContent
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno",
                    error = ex.Message
                });
            }
        }

        #region Helper Methods

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

        private string GenerateControlNumber(string nitEmisor)
        {
            // Formato: DTE-01-NNNNNNNN-000000001
            var sequence = GetNextSequenceNumber(nitEmisor);
            return $"DTE-01-{nitEmisor}-{sequence:000000000}";
        }

        private int GetNextSequenceNumber(string nitEmisor)
        {
            // Implementar lógica de secuencia
            return new Random().Next(1, 999999);
        }

        private object MapEmisorFromUser(dynamic user)
        {
            return new
            {
                nit = user.Nit,  // ✅ Campo 'nit' de tabla users
                nrc = user.Nrc,  // ✅ Campo 'nrc' de tabla users
                nombre = user.BusinessName ?? user.CommercialName, // business_name o commercial_name
                codActividad = user.EconomicActivity, // economic_activity
                descActividad = user.EconomicActivityDesc, // economic_activity_desc
                nombreComercial = user.CommercialName, // commercial_name
                tipoEstablecimiento = "02",
                direccion = new
                {
                    departamento = "06", // Hardcodeado por ahora - agregar a tabla users si necesitas
                    municipio = "20",    // Hardcodeado por ahora
                    complemento = "San Salvador" // Hardcodeado por ahora
                },
                telefono = user.Phone, // phone
                correo = user.Email,   // email
                codEstableMH = "0001", // Hardcodeado por ahora
                codPuntoVentaMH = "001"  // Hardcodeado por ahora
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
}
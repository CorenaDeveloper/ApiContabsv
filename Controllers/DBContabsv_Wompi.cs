using ApiContabsv.Models.Contabsv;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_Wompi : Controller
    {
        private readonly ContabsvContext _context;
        private readonly WompiService _wompiService;

        public DBContabsv_Wompi(ContabsvContext context, WompiService wompiService)
        {
            _context = context;
            _wompiService = wompiService;
        }

        [HttpPost("CrearTransaccion")]
        public async Task<ActionResult> CrearTransaccion([FromBody] CrearTransaccionRequest request)
        {
            try
            {
                var suscripcion = await _context.Suscripciones
                    .Include(s => s.SuscripcionDetalles)
                    .Include(s => s.IdClienteNavigation)
                    .FirstOrDefaultAsync(s => s.IdSuscripcion == request.IdSuscripcion);

                if (suscripcion == null)
                    return NotFound("Suscripción no encontrada");

                var monto = suscripcion.SuscripcionDetalles
                            .Where(d => d.Activo == true)
                            .Sum(d => d.PrecioUnitario);

                var cliente = suscripcion.IdClienteNavigation;
                var descripcion = $"Pago suscripción ContabSV - {cliente.NombreComercial ?? cliente.NombreRazonSocial}";

                var result = await _wompiService.CreateTransaction(
                    monto,
                    descripcion,
                    cliente.Correo ?? "cliente@contabsv.com",
                    request.RedirectUrl
                );

                if (result.Success)
                {
                    // ✅ NO guardar pago aquí - el webhook lo hace cuando se confirme
                    return Ok(new
                    {
                        success = true,
                        transactionId = result.TransactionId,
                        checkoutUrl = result.CheckoutUrl,
                        referencia = result.Referencia,
                        idSuscripcion = request.IdSuscripcion
                    });
                }
                else
                {
                    return StatusCode(500, result.Error);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPost("VerificarPago")]
        public async Task<ActionResult> VerificarPago([FromBody] VerificarPagoRequest request)
        {
            try
            {
                var suscripcion = await _context.Suscripciones
                    .Include(s => s.SuscripcionDetalles)
                    .FirstOrDefaultAsync(s => s.IdSuscripcion == request.IdSuscripcion);

                if (suscripcion == null)
                    return NotFound("Suscripción no encontrada");

                var monto = suscripcion.SuscripcionDetalles
                    .Where(d => d.Activo == true)
                    .Sum(d => d.PrecioUnitario);

                // Verificar si ya existe un pago para este enlace
                var pagoExistente = await _context.HistorialPagos
                    .FirstOrDefaultAsync(p => p.PaypalPaymentId == request.TransactionId);

                if (pagoExistente != null)
                {
                    return Ok(new
                    {
                        success = true,
                        message = pagoExistente.EstadoPago == "completado"
                            ? "Pago confirmado exitosamente"
                            : "Pago en proceso de confirmación",
                        estado = pagoExistente.EstadoPago,
                        transactionId = request.TransactionId,
                        idPago = pagoExistente.IdPago
                    });
                }

                var pago = new HistorialPago
                {
                    IdSuscripcion = request.IdSuscripcion,
                    IdCliente = suscripcion.IdCliente,
                    FechaPago = DateTime.Now,
                    Monto = monto,
                    MetodoPago = "wompi",
                    EstadoPago = "pendiente",
                    PaypalPaymentId = request.TransactionId,
                    DetallePago = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        detalle = suscripcion.SuscripcionDetalles
                            .Where(d => d.Activo == true)
                            .Select(d => new { concepto = d.Concepto, monto = d.PrecioUnitario }),
                        total = monto
                    }),
                    FechaCreacion = DateTime.Now
                };

                _context.HistorialPagos.Add(pago);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Pago registrado, esperando confirmación",
                    estado = "pendiente",
                    transactionId = request.TransactionId,
                    idPago = pago.IdPago
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPost("Webhook")]
        [AllowAnonymous]
        public async Task<ActionResult> RecibirWebhookWompi([FromBody] JsonElement webhookData)
        {
            try
            {
                // Validar firma HMAC
                var bodyRaw = webhookData.GetRawText();
                var wompiHash = Request.Headers["wompi_hash"].ToString();
                if (!string.IsNullOrEmpty(wompiHash))
                {
                    var hashCalculado = EncriptarData(bodyRaw, _wompiService.ApiSecret);
                    if (hashCalculado != wompiHash)
                        return Unauthorized(new { mensaje = "Hash inválido" });
                }

                // Leer campos correctos de Wompi SV
                var resultado = webhookData.TryGetProperty("ResultadoTransaccion", out var resEl)
                    ? resEl.GetString() : "";

                if (resultado != "ExitosaAprobada")
                    return Ok(new { mensaje = "Pago no aprobado, ignorado" });

                var idTransaccion = webhookData.TryGetProperty("IdTransaccion", out var trEl)
                    ? trEl.GetString() : "";

                // IdentificadorEnlaceComercio = la referencia que TÚ mandaste al crear el enlace
                var referencia = "";
                if (webhookData.TryGetProperty("EnlacePago", out var enlaceEl))
                    referencia = enlaceEl.TryGetProperty("IdentificadorEnlaceComercio", out var refEl)
                        ? refEl.GetString() : "";

                // Buscar pago pendiente — primero por referencia (tu clave), luego por IdTransaccion
                var pagoPendiente = await _context.HistorialPagos
                    .FirstOrDefaultAsync(p =>
                        (p.PaypalPaymentId == referencia || p.PaypalPaymentId == idTransaccion)
                        && p.EstadoPago == "pendiente");

                if (pagoPendiente == null)
                {
                    Console.WriteLine($"WEBHOOK: No se encontró pago pendiente para referencia={referencia}");
                    return Ok(new { mensaje = "Sin pago pendiente, ignorado" });
                }

                // Confirmar pago y extender suscripción
                pagoPendiente.EstadoPago = "completado";
                pagoPendiente.FechaPago = DateTime.Now;

                var suscripcion = await _context.Suscripciones
                    .Include(s => s.SuscripcionDetalles)
                    .FirstOrDefaultAsync(s => s.IdSuscripcion == pagoPendiente.IdSuscripcion);

                if (suscripcion != null)
                {
                    foreach (var detalle in suscripcion.SuscripcionDetalles.Where(d => d.Activo == true))
                    {
                        if (detalle.TipoCobro == "mensual")
                            detalle.FechaVencimiento = detalle.FechaVencimiento.AddMonths(1);
                        else if (detalle.TipoCobro == "anual")
                            detalle.FechaVencimiento = detalle.FechaVencimiento.AddYears(1);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Webhook procesado correctamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WEBHOOK ERROR: {ex.Message}");
                return Ok(new { mensaje = "Procesado" });
            }
        }

        private static string EncriptarData(string body, string secret)
        {
            var encoding = new System.Text.UTF8Encoding();
            var hash = new System.Security.Cryptography.HMACSHA256(encoding.GetBytes(secret));
            byte[] stream = hash.ComputeHash(encoding.GetBytes(body));
            string textHash = string.Concat(stream.Select(b => b.ToString("x2")));
            hash.Dispose();
            return textHash;
        }
    }

    public class CrearTransaccionRequest
    {
        public int IdSuscripcion { get; set; }
        public string RedirectUrl { get; set; }
    }

    public class VerificarPagoRequest
    {
        public string TransactionId { get; set; }
        public int IdSuscripcion { get; set; }
    }
}
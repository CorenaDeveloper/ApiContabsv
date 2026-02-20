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

                // Calcular monto total mensual
                var monto = suscripcion.SuscripcionDetalles
                    .Where(d => d.Activo == true && d.TipoCobro == "mensual")
                    .Sum(d => d.PrecioUnitario);

                var cliente = suscripcion.IdClienteNavigation;
                var descripcion = $"Pago suscripción ContabSV - {cliente.NombreComercial ?? cliente.NombreComercial}";

                var result = await _wompiService.CreateTransaction(
                    monto,
                    descripcion,
                    cliente.Correo ?? "cliente@contabsv.com",
                    request.RedirectUrl
                );

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        transactionId = result.TransactionId,
                        checkoutUrl = result.CheckoutUrl,
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
                    .Where(d => d.Activo == true && d.TipoCobro == "mensual")
                    .Sum(d => d.PrecioUnitario);

                // Registrar pago como "pendiente" hasta que llegue el webhook
                var pago = new HistorialPago
                {
                    IdSuscripcion = request.IdSuscripcion,
                    IdCliente = suscripcion.IdCliente,
                    FechaPago = DateTime.Now,
                    Monto = monto,
                    MetodoPago = "wompi",
                    EstadoPago = "pendiente", // ✅ Cambiar a "pendiente"
                    PaypalPaymentId = request.TransactionId,
                    DetallePago = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        detalle = suscripcion.SuscripcionDetalles
                            .Where(d => d.Activo == true && d.TipoCobro == "mensual")
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
                    message = "Pago en proceso de confirmación",
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
        [AllowAnonymous] // ⚠️ Wompi no envía autenticación
        public async Task<ActionResult> RecibirWebhookWompi([FromBody] JsonElement webhookData)
        {
            try
            {
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine("🔔 WEBHOOK RECIBIDO DE WOMPI");
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine(webhookData.ToString());
                Console.WriteLine("═══════════════════════════════════════");

                // Extraer datos del webhook
                var idEnlace = webhookData.GetProperty("idEnlace").GetInt32();
                var identificadorEnlaceComercio = webhookData.GetProperty("identificadorEnlaceComercio").GetString();
                var esProductiva = webhookData.GetProperty("esProductiva").GetBoolean();
                var esAprobada = webhookData.GetProperty("esAprobada").GetBoolean();
                var monto = webhookData.GetProperty("monto").GetDecimal();
                var idTransaccion = webhookData.GetProperty("idTransaccion").GetInt32();

                Console.WriteLine($"✅ Pago aprobado: {esAprobada}, Monto: ${monto}");

                // Solo procesar si es pago aprobado
                if (esAprobada)
                {
                    // Extraer idSuscripcion del identificadorEnlaceComercio
                    // Formato: "CONTABSV-{timestamp}-{idSuscripcion}"
                    // O mejor: guardar en un diccionario temporal al crear el enlace

                    // Por ahora, buscar pago pendiente con este idEnlace
                    var pagoPendiente = await _context.HistorialPagos
                        .FirstOrDefaultAsync(p => p.PaypalPaymentId == idEnlace.ToString()
                                               && p.EstadoPago == "pendiente");

                    if (pagoPendiente != null)
                    {
                        // Actualizar estado del pago
                        pagoPendiente.EstadoPago = "completado";
                        pagoPendiente.FechaPago = DateTime.Now;

                        // Actualizar suscripción
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
                        Console.WriteLine("✅ Pago confirmado y suscripción actualizada");
                    }
                }

                // ⚠️ SIEMPRE devolver 200 OK para que Wompi no reintente
                return Ok(new { mensaje = "Webhook procesado" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error procesando webhook: {ex.Message}");
                // ⚠️ SIEMPRE devolver 200 OK incluso con error
                return Ok(new { mensaje = "Error procesado" });
            }
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
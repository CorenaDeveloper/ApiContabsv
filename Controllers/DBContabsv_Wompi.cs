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
                var idEnlace = webhookData.GetProperty("idEnlace").GetInt32();
                var esAprobada = webhookData.GetProperty("esAprobada").GetBoolean();

                if (!esAprobada)
                {
                    return Ok(new { mensaje = "Pago no aprobado" });
                }

                var pagoPendiente = await _context.HistorialPagos
                    .FirstOrDefaultAsync(p => p.PaypalPaymentId == idEnlace.ToString()
                                           && p.EstadoPago == "pendiente");

                if (pagoPendiente != null)
                {
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
                }

                return Ok(new { mensaje = "Webhook procesado" });
            }
            catch (Exception ex)
            {
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
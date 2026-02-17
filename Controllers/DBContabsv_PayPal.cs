using ApiContabsv.Models.Contabsv;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_PayPal : Controller
    {
        private readonly ContabsvContext _context;
        private readonly PayPalService _paypalService;

        public DBContabsv_PayPal(ContabsvContext context, PayPalService paypalService)
        {
            _context = context;
            _paypalService = paypalService;
        }

        /// <summary>
        /// Crear orden de pago en PayPal para una suscripción
        /// Solo crea la orden, NO registra pago hasta que se capture
        /// </summary>
        [HttpPost("CrearOrden")]
        public async Task<ActionResult> CrearOrden([FromBody] CrearOrdenRequest request)
        {
            try
            {
                var suscripcion = await _context.Suscripciones
                    .Include(s => s.SuscripcionDetalles)
                    .FirstOrDefaultAsync(s => s.IdSuscripcion == request.IdSuscripcion);

                if (suscripcion == null)
                    return NotFound("Suscripción no encontrada.");

                var detalleActivo = suscripcion.SuscripcionDetalles
                    .Where(d => d.Activo == true)
                    .ToList();

                decimal monto = detalleActivo.Sum(d => d.PrecioUnitario);

                if (monto <= 0)
                    return BadRequest("El monto a cobrar es $0.00");

                var conceptos = string.Join(", ", detalleActivo.Select(d => d.Concepto));
                var descripcion = $"Pago suscripción ContabSV - {conceptos}";
                if (descripcion.Length > 127) descripcion = descripcion.Substring(0, 127);

                var result = await _paypalService.CreateOrder(
                    monto: monto,
                    descripcion: descripcion,
                    referencia: $"SUSC-{suscripcion.IdSuscripcion}",
                    returnUrl: request.ReturnUrl,
                    cancelUrl: request.CancelUrl
                );

                if (!result.Success)
                    return StatusCode(500, result.Error);

                // Solo devolvemos la orden, NO registramos pago aún
                return Ok(new
                {
                    success = true,
                    orderId = result.OrderId,
                    approvalUrl = result.ApprovalUrl,
                    idSuscripcion = suscripcion.IdSuscripcion,
                    monto = monto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Capturar pago después de que el usuario aprueba en PayPal
        /// AQUÍ se registra el pago y se renuevan las fechas
        /// </summary>
        [HttpPost("CapturarPago")]
        public async Task<ActionResult> CapturarPago([FromBody] CapturarPagoRequest request)
        {
            try
            {
                // Capturar el pago en PayPal
                var result = await _paypalService.CaptureOrder(request.OrderId);

                if (!result.Success)
                    return StatusCode(500, result.Error);

                // Obtener suscripción con detalle
                var suscripcion = await _context.Suscripciones
                    .Include(s => s.SuscripcionDetalles)
                    .FirstOrDefaultAsync(s => s.IdSuscripcion == request.IdSuscripcion);

                if (suscripcion == null)
                    return NotFound("Suscripción no encontrada.");

                var detalleActivo = suscripcion.SuscripcionDetalles
                    .Where(d => d.Activo == true)
                    .ToList();

                decimal monto = detalleActivo.Sum(d => d.PrecioUnitario);

                // Generar JSON del desglose
                var detallePagoJson = JsonSerializer.Serialize(new
                {
                    detalle = detalleActivo.Select(d => new { d.Concepto, monto = d.PrecioUnitario }),
                    total = monto
                });

                // Registrar pago COMPLETADO en historial
                var pago = new HistorialPago
                {
                    IdSuscripcion = suscripcion.IdSuscripcion,
                    IdCliente = suscripcion.IdCliente,
                    FechaPago = DateTime.Now,
                    Monto = monto,
                    MetodoPago = "paypal",
                    EstadoPago = "completado",
                    DetallePago = detallePagoJson,
                    PaypalPaymentId = result.PaymentId,
                    FechaCreacion = DateTime.Now
                };

                _context.HistorialPagos.Add(pago);

                // Renovar fechas de vencimiento
                suscripcion.FechaModificacion = DateTime.Now;

                foreach (var detalle in detalleActivo)
                {
                    switch (detalle.TipoCobro)
                    {
                        case "mensual":
                            detalle.FechaInicio = DateTime.Now;
                            detalle.FechaVencimiento = DateTime.Now.AddMonths(1);
                            break;
                        case "anual":
                            detalle.FechaInicio = DateTime.Now;
                            detalle.FechaVencimiento = DateTime.Now.AddYears(1);
                            break;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    status = result.Status,
                    paymentId = result.PaymentId,
                    payerId = result.PayerId,
                    idPago = pago.IdPago,
                    message = "Pago completado exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("EstadoOrden/{orderId}")]
        public async Task<ActionResult> EstadoOrden(string orderId)
        {
            try
            {
                var status = await _paypalService.GetOrderStatus(orderId);
                return Ok(new { orderId, status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }

    public class CrearOrdenRequest
    {
        public int IdSuscripcion { get; set; }
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
    }

    public class CapturarPagoRequest
    {
        public string OrderId { get; set; }
        public int IdSuscripcion { get; set; }
    }
}
using ApiContabsv.Models.Contabsv;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                var status = await _wompiService.GetTransactionStatus(request.TransactionId);

                if (status.Status == "APPROVED" || status.Status == "SUCCESS")
                {
                    var suscripcion = await _context.Suscripciones
                        .Include(s => s.SuscripcionDetalles)
                        .FirstOrDefaultAsync(s => s.IdSuscripcion == request.IdSuscripcion);

                    if (suscripcion == null)
                        return NotFound("Suscripción no encontrada");

                    // Calcular monto total
                    var monto = suscripcion.SuscripcionDetalles
                        .Where(d => d.Activo == true && d.TipoCobro == "mensual")
                        .Sum(d => d.PrecioUnitario);

                    // Registrar en HistorialPagos
                    var pago = new HistorialPago
                    {
                        IdSuscripcion = request.IdSuscripcion,
                        IdCliente = suscripcion.IdCliente,  // ✅ Agregar IdCliente también
                        FechaPago = DateTime.Now,
                        Monto = monto,
                        MetodoPago = "wompi",
                        EstadoPago = "completado",
                        PaypalPaymentId = request.TransactionId,  // ✅ Usar este campo para el ID de Wompi
                        DetallePago = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            detalle = suscripcion.SuscripcionDetalles
                                .Where(d => d.Activo == true && d.TipoCobro == "mensual")
                                .Select(d => new
                                {
                                    concepto = d.Concepto,
                                    monto = d.PrecioUnitario
                                }),
                            total = monto
                        }),
                        FechaCreacion = DateTime.Now  // ✅ Opcional pero buena práctica
                    };

                    _context.HistorialPagos.Add(pago);

                    // Actualizar fechas de vencimiento
                    foreach (var detalle in suscripcion.SuscripcionDetalles.Where(d => d.Activo == true))
                    {
                        if (detalle.TipoCobro == "mensual")
                            detalle.FechaVencimiento = detalle.FechaVencimiento.AddMonths(1);
                        else if (detalle.TipoCobro == "anual")
                            detalle.FechaVencimiento = detalle.FechaVencimiento.AddYears(1);
                    }

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = "Pago registrado exitosamente",
                        transactionId = request.TransactionId,
                        idPago = pago.IdPago
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Pago no completado. Estado: {status.Status}"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
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
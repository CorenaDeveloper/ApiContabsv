using ApiContabsv.Models.Contabsv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_SuscripcionesController : Controller
    {
        private readonly ContabsvContext _context;

        public DBContabsv_SuscripcionesController(ContabsvContext context)
        {
            _context = context;
        }


        // =============================================
        // SUSCRIPCIONES
        // =============================================

        /// <summary>
        /// Obtener suscripción con detalle completo por idCliente
        /// </summary>
        [HttpGet("Suscripciones")]
        public async Task<ActionResult> GetSuscripciones([FromQuery] int? idCliente)
        {
            try
            {
                var query = _context.Suscripciones.AsQueryable();

                if (idCliente.HasValue)
                {
                    query = query.Where(s => s.IdCliente == idCliente);
                }

                var lista = await query
                    .Select(s => new
                    {
                        s.IdSuscripcion,
                        s.IdCliente,
                        cliente = _context.Clientes
                            .Where(c => c.IdCliente == s.IdCliente)
                            .Select(c => c.Nombres + " " + c.Apellidos)
                            .FirstOrDefault(),
                        correo = _context.Clientes
                            .Where(c => c.IdCliente == s.IdCliente)
                            .Select(c => c.Correo)
                            .FirstOrDefault(),
                        s.EstadoSuscripcion,
                        s.FechaInicio,
                        s.PaypalSubscriptionId,
                        s.FechaCreacion,
                        s.FechaModificacion,
                        detalle = s.SuscripcionDetalles
                            .Where(d => d.Activo == true)
                            .Select(d => new
                            {
                                d.IdDetalle,
                                d.Concepto,
                                d.PrecioUnitario,
                                d.TipoCobro,
                                d.FechaInicio,
                                d.FechaVencimiento,
                                d.Activo
                            }).ToList(),
                        totalMensual = s.SuscripcionDetalles
                            .Where(d => d.Activo == true && d.TipoCobro == "mensual")
                            .Sum(d => d.PrecioUnitario),
                        totalAnual = s.SuscripcionDetalles
                            .Where(d => d.Activo == true && d.TipoCobro == "anual")
                            .Sum(d => d.PrecioUnitario),
                        cantidadItems = s.SuscripcionDetalles
                            .Count(d => d.Activo == true)
                    })
                    .ToListAsync();

                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtener suscripción por id
        /// </summary>
        [HttpGet("Suscripciones/{id}")]
        public async Task<ActionResult> GetSuscripcion(int id)
        {
            try
            {
                var suscripcion = await _context.Suscripciones
                    .Where(s => s.IdSuscripcion == id)
                    .Select(s => new
                    {
                        s.IdSuscripcion,
                        s.IdCliente,
                        cliente = _context.Clientes
                            .Where(c => c.IdCliente == s.IdCliente)
                            .Select(c => c.Nombres + " " + c.Apellidos)
                            .FirstOrDefault(),
                        s.EstadoSuscripcion,
                        s.FechaInicio,
                        s.PaypalSubscriptionId,
                        detalle = s.SuscripcionDetalles
                            .Select(d => new
                            {
                                d.IdDetalle,
                                d.Concepto,
                                d.PrecioUnitario,
                                d.TipoCobro,
                                d.FechaInicio,
                                d.FechaVencimiento,
                                d.Activo
                            }).ToList(),
                        totalMensual = s.SuscripcionDetalles
                            .Where(d => d.Activo == true && d.TipoCobro == "mensual")
                            .Sum(d => d.PrecioUnitario),
                        totalAnual = s.SuscripcionDetalles
                            .Where(d => d.Activo == true && d.TipoCobro == "anual")
                            .Sum(d => d.PrecioUnitario)
                    })
                    .FirstOrDefaultAsync();

                if (suscripcion == null)
                {
                    return NotFound("Suscripción no encontrada.");
                }

                return Ok(suscripcion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Crear suscripción
        /// </summary>
        [HttpPost("Suscripciones")]
        public async Task<ActionResult> CreateSuscripcion(Suscripcione suscripcion)
        {
            try
            {
                suscripcion.FechaCreacion = DateTime.Now;
                _context.Suscripciones.Add(suscripcion);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSuscripcion), new { id = suscripcion.IdSuscripcion }, suscripcion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualizar suscripción (estado, paypal, etc)
        /// </summary>
        [HttpPut("Suscripciones")]
        public async Task<IActionResult> UpdateSuscripcion(Suscripcione s)
        {
            if (s.IdSuscripcion == 0)
            {
                return BadRequest("El ID de la suscripción es inválido.");
            }

            try
            {
                var existing = await _context.Suscripciones.FindAsync(s.IdSuscripcion);
                if (existing == null)
                {
                    return NotFound("Suscripción no encontrada.");
                }

                existing.EstadoSuscripcion = s.EstadoSuscripcion;
                existing.PaypalSubscriptionId = s.PaypalSubscriptionId;
                existing.FechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // =============================================
        // SUSCRIPCION DETALLE
        // =============================================

        /// <summary>
        /// Obtener detalle por idSuscripcion
        /// </summary>
        [HttpGet("SuscripcionDetalle")]
        public async Task<ActionResult> GetSuscripcionDetalle([FromQuery] int idSuscripcion)
        {
            try
            {
                var detalle = await _context.SuscripcionDetalles
                    .Where(d => d.IdSuscripcion == idSuscripcion)
                    .Select(d => new
                    {
                        d.IdDetalle,
                        d.IdSuscripcion,
                        d.Concepto,
                        d.PrecioUnitario,
                        d.TipoCobro,
                        d.FechaInicio,
                        d.FechaVencimiento,
                        d.Activo,
                        d.FechaCreacion
                    })
                    .ToListAsync();

                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Agregar item al detalle de suscripción
        /// </summary>
        [HttpPost("SuscripcionDetalle")]
        public async Task<ActionResult> CreateDetalle(SuscripcionDetalle detalle)
        {
            try
            {
                detalle.FechaCreacion = DateTime.Now;
                _context.SuscripcionDetalles.Add(detalle);
                await _context.SaveChangesAsync();

                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualizar item del detalle
        /// </summary>
        [HttpPut("SuscripcionDetalle")]
        public async Task<IActionResult> UpdateDetalle(SuscripcionDetalle d)
        {
            if (d.IdDetalle == 0)
            {
                return BadRequest("El ID del detalle es inválido.");
            }

            try
            {
                var existing = await _context.SuscripcionDetalles.FindAsync(d.IdDetalle);
                if (existing == null)
                {
                    return NotFound("Detalle no encontrado.");
                }

                existing.Concepto = d.Concepto;
                existing.PrecioUnitario = d.PrecioUnitario;
                existing.TipoCobro = d.TipoCobro;
                existing.FechaInicio = d.FechaInicio;
                existing.FechaVencimiento = d.FechaVencimiento;
                existing.Activo = d.Activo;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Eliminar item del detalle
        /// </summary>
        [HttpDelete("SuscripcionDetalle/{id}")]
        public async Task<IActionResult> DeleteDetalle(int id)
        {
            try
            {
                var detalle = await _context.SuscripcionDetalles.FindAsync(id);
                if (detalle == null)
                {
                    return NotFound("Detalle no encontrado.");
                }

                _context.SuscripcionDetalles.Remove(detalle);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // =============================================
        // HISTORIAL DE PAGOS
        // =============================================

        /// <summary>
        /// Obtener historial de pagos por idCliente o idSuscripcion
        /// </summary>
        [HttpGet("HistorialPagos")]
        public async Task<ActionResult> GetHistorialPagos([FromQuery] int? idCliente, [FromQuery] int? idSuscripcion)
        {
            try
            {
                var query = _context.HistorialPagos.AsQueryable();

                if (idCliente.HasValue)
                {
                    query = query.Where(p => p.IdCliente == idCliente);
                }

                if (idSuscripcion.HasValue)
                {
                    query = query.Where(p => p.IdSuscripcion == idSuscripcion);
                }

                var lista = await query
                    .OrderByDescending(p => p.FechaPago)
                    .Select(p => new
                    {
                        p.IdPago,
                        p.IdSuscripcion,
                        p.IdCliente,
                        cliente = _context.Clientes
                            .Where(c => c.IdCliente == p.IdCliente)
                            .Select(c => c.Nombres + " " + c.Apellidos)
                            .FirstOrDefault(),
                        p.FechaPago,
                        p.Monto,
                        p.MetodoPago,
                        p.EstadoPago,
                        p.DetallePago,
                        p.PaypalPaymentId,
                        p.FechaCreacion
                    })
                    .ToListAsync();

                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Registrar pago - genera el JSON del detalle automáticamente
        /// </summary>
        [HttpPost("HistorialPagos")]
        public async Task<ActionResult> CreatePago(HistorialPago pago)
        {
            try
            {
                // Si no viene detallePago, lo generamos del detalle activo de la suscripción
                if (string.IsNullOrEmpty(pago.DetallePago))
                {
                    var detalles = await _context.SuscripcionDetalles
                        .Where(d => d.IdSuscripcion == pago.IdSuscripcion && d.Activo == true)
                        .Select(d => new { d.Concepto, monto = d.PrecioUnitario })
                        .ToListAsync();

                    var total = detalles.Sum(d => d.monto);

                    pago.DetallePago = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        detalle = detalles,
                        total = total
                    });

                    // Si no viene monto, lo calculamos
                    if (pago.Monto == 0)
                    {
                        pago.Monto = total;
                    }
                }

                pago.FechaCreacion = DateTime.Now;
                _context.HistorialPagos.Add(pago);
                await _context.SaveChangesAsync();

                return Ok(pago);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualizar estado del pago (completado, fallido, reembolsado)
        /// </summary>
        [HttpPut("HistorialPagos")]
        public async Task<IActionResult> UpdatePago(HistorialPago p)
        {
            if (p.IdPago == 0)
            {
                return BadRequest("El ID del pago es inválido.");
            }

            try
            {
                var existing = await _context.HistorialPagos.FindAsync(p.IdPago);
                if (existing == null)
                {
                    return NotFound("Pago no encontrado.");
                }

                existing.EstadoPago = p.EstadoPago;
                existing.MetodoPago = p.MetodoPago;
                existing.PaypalPaymentId = p.PaypalPaymentId;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // =============================================
        // RESUMEN DE COBROS PENDIENTES
        // =============================================

        /// <summary>
        /// Obtener resumen de cobros - todas las suscripciones activas con su total
        /// </summary>
        [HttpGet("ResumenCobros")]
        public async Task<ActionResult> GetResumenCobros()
        {
            try
            {
                var resumen = await _context.Suscripciones
                    .Where(s => s.EstadoSuscripcion == "activa")
                    .Select(s => new
                    {
                        s.IdSuscripcion,
                        s.IdCliente,
                        cliente = _context.Clientes
                            .Where(c => c.IdCliente == s.IdCliente)
                            .Select(c => c.Nombres + " " + c.Apellidos)
                            .FirstOrDefault(),
                        correo = _context.Clientes
                            .Where(c => c.IdCliente == s.IdCliente)
                            .Select(c => c.Correo)
                            .FirstOrDefault(),
                        s.EstadoSuscripcion,
                        cantidadItems = s.SuscripcionDetalles.Count(d => d.Activo == true),
                        totalMensual = s.SuscripcionDetalles
                            .Where(d => d.Activo == true && d.TipoCobro == "mensual")
                            .Sum(d => d.PrecioUnitario),
                        proximoVencimiento = s.SuscripcionDetalles
                            .Where(d => d.Activo == true)
                            .Min(d => d.FechaVencimiento),
                        ultimoPago = s.HistorialPagos
                            .Where(p => p.EstadoPago == "completado")
                            .OrderByDescending(p => p.FechaPago)
                            .Select(p => p.FechaPago)
                            .FirstOrDefault()
                    })
                    .OrderBy(s => s.proximoVencimiento)
                    .ToListAsync();

                return Ok(resumen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}

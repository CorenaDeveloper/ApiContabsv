using ApiContabsv.Models.Contabsv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_OrdenesCompraController : Controller
    {
        public readonly ContabsvContext _contabsvContext;

        public DBContabsv_OrdenesCompraController(ContabsvContext contabsvContext)
        {
            _contabsvContext = contabsvContext;
        }

        // ============================================================
        // ÓRDENES DE COMPRA - ENCABEZADO
        // ============================================================

        [HttpGet("Ordenes")]
        [SwaggerOperation(Summary = "Listar todas las órdenes de compra de un cliente.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> GetOrdenes(int idCliente, string estado = null)
        {
            try
            {
                var query = _contabsvContext.InvOrdenesCompras
                    .Where(x => x.IdCliente == idCliente);

                if (!string.IsNullOrEmpty(estado))
                    query = query.Where(x => x.Estado == estado);

                var ordenes = await query
                    .OrderByDescending(x => x.FechaOrden)
                    .Select(o => new
                    {
                        o.IdCompra,
                        o.IdCliente,
                        o.IdProveedor,
                        o.NumeroOrden,
                        o.FechaOrden,
                        o.FechaCierre,
                        o.Estado,
                        o.Observaciones,
                        o.Responsable,
                        totalProductos = _contabsvContext.InvOrdenesCompraDetalles
                            .Count(d => d.IdCompra == o.IdCompra),
                        totalOrden = _contabsvContext.InvOrdenesCompraDetalles
                            .Where(d => d.IdCompra == o.IdCompra)
                            .Sum(d => (decimal?)d.CantidadOrdenada * d.CostoUnitario) ?? 0
                    })
                    .ToListAsync();

                return Ok(ordenes);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpGet("Ordenes/{id}")]
        [SwaggerOperation(Summary = "Obtener una orden de compra por ID con su detalle.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(404, "Orden no encontrada")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> GetOrden(int id)
        {
            try
            {
                var orden = await _contabsvContext.InvOrdenesCompras
                    .Where(o => o.IdCompra == id)
                    .Select(o => new
                    {
                        o.IdCompra,
                        o.IdCliente,
                        o.IdProveedor,
                        o.NumeroOrden,
                        o.FechaOrden,
                        o.FechaCierre,
                        o.Estado,
                        o.Observaciones,
                        o.Responsable,
                        detalle = _contabsvContext.InvOrdenesCompraDetalles
                            .Where(d => d.IdCompra == o.IdCompra)
                            .Select(d => new
                            {
                                d.IdDetalle,
                                d.IdProducto,
                                d.CantidadOrdenada,
                                d.CantidadRecibida,
                                d.CostoUnitario,
                                d.PrecioVenta,
                                d.Lote,
                                d.Observaciones,
                                nombreProducto = _contabsvContext.InvProductos
                                    .Where(p => p.IdProducto == d.IdProducto)
                                    .Select(p => p.Nombre)
                                    .FirstOrDefault(),
                                skuProducto = _contabsvContext.InvProductos
                                    .Where(p => p.IdProducto == d.IdProducto)
                                    .Select(p => p.Sku)
                                    .FirstOrDefault(),
                                imagenProducto = _contabsvContext.InvProductos
                                    .Where(p => p.IdProducto == d.IdProducto)
                                    .Select(p => p.Imagen)
                                    .FirstOrDefault()
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (orden == null)
                    return NotFound("Orden de compra no encontrada.");

                return Ok(orden);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPost("Ordenes")]
        [SwaggerOperation(Summary = "Crear una nueva orden de compra.")]
        [SwaggerResponse(201, "Orden creada exitosamente")]
        [SwaggerResponse(400, "Datos inválidos")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> CrearOrden([FromBody] InvOrdenesCompra dto)
        {
            try
            {
                if (dto.IdCliente == 0)
                    return BadRequest("IdCliente es requerido.");

                if (dto.IdProveedor == 0)
                    return BadRequest("IdProveedor es requerido.");

                var orden = new InvOrdenesCompra
                {
                    IdCliente = dto.IdCliente,
                    IdProveedor = dto.IdProveedor,
                    NumeroOrden = dto.NumeroOrden,
                    FechaOrden = DateTime.Now,
                    Estado = "Abierta",
                    Observaciones = dto.Observaciones,
                    Responsable = dto.Responsable
                };

                _contabsvContext.InvOrdenesCompras.Add(orden);
                await _contabsvContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOrden), new { id = orden.IdCompra }, new { orden.IdCompra });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPut("Ordenes")]
        [SwaggerOperation(Summary = "Actualizar datos generales de una orden de compra (solo si está Abierta).")]
        [SwaggerResponse(204, "Actualizado exitosamente")]
        [SwaggerResponse(400, "Datos inválidos o la orden no está abierta")]
        [SwaggerResponse(404, "Orden no encontrada")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> ActualizarOrden([FromBody] InvOrdenesCompra dto)
        {
            try
            {
                var orden = await _contabsvContext.InvOrdenesCompras.FindAsync(dto.IdCompra);
                if (orden == null)
                    return NotFound("Orden de compra no encontrada.");

                if (orden.Estado != "Abierta")
                    return BadRequest("Solo se pueden editar órdenes en estado Abierta.");

                orden.IdProveedor = dto.IdProveedor;
                orden.NumeroOrden = dto.NumeroOrden;
                orden.Observaciones = dto.Observaciones;
                orden.Responsable = dto.Responsable;

                await _contabsvContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpDelete("Ordenes/{id}")]
        [SwaggerOperation(Summary = "Eliminar una orden de compra (solo si está Abierta).")]
        [SwaggerResponse(204, "Eliminada exitosamente")]
        [SwaggerResponse(400, "No se puede eliminar una orden cerrada o anulada")]
        [SwaggerResponse(404, "Orden no encontrada")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> EliminarOrden(int id)
        {
            try
            {
                var orden = await _contabsvContext.InvOrdenesCompras
                    .Include(o => o.InvOrdenesCompraDetalles)
                    .FirstOrDefaultAsync(o => o.IdCompra == id);

                if (orden == null)
                    return NotFound("Orden de compra no encontrada.");

                if (orden.Estado != "Abierta")
                    return BadRequest("Solo se pueden eliminar órdenes en estado Abierta.");

                _contabsvContext.InvOrdenesCompraDetalles.RemoveRange(orden.InvOrdenesCompraDetalles);
                _contabsvContext.InvOrdenesCompras.Remove(orden);
                await _contabsvContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        // ============================================================
        // DETALLE DE ORDEN
        // ============================================================

        [HttpPost("Ordenes/Detalle")]
        [SwaggerOperation(Summary = "Agregar un producto al detalle de una orden de compra.")]
        [SwaggerResponse(201, "Producto agregado")]
        [SwaggerResponse(400, "La orden no está abierta o datos inválidos")]
        [SwaggerResponse(404, "Orden o producto no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> AgregarDetalle([FromBody] InvOrdenesCompraDetalle dto)
        {
            try
            {
                var orden = await _contabsvContext.InvOrdenesCompras.FindAsync(dto.IdCompra);
                if (orden == null)
                    return NotFound("Orden de compra no encontrada.");

                if (orden.Estado != "Abierta")
                    return BadRequest("Solo se pueden agregar productos a órdenes en estado Abierta.");

                var producto = await _contabsvContext.InvProductos.FindAsync(dto.IdProducto);
                if (producto == null)
                    return NotFound("Producto no encontrado.");

                var detalle = new InvOrdenesCompraDetalle
                {
                    IdCompra = dto.IdCompra,
                    IdProducto = dto.IdProducto,
                    CantidadOrdenada = dto.CantidadOrdenada,
                    CantidadRecibida = 0,
                    CostoUnitario = dto.CostoUnitario,
                    PrecioVenta = dto.PrecioVenta,
                    Lote = dto.Lote,
                    Observaciones = dto.Observaciones
                };

                _contabsvContext.InvOrdenesCompraDetalles.Add(detalle);
                await _contabsvContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOrden), new { id = dto.IdCompra }, new { detalle.IdDetalle });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPut("Ordenes/Detalle")]
        [SwaggerOperation(Summary = "Actualizar un producto del detalle (solo si la orden está Abierta).")]
        [SwaggerResponse(204, "Actualizado exitosamente")]
        [SwaggerResponse(400, "La orden no está abierta")]
        [SwaggerResponse(404, "Detalle no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> ActualizarDetalle([FromBody] InvOrdenesCompraDetalle dto)
        {
            try
            {
                var detalle = await _contabsvContext.InvOrdenesCompraDetalles.FindAsync(dto.IdDetalle);
                if (detalle == null)
                    return NotFound("Detalle no encontrado.");

                var orden = await _contabsvContext.InvOrdenesCompras.FindAsync(detalle.IdCompra);
                if (orden == null || orden.Estado != "Abierta")
                    return BadRequest("Solo se pueden editar detalles de órdenes en estado Abierta.");

                detalle.CantidadOrdenada = dto.CantidadOrdenada;
                detalle.CostoUnitario = dto.CostoUnitario;
                detalle.PrecioVenta = dto.PrecioVenta;
                detalle.Lote = dto.Lote;
                detalle.Observaciones = dto.Observaciones;

                await _contabsvContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpDelete("Ordenes/Detalle/{idDetalle}")]
        [SwaggerOperation(Summary = "Eliminar un producto del detalle (solo si la orden está Abierta).")]
        [SwaggerResponse(204, "Eliminado exitosamente")]
        [SwaggerResponse(400, "La orden no está abierta")]
        [SwaggerResponse(404, "Detalle no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> EliminarDetalle(int idDetalle)
        {
            try
            {
                var detalle = await _contabsvContext.InvOrdenesCompraDetalles.FindAsync(idDetalle);
                if (detalle == null)
                    return NotFound("Detalle no encontrado.");

                var orden = await _contabsvContext.InvOrdenesCompras.FindAsync(detalle.IdCompra);
                if (orden == null || orden.Estado != "Abierta")
                    return BadRequest("Solo se pueden eliminar detalles de órdenes en estado Abierta.");

                _contabsvContext.InvOrdenesCompraDetalles.Remove(detalle);
                await _contabsvContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        // ============================================================
        // CERRAR ORDEN → GENERA MOVIMIENTOS DE INVENTARIO
        // ============================================================

        [HttpPost("Ordenes/{id}/Cerrar")]
        [SwaggerOperation(Summary = "Cerrar una orden de compra y generar movimientos de inventario (Entradas).")]
        [SwaggerResponse(200, "Orden cerrada exitosamente")]
        [SwaggerResponse(400, "La orden no está abierta o no tiene productos con cantidad recibida")]
        [SwaggerResponse(404, "Orden no encontrada")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> CerrarOrden(int id, [FromBody] CerrarOrdenRequest request)
        {
            using var transaction = await _contabsvContext.Database.BeginTransactionAsync();
            try
            {
                var orden = await _contabsvContext.InvOrdenesCompras
                    .Include(o => o.InvOrdenesCompraDetalles)
                    .FirstOrDefaultAsync(o => o.IdCompra == id);

                if (orden == null)
                    return NotFound("Orden de compra no encontrada.");

                if (orden.Estado != "Abierta")
                    return BadRequest("Solo se pueden cerrar órdenes en estado Abierta.");

                // Actualizar cantidades recibidas si vienen en el request
                if (request?.Detalle != null && request.Detalle.Any())
                {
                    foreach (var item in request.Detalle)
                    {
                        var detalle = orden.InvOrdenesCompraDetalles
                            .FirstOrDefault(d => d.IdDetalle == item.IdDetalle);
                        if (detalle != null)
                            detalle.CantidadRecibida = item.CantidadRecibida;
                    }
                    await _contabsvContext.SaveChangesAsync();
                }

                // Validar que al menos un producto tiene cantidad recibida > 0
                var detallesConRecepcion = orden.InvOrdenesCompraDetalles
                    .Where(d => d.CantidadRecibida.HasValue && d.CantidadRecibida > 0)
                    .ToList();

                if (!detallesConRecepcion.Any())
                    return BadRequest("Debe ingresar al menos una cantidad recibida mayor a 0.");

                // Generar movimientos de inventario por cada detalle recibido
                var movimientos = new List<InvMovimiento>();
                foreach (var detalle in detallesConRecepcion)
                {
                    var producto = await _contabsvContext.InvProductos.FindAsync(detalle.IdProducto);
                    if (producto == null) continue;

                    var movimiento = new InvMovimiento
                    {
                        IdProducto = detalle.IdProducto,
                        IdCliente = orden.IdCliente,
                        TipoMovimiento = "Entrada",
                        Cantidad = (int)detalle.CantidadRecibida.Value,
                        CostoUnitario = detalle.CostoUnitario,
                        CostoReal = detalle.CostoUnitario,
                        PrecioVentaUnitario = detalle.PrecioVenta,
                        Lote = detalle.Lote,
                        LoteIngreso = Guid.NewGuid(),
                        CantidadDisponible = detalle.CantidadRecibida.Value,
                        IdCompra = orden.IdCompra,
                        NumeroDocumento = orden.NumeroOrden,
                        MotivoMovimiento = "Orden de Compra",
                        Responsable = request?.Responsable ?? orden.Responsable,
                        FechaMovimiento = DateTime.Now,
                        Observaciones = detalle.Observaciones
                    };

                    movimientos.Add(movimiento);

                    // Actualizar stock del producto
                    if (producto.TipoItemId != 2)
                        producto.Stock += (int)detalle.CantidadRecibida.Value;
                }

                _contabsvContext.InvMovimientos.AddRange(movimientos);

                // Cerrar la orden
                orden.Estado = "Cerrada";
                orden.FechaCierre = DateTime.Now;

                await _contabsvContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Orden cerrada exitosamente.",
                    idCompra = orden.IdCompra,
                    movimientosGenerados = movimientos.Count
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPost("Ordenes/{id}/Anular")]
        [SwaggerOperation(Summary = "Anular una orden de compra (solo si está Abierta).")]
        [SwaggerResponse(204, "Anulada exitosamente")]
        [SwaggerResponse(400, "La orden no está abierta")]
        [SwaggerResponse(404, "Orden no encontrada")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> AnularOrden(int id)
        {
            try
            {
                var orden = await _contabsvContext.InvOrdenesCompras.FindAsync(id);
                if (orden == null)
                    return NotFound("Orden de compra no encontrada.");

                if (orden.Estado != "Abierta")
                    return BadRequest("Solo se pueden anular órdenes en estado Abierta.");

                orden.Estado = "Anulada";
                await _contabsvContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }
    }

    // DTO para recibir cantidades al cerrar
    public class CerrarOrdenRequest
    {
        public string Responsable { get; set; }
        public List<DetalleRecepcion> Detalle { get; set; }
    }

    public class DetalleRecepcion
    {
        public int IdDetalle { get; set; }
        public decimal CantidadRecibida { get; set; }
    }
}
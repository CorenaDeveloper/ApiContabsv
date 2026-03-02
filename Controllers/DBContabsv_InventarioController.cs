using ApiContabsv.DTO.DB_ContabsvDTO;
using ApiContabsv.Models.Contabsv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_InventarioController : Controller
    {
        public readonly ContabsvContext _contabsvContext;

        public DBContabsv_InventarioController(ContabsvContext contabsvContext)
        {
            _contabsvContext = contabsvContext;
        }

        [HttpGet("Movimientos")]
        [SwaggerOperation(Summary = "Listar todos los movimientos de inventario.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "No encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<IEnumerable<InventarioDTO>>> GetMovimientos(int idCliente, int? idProducto = null, string tipoMovimiento = null)
        {
            try
            {
                var query = _contabsvContext.InvMovimientos
                    .Where(x => x.IdCliente == idCliente);

                // Filtrar por producto si se proporciona
                if (idProducto.HasValue && idProducto.Value > 0)
                {
                    query = query.Where(x => x.IdProducto == idProducto.Value);
                }

                // Filtrar por tipo de movimiento si se proporciona
                if (!string.IsNullOrEmpty(tipoMovimiento))
                {
                    query = query.Where(x => x.TipoMovimiento == tipoMovimiento);
                }

                var movimientos = await query
                    .Select(m => new
                    {
                        m.IdMovimiento,
                        m.IdProducto,
                        m.IdCliente,
                        m.TipoMovimiento,
                        m.Cantidad,
                        m.CostoUnitario,
                        m.PrecioVentaUnitario,
                        m.Lote,
                        m.NumeroSerie,
                        m.Ubicacion,
                        m.IdCompra,
                        m.IdVenta,
                        m.NumeroDocumento,
                        m.MotivoMovimiento,
                        m.Responsable,
                        m.FechaMovimiento,
                        m.Observaciones,
                        nombreProducto = _contabsvContext.InvProductos
                            .Where(p => p.IdProducto == m.IdProducto)
                            .Select(p => p.Nombre)
                            .FirstOrDefault(),
                        skuProducto = _contabsvContext.InvProductos
                            .Where(p => p.IdProducto == m.IdProducto)
                            .Select(p => p.Sku)
                            .FirstOrDefault(),
                        imagenProducto = _contabsvContext.InvProductos
                            .Where(p => p.IdProducto == m.IdProducto)
                            .Select(p => p.Imagen)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(m => m.FechaMovimiento)
                    .ToListAsync();

                return Ok(movimientos);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPost("Movimientos/Existencias")]
        [SwaggerOperation(Summary = "Listar existencias  de inventario.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "No encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> GetExistenciass([FromBody] object json)
        {
            try
            {
                var jsonInput = new SqlParameter
                {
                    ParameterName = "@jsonInput",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = -1,
                    Value = json.ToString()
                };

                var jsonOutput = new SqlParameter
                {
                    ParameterName = "@jsonOutput",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = -1,
                    Direction = ParameterDirection.Output
                };


                await _contabsvContext.Database.ExecuteSqlRawAsync(
                                 "EXEC [dbo].[sp_ListarExistencias] @jsonInput, @jsonOutput OUTPUT",
                                 jsonInput, jsonOutput
                              );

                var jsonResult = jsonOutput.Value?.ToString();

                if (!string.IsNullOrEmpty(jsonResult))
                {
                    return Content(jsonResult, "application/json");
                }
                else
                {
                    return StatusCode(500, new { message = "Error al procesar la solicitud" });
                }
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpGet("Movimientos/{id}")]
        [SwaggerOperation(Summary = "Obtener un movimiento de inventario por ID.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Movimiento no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<InventarioDTO>> GetMovimiento(int id)
        {
            try
            {
                var m = await _contabsvContext.InvMovimientos.FindAsync(id);

                if (m == null)
                    return NotFound("Movimiento no encontrado.");

                var movimiento = new
                {
                    m.IdMovimiento,
                    m.IdProducto,
                    m.IdCliente,
                    m.TipoMovimiento,
                    m.Cantidad,
                    m.CostoUnitario,
                    m.PrecioVentaUnitario,
                    m.Lote,
                    m.NumeroSerie,
                    m.Ubicacion,
                    m.IdCompra,
                    m.IdVenta,
                    m.NumeroDocumento,
                    m.MotivoMovimiento,
                    m.Responsable,
                    m.FechaMovimiento,
                    m.Observaciones,
                    nombreProducto = _contabsvContext.InvProductos
                        .Where(p => p.IdProducto == m.IdProducto)
                        .Select(p => p.Nombre)
                        .FirstOrDefault(),
                    skuProducto = _contabsvContext.InvProductos
                        .Where(p => p.IdProducto == m.IdProducto)
                        .Select(p => p.Sku)
                        .FirstOrDefault(),
                    imagenProducto = _contabsvContext.InvProductos
                        .Where(p => p.IdProducto == m.IdProducto)
                        .Select(p => p.Imagen)
                        .FirstOrDefault()
                };

                return Ok(movimiento);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPost("Movimientos")]
        [SwaggerOperation(Summary = "Crear un nuevo movimiento de inventario.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Producto no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<InventarioDTO>> CrearMovimiento(InventarioDTO dto)
        {
            try
            {
                // Validar que el producto existe
                var producto = await _contabsvContext.InvProductos.FindAsync(dto.idProducto);
                if (producto == null)
                    return NotFound("Producto no encontrado.");

                //  GENERAR LOTE AUTOMÁTICO PARA CADA ENTRADA
                var loteIngresoId = Guid.NewGuid();

                var movimiento = new InvMovimiento
                {
                    IdMovimiento = 0,
                    IdProducto = dto.idProducto,
                    IdCliente = dto.idCliente,
                    TipoMovimiento = dto.tipoMovimiento,
                    Cantidad = dto.cantidad,
                    CostoUnitario = dto.costoUnitario,
                    PrecioVentaUnitario = dto.precioVentaUnitario,
                    Lote = dto.lote, 
                    LoteIngreso = loteIngresoId, 
                    CostoReal = dto.costoUnitario,
                    CantidadDisponible = dto.tipoMovimiento == "Entrada" ? dto.cantidad : 0, 
                    NumeroSerie = dto.numeroSerie,
                    Ubicacion = dto.ubicacion,
                    IdCompra = dto.idCompra,
                    IdVenta = dto.idVenta,
                    NumeroDocumento = dto.numeroDocumento,
                    MotivoMovimiento = dto.motivoMovimiento,
                    Responsable = dto.responsable,
                    FechaMovimiento = dto.fechaMovimiento,
                    Observaciones = dto.observaciones
                };

                _contabsvContext.Add(movimiento);

                // Solo actualizar stock si NO es un servicio (TipoItemId != 2)
                if (producto.TipoItemId == 2) // Servicio
                {
                    producto.Stock = 1;
                }
                else
                {
                    producto.Stock += dto.cantidad;
                }

                await _contabsvContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetMovimiento), new { id = movimiento.IdMovimiento }, dto);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPut("Movimientos")]
        [SwaggerOperation(Summary = "Actualizar un movimiento de inventario existente.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Movimiento no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<InventarioDTO>> UpdateMovimiento(InventarioDTO dto)
        {
            try
            {
                var movimiento = await _contabsvContext.InvMovimientos.FindAsync(dto.idMovimiento);
                if (movimiento == null)
                {
                    return BadRequest("Movimiento no encontrado");
                }

                var producto = await _contabsvContext.InvProductos.FindAsync(movimiento.IdProducto);
                if (producto == null)
                    return NotFound("Producto no encontrado.");

                // REVERTIR CANTIDAD DISPONIBLE DEL LOTE ANTERIOR (SOLO ENTRADAS)
                if (movimiento.TipoMovimiento == "Entrada" && movimiento.LoteIngreso != null)
                {
                    // No revertir porque puede afectar ventas ya procesadas con FIFO
                    // Solo permitir modificar datos no críticos
                }

                // Revertir stock físico si no es servicio
                if (producto.TipoItemId != 2)
                {
                    producto.Stock -= movimiento.Cantidad;
                }

                // Actualizar los datos del movimiento (excepto campos FIFO críticos)
                movimiento.TipoMovimiento = dto.tipoMovimiento;
                movimiento.Cantidad = dto.cantidad;
                movimiento.CostoUnitario = dto.costoUnitario;
                movimiento.PrecioVentaUnitario = dto.precioVentaUnitario;
                movimiento.Lote = dto.lote; // Lote manual sí se puede cambiar
                                            //  NO CAMBIAR: LoteIngreso, CostoReal, CantidadDisponible
                movimiento.NumeroSerie = dto.numeroSerie;
                movimiento.Ubicacion = dto.ubicacion;
                movimiento.MotivoMovimiento = dto.motivoMovimiento;
                movimiento.Responsable = dto.responsable;
                movimiento.Observaciones = dto.observaciones;

                // ACTUALIZAR CAMPOS FIFO SOLO SI ES ENTRADA
                if (dto.tipoMovimiento == "Entrada")
                {
                    movimiento.CostoReal = dto.costoUnitario;
                    movimiento.CantidadDisponible = dto.cantidad; // Resetear disponibilidad
                }

                // Aplicar nuevo stock
                if (producto.TipoItemId == 2)
                {
                    producto.Stock = 1;
                }
                else
                {
                    producto.Stock += dto.cantidad;
                }

                await _contabsvContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpDelete("Movimientos/{id}")]
        [SwaggerOperation(Summary = "Eliminar un movimiento de inventario.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Movimiento no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> DeleteMovimiento(int id)
        {
            try
            {
                var movimiento = await _contabsvContext.InvMovimientos.FindAsync(id);
                if (movimiento == null)
                {
                    return BadRequest("Movimiento no encontrado");
                }

                // ✅ VALIDAR SI EL LOTE YA FUE USADO EN VENTAS (FIFO)
                if (movimiento.TipoMovimiento == "Entrada" && movimiento.CantidadDisponible < movimiento.Cantidad)
                {
                    var cantidadUsada = movimiento.Cantidad - movimiento.CantidadDisponible;
                    return BadRequest($"No se puede eliminar: este lote ya fue usado en ventas ({cantidadUsada} unidades vendidas)");
                }

                var producto = await _contabsvContext.InvProductos.FindAsync(movimiento.IdProducto);
                if (producto != null)
                {
                    if (producto.TipoItemId != 2)
                    {
                        producto.Stock -= movimiento.Cantidad;
                    }
                    else
                    {
                        producto.Stock = 1;
                    }
                }

                _contabsvContext.InvMovimientos.Remove(movimiento);
                await _contabsvContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpGet("StockActual")]
        [SwaggerOperation(Summary = "Obtener el stock actual de un producto calculado desde movimientos.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Producto no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<int>> GetStockActual(int idProducto, int idCliente)
        {
            try
            {
                var producto = await _contabsvContext.InvProductos
                    .Where(p => p.IdProducto == idProducto && p.IdCliente == idCliente)
                    .FirstOrDefaultAsync();

                if (producto == null)
                    return NotFound("Producto no encontrado.");

                // Para servicios, siempre retornar stock disponible
                if (producto.TipoItemId == 2)
                    return Ok(1); // Servicios siempre disponibles

                return Ok(producto.Stock);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpGet("Kardex")]
        [SwaggerOperation(Summary = "Obtener el kardex (historial) de movimientos de un producto.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Producto no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult> GetKardex(int idProducto, int idCliente)
        {
            try
            {
                // Obtener información del producto
                var producto = await _contabsvContext.InvProductos
                    .Where(p => p.IdProducto == idProducto && p.IdCliente == idCliente)
                    .FirstOrDefaultAsync();

                if (producto == null)
                    return NotFound("Producto no encontrado.");

                var movimientos = await _contabsvContext.InvMovimientos
                    .Where(m => m.IdProducto == idProducto && m.IdCliente == idCliente)
                    .OrderBy(m => m.FechaMovimiento)
                    .Select(m => new
                    {
                        m.IdMovimiento,
                        m.FechaMovimiento,
                        m.TipoMovimiento,
                        m.Cantidad,
                        m.CostoUnitario,
                        m.PrecioVentaUnitario,
                        m.Lote,
                        m.NumeroSerie,
                        m.MotivoMovimiento,
                        m.Responsable,
                        m.NumeroDocumento
                    })
                    .ToListAsync();

                // Calcular stock acumulado (solo para productos físicos)
                int stockAcumulado = 0;
                var kardex = movimientos.Select(m =>
                {
                    // Solo acumular stock si no es servicio
                    if (producto.TipoItemId != 2)
                        stockAcumulado += m.Cantidad;
                    else
                        stockAcumulado = 1; // Servicios siempre "1"

                    return new
                    {
                        m.IdMovimiento,
                        m.FechaMovimiento,
                        m.TipoMovimiento,
                        m.Cantidad,
                        StockAcumulado = stockAcumulado,
                        m.CostoUnitario,
                        m.PrecioVentaUnitario,
                        ValorTotal = m.Cantidad * (m.CostoUnitario ?? 0),
                        m.Lote,
                        m.NumeroSerie,
                        m.MotivoMovimiento,
                        m.Responsable,
                        m.NumeroDocumento,
                        EsServicio = producto.TipoItemId == 2
                    };
                }).ToList();

                return Ok(kardex);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }
    }
}
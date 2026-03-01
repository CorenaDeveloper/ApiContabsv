using ApiContabsv.DTO.DB_ContabsvDTO;
using ApiContabsv.Models.Contabsv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Swashbuckle.AspNetCore.Annotations;
using static System.Net.Mime.MediaTypeNames;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_InvProductoController : Controller
    {
        public readonly ContabsvContext _contabsvContext;

        public DBContabsv_InvProductoController(ContabsvContext ContabsvContext)
        {
            _contabsvContext = ContabsvContext;
        }

        [HttpGet("Productos")]
        [SwaggerOperation(Summary = "Listar todas las Productos.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<IEnumerable<ProductoDTO>>> GetProductos(int idCliente)
        {
            try
            {
                var a = await _contabsvContext.InvProductos
                    .Where(x => x.IdCliente == idCliente)
                    .Select(c => new
                    {
                        c.IdProducto,
                        c.Nombre,
                        c.Descripcion,
                        c.IdMarca,
                        c.IdCategoria,
                        c.IdTipo,
                        c.CodigoBarra,
                        c.PrecioCompra,
                        c.PrecioVenta,
                        c.Estado,
                        c.FechaRegistro,
                        c.IdCliente,
                        c.Stock,
                        c.StockMinimo,
                        c.StockMaximo,
                        c.Peso,
                        c.Volumen,
                        c.UnidadMedida,
                        c.Imagen,
                        c.Sku,
                        c.FactorCaja,
                        c.CodigoUnidadMh,
                        nombreMarca = _contabsvContext.InvMarcas
                            .Where(m => m.IdMarca == c.IdMarca)
                            .Select(m => m.Nombre)
                            .FirstOrDefault(),
                        nombreCategoria = _contabsvContext.InvCategorias
                            .Where(cat => cat.IdCategoria == c.IdCategoria)
                            .Select(cat => cat.Nombre)
                             .FirstOrDefault(),
                       nombreTipo = _contabsvContext.InvTiposProductos
                            .Where(t => t.IdTipo == c.IdTipo)
                            .Select(t => t.Nombre)
                            .FirstOrDefault(),
                        c.TipoItemId,
                        nombreTipoItem = c.TipoItemId == 2 ? "Servicio" : c.TipoItemId == 1 ? "Bienes" : c.TipoItemId == 3 ? "Ambos (Servicio/Producto)": "N/A"

                    }).ToListAsync();

                return Ok(a);
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpGet("Productos/{id}")]
        [SwaggerOperation(Summary = "Obtener una Producto por ID.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<ProductoDTO>> GetProducto(int id)
        {
            try
            {
                var c = await _contabsvContext.InvProductos.FindAsync(id);

                if (c == null)
                    return NotFound("Dato no encontrada.");

                var b = new
                {
                    c.IdProducto,
                    c.Nombre,
                    c.Descripcion,
                    c.IdMarca,
                    c.IdCategoria,
                    c.IdTipo,
                    c.CodigoBarra,
                    c.PrecioCompra,
                    c.PrecioVenta,
                    c.Estado,
                    c.FechaRegistro,
                    c.IdCliente,
                    c.Stock,
                    c.StockMinimo,
                    c.StockMaximo,
                    c.Peso,
                    c.Volumen,
                    c.UnidadMedida,
                    c.Imagen,
                    c.Sku,
                    c.FactorCaja,
                    c.CodigoUnidadMh,
                    nombreMarca = _contabsvContext.InvMarcas
                            .Where(m => m.IdMarca == c.IdMarca)
                            .Select(m => m.Nombre)
                            .FirstOrDefault(),
                    nombreCategoria = _contabsvContext.InvCategorias
                            .Where(cat => cat.IdCategoria == c.IdCategoria)
                            .Select(cat => cat.Nombre)
                            .FirstOrDefault(),
                    nombreTipo = _contabsvContext.InvTiposProductos
                            .Where(t => t.IdTipo == c.IdTipo)
                            .Select(t => t.Nombre)
                            .FirstOrDefault(),
                     c.TipoItemId,
                    nombreTipoItem = c.TipoItemId == 2 ? "Servicio" : c.TipoItemId == 1 ? "Bienes" : c.TipoItemId == 3 ? "Ambos (Servicio/Producto)" : "N/A"
                };

                return Ok(b);
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }

        }

        [HttpPost("Productos")]
        [SwaggerOperation(Summary = "Crear una nueva Producto.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<ProductoDTO>> CrearProducto(ProductoDTO c)
        {
            try
            {
                var a = new InvProducto
                {
                    IdProducto = 0,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    IdMarca = c.IdMarca,
                    IdCategoria = c.IdCategoria,
                    IdTipo = c.IdTipo,
                    CodigoBarra = c.CodigoBarra,
                    PrecioCompra = c.PrecioCompra,
                    PrecioVenta = c.PrecioVenta,
                    Estado = true,
                    FechaRegistro = DateTime.Now,
                    IdCliente = c.IdCliente,
                    Stock = c.Stock,
                    StockMinimo = c.StockMinimo,
                    StockMaximo = c.StockMaximo,
                    Peso = c.Peso,
                    Volumen = c.Volumen,
                    UnidadMedida = c.UnidadMedida,
                    Imagen = c.Imagen,
                    Sku = c.Sku,
                    TipoItemId = c.TipoItemId,
                    FactorCaja = c.FactorCaja,
                    CodigoUnidadMh = c.CodigoUnidadMh   
                };

                _contabsvContext.Add(a);
                await _contabsvContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProducto), new { id = c.IdProducto }, c);
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPut("Productos")]
        [SwaggerOperation(Summary = "Actualizar una Tipo Producto existente.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<ProductoDTO>> UpdateProducto(ProductoDTO c)
        {
            try
            {
                var a = await _contabsvContext.InvProductos.FindAsync(c.IdProducto);
                if (a == null)
                {
                    return BadRequest("Dato no encontrada");
                }

                a.Nombre = c.Nombre;
                a.Descripcion = c.Descripcion;
                a.IdMarca = c.IdMarca;
                a.IdCategoria = c.IdCategoria;  
                a.IdTipo = c.IdTipo;    
                a.CodigoBarra = c.CodigoBarra;
                a.PrecioCompra = c.PrecioCompra;
                a.PrecioVenta = c.PrecioVenta;
                a.Estado = c.Estado;
                a.Stock = c.Stock;
                a.StockMinimo = c.StockMinimo;
                a.StockMaximo = c.StockMaximo;
                a.Peso = c.Peso;
                a.Volumen = c.Volumen;
                a.UnidadMedida = c.UnidadMedida;
                a.Imagen = c.Imagen;
                a.Sku = c.Sku;
                a.TipoItemId = c.TipoItemId;
                a.FactorCaja = c.FactorCaja;
                a.CodigoUnidadMh = c.CodigoUnidadMh;

                await _contabsvContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpDelete("Productos/{id}")]
        [SwaggerOperation(Summary = "Eliminar una Producto.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> DeleteProducto(int id)
        {

            try
            {
                var a = await _contabsvContext.InvProductos.FindAsync(id);
                if (a == null)
                {
                    return BadRequest("Dato no encontrada");
                }

                var stocksRelacionados = _contabsvContext.InvStocks.Where(s => s.IdProducto == id);
                if (stocksRelacionados.Any())
                {
                    _contabsvContext.InvStocks.RemoveRange(stocksRelacionados);
                    await _contabsvContext.SaveChangesAsync();
                }

                _contabsvContext.InvProductos.Remove(a);
                await _contabsvContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }
    }
}

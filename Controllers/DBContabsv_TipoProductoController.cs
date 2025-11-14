using ApiContabsv.DTO.DB_ContabsvDTO;
using ApiContabsv.Models.Contabsv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_TipoProductoController : Controller
    {
        public readonly ContabsvContext _contabsvContext;

        public DBContabsv_TipoProductoController(ContabsvContext ContabsvContext)
        {
            _contabsvContext = ContabsvContext;
        }

        [HttpGet("TipoProducto")]
        [SwaggerOperation(Summary = "Listar todas las Tipos Productos.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<IEnumerable<TipoProductoDTO>>> GetTipoProductos(int idCliente)
        {
            try
            {
                var a = await _contabsvContext.InvTiposProductos
                    .Where(x => x.IdCliente == idCliente)
                    .Select(c => new
                    {
                        c.IdTipo,
                        c.Nombre,
                        c.Descripcion,
                        c.Estado,
                        c.IdCliente
                    }).ToListAsync();

                return Ok(a);
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpGet("TipoProducto/{id}")]
        [SwaggerOperation(Summary = "Obtener una Tipo Producto por ID.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<TipoProductoDTO>> GetTipoProducto(int id)
        {
            try
            {
                var c = await _contabsvContext.InvTiposProductos.FindAsync(id);

                if (c == null)
                    return NotFound("Dato no encontrada.");

                var b = new
                {
                    c.IdTipo,
                    c.Nombre,
                    c.Descripcion,
                    c.Estado,
                    c.IdCliente
                };

                return Ok(b);
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }

        }

        [HttpPost("TipoProducto")]
        [SwaggerOperation(Summary = "Crear una nueva Tipo Producto.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<TipoProductoDTO>> CrearTipoProducto(TipoProductoDTO c)
        {
            try
            {
                var a = new InvTiposProducto
                {
                    IdTipo = 0,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Estado = c.Estado,
                    IdCliente = c.IdCliente
                };

                _contabsvContext.Add(a);
                await _contabsvContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTipoProducto), new { id = c.IdTipo }, c);
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPut("TipoProducto")]
        [SwaggerOperation(Summary = "Actualizar una Tipo Producto existente.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<TipoProductoDTO>> UpdateTipoProducto(TipoProductoDTO c)
        {
            try
            {
                var a = await _contabsvContext.InvTiposProductos.FindAsync(c.IdTipo);
                if (a == null)
                {
                    return BadRequest("Dato no encontrada");
                }

                a.Nombre = c.Nombre;
                a.Descripcion = c.Descripcion;
                a.Estado = c.Estado;

                await _contabsvContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpDelete("TipoProducto/{id}")]
        [SwaggerOperation(Summary = "Eliminar una Tipo Producto.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> DeleteTipoProducto(int id)
        {

            try
            {
                var a = await _contabsvContext.InvTiposProductos.FindAsync(id);
                if (a == null)
                {
                    return BadRequest("Dato no encontrada");
                }

                _contabsvContext.InvTiposProductos.Remove(a);
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

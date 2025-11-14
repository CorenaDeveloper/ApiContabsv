using ApiContabsv.DTO.DB_ContabsvDTO;
using ApiContabsv.Models.Contabsv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_CategoriasController : Controller
    {
        public readonly ContabsvContext _contabsvContext;

        public DBContabsv_CategoriasController(ContabsvContext ContabsvContext)
        {
            _contabsvContext = ContabsvContext;
        }

        [HttpGet("Categorias")]
        [SwaggerOperation(Summary = "Listar todas las Categorias.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> GetCategorias(int idCliente)
        {
            try
            {
                var a = await _contabsvContext.InvCategorias
                    .Where(x => x.IdCliente == idCliente)
                    .Select(c => new
                    {
                        c.IdCategoria,
                        c.Nombre,
                        c.Descripcion,
                        c.Estado,
                        c.FechaRegistro,
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

        [HttpGet("Categorias/{id}")]
        [SwaggerOperation(Summary = "Obtener una Categoria por ID.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<CategoriaDTO>> GetCategoria(int id)
        {
            try
            {
                var c = await _contabsvContext.InvCategorias.FindAsync(id);

                if (c == null)
                    return NotFound("Categoria no encontrada.");

                var b = new
                {
                    c.IdCategoria,
                    c.Nombre,
                    c.Descripcion,
                    c.Estado,
                    c.FechaRegistro,
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

        [HttpPost("Categorias")]
        [SwaggerOperation(Summary = "Crear una nueva Categoria.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<CategoriaDTO>> CrearCategoria(CategoriaDTO c)
        {
            try
            {
                var a = new InvCategoria
                {
                    IdCategoria = 0,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Estado = c.Estado,
                    FechaRegistro = DateTime.Now,
                    IdCliente = c.IdCliente
                };

                _contabsvContext.Add(a);
                await _contabsvContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCategoria), new { id = c.IdCategoria }, c);
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPut("Categorias")]
        [SwaggerOperation(Summary = "Actualizar una Categoria existente.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<CategoriaDTO>> UpdateCategoria(CategoriaDTO c)
        {
            try
            {
                var a = await _contabsvContext.InvCategorias.FindAsync(c.IdCategoria);
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

        [HttpDelete("Categorias/{id}")]
        [SwaggerOperation(Summary = "Eliminar una Categoria.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {

            try
            {
                var a = await _contabsvContext.InvCategorias.FindAsync(id);
                if (a == null)
                {
                    return BadRequest("Dato no encontrada");
                }

                _contabsvContext.InvCategorias.Remove(a);
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

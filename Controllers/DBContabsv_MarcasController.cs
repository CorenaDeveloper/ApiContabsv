
using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.DTO.DB_ContabsvDTO;
using ApiContabsv.Models.Contabsv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.RegularExpressions;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_MarcasController : Controller
    {
        public readonly ContabsvContext _contabsvContext;

        public DBContabsv_MarcasController(ContabsvContext ContabsvContext)
        {
            _contabsvContext = ContabsvContext;
        }

        [HttpGet("Marcas")]
        [SwaggerOperation(Summary = "Listar todas las marcas.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<IEnumerable<MarcaDTO>>> GetMarcas(int idCliente)
        {
            try
            {
                var a = await _contabsvContext.InvMarcas
                    .Where(x => x.IdCliente == idCliente)
                    .Select(c => new
                    {
                        c.IdMarca,
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

        [HttpGet("Marcas/{id}")]
        [SwaggerOperation(Summary = "Obtener una marca por ID.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<MarcaDTO>> GetMarca(int id)
        {
            try
            {
                var c = await _contabsvContext.InvMarcas.FindAsync(id);

                if (c == null)
                    return NotFound("Marca no encontrada.");

                var b = new
                {
                    c.IdMarca,
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

        [HttpPost("Marcas")]
        [SwaggerOperation(Summary = "Crear una nueva marca.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<MarcaDTO>> CrearMarca(MarcaDTO c)
        {
            try
            {
                var a = new InvMarca
                {
                    IdMarca = 0,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Estado = c.Estado,
                    FechaRegistro = DateTime.Now,
                    IdCliente = c.IdCliente
                };

                _contabsvContext.Add(a);
                await _contabsvContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetMarca), new { id = c.IdMarca }, c);
            }
            catch (Exception ex)
            {

                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPut("Marcas")]
        [SwaggerOperation(Summary = "Actualizar una marca existente.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<MarcaDTO>> UpdateMarca(MarcaDTO c)
        {
            try
            {
                var a = await _contabsvContext.InvMarcas.FindAsync(c.IdMarca);
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

        [HttpDelete("Marcas/{id}")]
        [SwaggerOperation(Summary = "Eliminar una marca.")]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> DeleteMarca(int id)
        {

            try
            {
                var a = await _contabsvContext.InvMarcas.FindAsync(id);
                if (a == null)
                {
                    return BadRequest("Dato no encontrada");
                }

                _contabsvContext.InvMarcas.Remove(a);
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

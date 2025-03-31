using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_ResolucionController : ControllerBase
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_ResolucionController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("Resolucion")]
        [SwaggerOperation(
        Summary = "CONSULTA RESOLUCIONES POR CLIENTE",
        Description = "Este endpoint permite consultar  lista de resoluciones por cliente.")]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Datos no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<IEnumerable<ResolucionesDTO>>> GetResoluciones(int idCliente)
        {
            try
            {
                var a = await _context.Resolucions
                    .Where(a => a.IdCliente == idCliente)
                    .Select(c => new
                    {
                      c.Id,
                      c.IdTipoDocumento,
                        Documento = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.Nombre : "N/A",
                        NombreCorto = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.NombreCorto : "N/A",
                        c.NumeroSerie,
                      c.NumeroResolucion,
                      c.Activo
                    }).ToListAsync();

                return Ok(a);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("Resolucion/{idResolucion}")]
        [SwaggerOperation(Summary = "CONSULTA RESOLUCIONES POR CLIENTE",
         Description = "Este endpoint permite consultar una resolucion por especifico")]
        [SwaggerResponse(200, "Consulta exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Datos no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<ResolucionesDTO>> GetResolucion(int idResolucion)
        {
            try
            {
                var c = await _context.Resolucions
                             .Include(r => r.IdTipoDocumentoNavigation)
                             .FirstOrDefaultAsync(r => r.Id == idResolucion);
                if (c == null)
                {
                    return NotFound("Proveedor no encontrado.");
                };


                var b = new
                {
                    c.Id,
                    c.IdTipoDocumento,
                    Documento = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.Nombre : "N/A",
                    NombreCorto = c.IdTipoDocumentoNavigation != null ? c.IdTipoDocumentoNavigation.NombreCorto : "N/A",
                    c.NumeroSerie,
                    c.NumeroResolucion,
                    c.Activo
                };

                return Ok(b);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPost("Resolucion")]
        [SwaggerOperation(
         Summary = "CREA RESOLUCION",
         Description = "Este endpoints registra una resolucion por tipo de documento."
        )]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<CreateResolucionesDTO>> CreateResolucion(CreateResolucionesDTO c)
        {
            try
            {
                var a = new Resolucion
                {
                    Id = 0,
                    IdTipoDocumento = c.IdTipoDocumento,
                    NumeroResolucion = c.NumeroResolucion,
                    NumeroSerie = c.NumeroSerie,
                    Activo = true,
                    IdCliente = c.IdCliente
                };

                _context.Add(a);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetResolucion), new { idResolucion = c.Id }, c);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPut("Resolucion")]
        [SwaggerOperation(
         Summary = "ACTUALIZA RESOLUCION",
         Description = "Este endpoints actualizar una resolucion por tipo de documento."
        )]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<UpdateResolucionesDTO>> UpdateResolucion(UpdateResolucionesDTO c)
        {
            try
            {
                var b = await _context.Resolucions.FindAsync(c.Id);
                if (b == null)
                {
                    return BadRequest("Resolucion no encontrada");
                }

                b.IdTipoDocumento = c.IdTipoDocumento;
                b.NumeroResolucion = c.NumeroResolucion;
                b.NumeroSerie = c.NumeroSerie;
                b.Activo = c.Activo;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpDelete("Resolucion")]
        [SwaggerOperation(
         Summary = "ELIMINANA UN RESOLUCION",
         Description = "Este endpoints elimina una resolucion por tipo de documento."
        )]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<IActionResult> DeleteResolucion(int id)
        {
            try
            {
                var b = await _context.Resolucions.FindAsync(id);
                if (b == null)
                {
                    return BadRequest("Resolucion no encontrada");
                }

                _context.Resolucions.Remove(b);
                await _context.SaveChangesAsync();
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

using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.DTO.DB_ContabsvDTO;
using ApiContabsv.Models.Contabilidad;
using ApiContabsv.Models.Contabsv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabsv_UsuariosFireBaseController : Controller
    {
        public readonly ContabsvContext _contabsvContext;

        public DBContabsv_UsuariosFireBaseController(ContabsvContext ContabsvContext)
        {
            _contabsvContext = ContabsvContext;
        }

        [HttpGet("UserFireBase")]
        [SwaggerOperation(
        Summary = "CONSULTA REGISTOR DE USUARIOS FIREBASE",
        Description = "Este endpoints registra todo los consultar  de los usuarios que se crean con firebase"
       )]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<UserFireBase>> Get(int id)
        {
            try
            {
                var a = await _contabsvContext.UsuariosFirebases
                       .FirstOrDefaultAsync(a => a.Id == id);

                if (a == null)
                {
                    return NotFound("usuario no encontrado.");
                };

                var b = new
                {
                    a.Id,
                    a.Uid,
                    a.Nombre,
                    a.Correo,
                    a.FotoUrl
                };

                return Ok(b);   

            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpPost("UserFireBase")]
        [SwaggerOperation(
         Summary = "CREA REGISTOR DE USUARIOS FIREBASE",
         Description = "Este endpoints registra todo los registro de los usuarios que se crean con firebase"
        )]
        [SwaggerResponse(200, "Operación exitosa")]
        [SwaggerResponse(400, "Solicitud incorrecta (datos inválidos)")]
        [SwaggerResponse(404, "Usuario no encontrado")]
        [SwaggerResponse(500, "Error interno del servidor")]
        public async Task<ActionResult<UserFireBase>> CreateResolucion(UserFireBase c)
        {
            try
            {
                var v = await _contabsvContext.UsuariosFirebases
                    .FirstOrDefaultAsync(x => x.Uid == c.Uid);

                if (v != null)
                {
                    v.UltimaSession = DateTime.Now;
                    await _contabsvContext.SaveChangesAsync();
                    return Ok(new { id = v.Id });

                }

                var a = new UsuariosFirebase
                {
                    Id = 0,
                    Uid = c.Uid,
                    Nombre = c.Nombre,
                    Correo = c.Correo,
                    FotoUrl = c.FotoUrl,
                    FechaRegistro = DateTime.Now
                };

                _contabsvContext.Add(a);
                await _contabsvContext.SaveChangesAsync();

                return Ok(new { id = a.Id });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }
    }
}

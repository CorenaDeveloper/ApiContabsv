using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_ClasificacionController : ControllerBase
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_ClasificacionController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("Clasificacion")]
        public async Task<ActionResult<IEnumerable<Clasificacion>>> GetClasificacion()
        {
            try
            {
                return await _context.Clasificacions
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

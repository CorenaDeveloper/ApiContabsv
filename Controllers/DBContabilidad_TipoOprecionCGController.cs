using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_TipoOprecionCGController : ControllerBase
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_TipoOprecionCGController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("TipoOperacionIngoGast")]
        public async Task<ActionResult<IEnumerable<TipoOperacionCg>>> GetTipoOperacion()
        {
            try
            {
                return await _context.TipoOperacionCgs
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_SectorController : ControllerBase
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_SectorController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("Sector")]
        public async Task<ActionResult<IEnumerable<Sector>>> GetSector()
        {
            try
            {
                return await _context.Sectors
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_ActividesdeEconomicasController : Controller
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_ActividesdeEconomicasController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("Actividades_Economicas")]
        public async Task<ActionResult<IEnumerable<ActividadesEconomica>>> GetACtividadEconomicas()
        {
            try
            {
                return await _context.ActividadesEconomicas
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

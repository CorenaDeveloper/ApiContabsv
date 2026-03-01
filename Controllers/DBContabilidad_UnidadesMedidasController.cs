using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_UnidadesMedidasController : Controller
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_UnidadesMedidasController(ContabilidadContext context)
        {
            _context = context;
        }
        [HttpGet("Unidades_Medidas")]
        public async Task<ActionResult<IEnumerable<CatUnidadesMedidum>>> GetUnidades()
        {
            try
            {
                return await _context.CatUnidadesMedida
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

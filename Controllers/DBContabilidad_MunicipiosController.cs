using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_MunicipiosController : Controller
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_MunicipiosController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("Municipios")]
        public async Task<ActionResult<IEnumerable<Municipio>>> GetMunicipios()
        {
            try
            {
                return await _context.Municipios
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_TipoItemController : Controller
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_TipoItemController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("TipoItem")]
        public async Task<ActionResult<IEnumerable<CatTipoItem>>> GetTIpoItem()
        {
            try
            {
                return await _context.CatTipoItems
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

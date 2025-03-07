using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_Tpo_DocumentoController : ControllerBase
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_Tpo_DocumentoController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("TipoDocumento")]
        public async Task<ActionResult<IEnumerable<TipoDocumento>>> GetTipoDocumento()
        {
            try
            {
                return await _context.TipoDocumentos
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

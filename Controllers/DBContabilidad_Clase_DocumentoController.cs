using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_Clase_DocumentoController : ControllerBase
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_Clase_DocumentoController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("Clase_Documentos")]
        public async Task<ActionResult<IEnumerable<ClaseDocumento>>> GetClassDocumento()
        {
            try
            {
                return await _context.ClaseDocumentos
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

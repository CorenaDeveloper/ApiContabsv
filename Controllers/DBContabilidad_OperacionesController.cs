using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_OperacionesController : ControllerBase
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_OperacionesController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("Operaciones")]
        public async Task<ActionResult<IEnumerable<Operacione>>> GetOperaciones()
        {
            try
            {
                return await _context.Operaciones
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

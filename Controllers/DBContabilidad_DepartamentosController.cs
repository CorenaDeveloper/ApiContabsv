using ApiContabsv.Models.Contabilidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBContabilidad_DepartamentosController : Controller
    {
        private readonly ContabilidadContext _context;

        public DBContabilidad_DepartamentosController(ContabilidadContext context)
        {
            _context = context;
        }

        [HttpGet("Departamentos")]
        public async Task<ActionResult<IEnumerable<Departamento>>> GetDepartamentos()
        {
            try
            {
                return await _context.Departamentos
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }
}

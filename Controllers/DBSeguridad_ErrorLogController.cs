using ApiContabsv.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_ErrorLogController : ControllerBase
    {
        private readonly SeguridadContext _context;

        public DBSeguridad_ErrorLogController(SeguridadContext context)
        {
            _context = context;
        }

        // 🔵 LISTAR TODOS LOS ERRORES
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ErrorLog>>> GetErrores()
        {
            return await _context.ErrorLogs.ToListAsync();
        }

        // 🔵 OBTENER UN ERROR POR ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ErrorLog>> GetError(int id)
        {
            var error = await _context.ErrorLogs.FindAsync(id);
            if (error == null)
                return NotFound();

            return error;
        }

        // 🔵 CREAR UN NUEVO REGISTRO DE ERROR
        [HttpPost]
        public async Task<ActionResult<ErrorLog>> CreateError(ErrorLog error)
        {
            error.ErrorDateTime = DateTime.Now;
            _context.ErrorLogs.Add(error);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetError), new { id = error.ErrorId }, error);
        }
    }
}

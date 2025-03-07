using ApiContabsv.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_ModulosController : ControllerBase
    {
        private readonly SeguridadContext _context;

        public DBSeguridad_ModulosController(SeguridadContext context)
        {
            _context = context;
        }

        // 🔵 LISTAR TODOS LOS MÓDULOS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Modulo>>> GetModulos()
        {
            return await _context.Modulos.ToListAsync();
        }

        // 🔵 OBTENER UN MÓDULO POR ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Modulo>> GetModulo(int id)
        {
            var modulo = await _context.Modulos.FindAsync(id);
            if (modulo == null)
                return NotFound();

            return modulo;
        }

        // 🔵 CREAR UN NUEVO MÓDULO
        [HttpPost]
        public async Task<ActionResult<Modulo>> CreateModulo(Modulo modulo)
        {
            _context.Modulos.Add(modulo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetModulo), new { id = modulo.IdModulo }, modulo);
        }

        // 🔵 ACTUALIZAR UN MÓDULO
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModulo(int id, Modulo modulo)
        {
            if (id != modulo.IdModulo)
            {
                return BadRequest("El ID Modulo no existe.");
            }
            try
            {
                _context.Entry(modulo).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ModuloExists(id))
                {
                    return NotFound("Modulo no encontrado.");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // 🔵 ELIMINAR UN MÓDULO
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModulo(int id)
        {
            var modulo = await _context.Modulos.FindAsync(id);
            if (modulo == null)
                return NotFound();

            _context.Modulos.Remove(modulo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ModuloExists(int id)
        {
            return _context.Modulos.Any(e => e.IdModulo == id);
        }
    }
}

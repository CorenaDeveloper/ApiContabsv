using ApiContabsv.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_AccionxModelController : ControllerBase
    {
        private readonly SeguridadContext _context;

        public DBSeguridad_AccionxModelController(SeguridadContext context)
        {
            _context = context;
        }

        // GET: DBSeguridad_AccionxModel?idModulo=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccionesxModulo>>> GetAcciones(int idModulo)
        {
            return await _context.AccionesxModulos
                .Where(a => a.IdModulo == idModulo)
                .ToListAsync();
        }

        // GET: DBSeguridad_AccionxModel/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AccionesxModulo>> GetAccion(int id)
        {
            var accion = await _context.AccionesxModulos.FindAsync(id);

            if (accion == null)
            {
                return NotFound();
            }

            return accion;
        }

        // POST: DBSeguridad_AccionxModel
        [HttpPost]
        public async Task<ActionResult<AccionesxModulo>> PostAccion(AccionesxModulo accion)
        {
            _context.AccionesxModulos.Add(accion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAccion), new { id = accion.IdAccion }, accion);
        }

        // PUT: DBSeguridad_AccionxModel/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAccion(int id, AccionesxModulo accion)
        {
            if (id != accion.IdAccion)
            {
                return BadRequest();
            }

            _context.Entry(accion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AccionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: DBSeguridad_AccionxModel/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccion(int id)
        {
            var accion = await _context.AccionesxModulos.FindAsync(id);
            if (accion == null)
            {
                return NotFound();
            }

            _context.AccionesxModulos.Remove(accion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AccionExists(int id)
        {
            return _context.AccionesxModulos.Any(e => e.IdAccion == id);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiContabsv.Models.Seguridad;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_AppsController : ControllerBase
    {
        private readonly SeguridadContext _context;

        public DBSeguridad_AppsController(SeguridadContext context)
        {
            _context = context;
        }


        // 🔵 LISTAR TODAS LAS APPS
        [HttpGet("Apps")]
        public async Task<ActionResult<IEnumerable<App>>> GetApps()
        {
            try
            {
                return await _context.Apps
                       .Include(r => r.Modulos)
                       .Include(i => i.Rols)
                       .ToListAsync();
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }
 
        }

        // 🔵 OBTENER UNA APP POR ID
        [HttpGet("Apps/{id}")]
        public async Task<ActionResult<App>> GetApp(int id)
        {
            try
            {
                var result = await _context.Apps
                            .Include(r => r.Modulos)
                            .Include(i => i.Rols)
                            .FirstOrDefaultAsync(a => a.Id == id);

                return Ok(result);
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}"); ;
            }

        }

        // 🔵 CREAR UNA NUEVA APP
        [HttpPost("Apps")]
        public async Task<ActionResult<App>> CreateApp(App app)
        {
            app.FechaCreado = DateTime.Now;
            _context.Apps.Add(app);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetApp), new { id = app.Id }, app);
        }

        // 🔵 ACTUALIZAR UNA APP
        [HttpPut("Apps/{id}")]
        public async Task<IActionResult> UpdateApp(int id, App app)
        {
            if (id != app.Id)
            {
                return BadRequest("El ID app no existe.");
            }
            try
            {
                _context.Entry(app).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppsExists(id))
                {
                    return NotFound("App no encontrado.");
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

        // 🔵 ELIMINAR UNA APP
        [HttpDelete("Apps/{id}")]
        public async Task<IActionResult> DeleteApp(int id)
        {
            var app = await _context.Apps
                .Include(a => a.Modulos)
                .Include(a => a.Rols)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (app == null)
                return NotFound();


            _context.Modulos.RemoveRange(app.Modulos);
            _context.Rols.RemoveRange(app.Rols);
            _context.Apps.Remove(app);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool AppsExists(int id)
        {
            return _context.Apps.Any(e => e.Id == id);
        }
    }
}

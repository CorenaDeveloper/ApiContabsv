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
            // Usar una transacción para asegurar que todo se elimine correctamente
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var modulo = await _context.Modulos.FindAsync(id);
                if (modulo == null)
                    return NotFound(new { message = "Módulo no encontrado" });

                int permisosAccionesEliminados = 0;
                int accionesEliminadas = 0;
                int permisosModuloEliminados = 0;

                // 1️⃣ Obtener todas las acciones del módulo
                var acciones = await _context.AccionesxModulos
                    .Where(a => a.IdModulo == id)
                    .ToListAsync();

                // 2️⃣ Eliminar permisos de acciones asociadas
                if (acciones.Any())
                {
                    var idsAcciones = acciones.Select(a => a.IdAccion).ToList();

                    var permisosAcciones = await _context.PermisosxRols
                        .Where(p => p.IdAccion != null && idsAcciones.Contains(p.IdAccion.Value))
                        .ToListAsync();

                    if (permisosAcciones.Any())
                    {
                        _context.PermisosxRols.RemoveRange(permisosAcciones);
                        permisosAccionesEliminados = permisosAcciones.Count;
                        await _context.SaveChangesAsync();
                    }

                    // 3️⃣ Eliminar las acciones del módulo
                    _context.AccionesxModulos.RemoveRange(acciones);
                    accionesEliminadas = acciones.Count;
                    await _context.SaveChangesAsync();
                }

                // 4️⃣ Eliminar permisos de módulo
                var permisosModulo = await _context.PermisosxRols
                    .Where(p => p.IdModulo == id)
                    .ToListAsync();

                if (permisosModulo.Any())
                {
                    _context.PermisosxRols.RemoveRange(permisosModulo);
                    permisosModuloEliminados = permisosModulo.Count;
                    await _context.SaveChangesAsync();
                }

                // 5️⃣ Finalmente, eliminar el módulo
                _context.Modulos.Remove(modulo);
                await _context.SaveChangesAsync();

                // Confirmar la transacción
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Módulo eliminado correctamente",
                    detalles = new
                    {
                        permisosAccionesEliminados,
                        accionesEliminadas,
                        permisosModuloEliminados
                    }
                });
            }
            catch (Exception ex)
            {
                // Revertir la transacción en caso de error
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Error al eliminar el módulo",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        private bool ModuloExists(int id)
        {
            return _context.Modulos.Any(e => e.IdModulo == id);
        }
    }
}

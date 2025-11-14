using ApiContabsv.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_PermisosxRolController : ControllerBase
    {
        private readonly SeguridadContext _context;

        public DBSeguridad_PermisosxRolController(SeguridadContext context)
        {
            _context = context;
        }

        // 🔵 LISTAR TODOS LOS PERMISOS POR ROL
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PermisosxRol>>> GetPermisos(int idRol)
        {
            var permisos = await _context.PermisosxRols
                .Where(p => p.IdRol == idRol)
                .ToListAsync();

            return permisos;
        }

        // 🔵 OBTENER UN PERMISO POR ID
        [HttpGet("{id}")]
        public async Task<ActionResult<PermisosxRol>> GetPermiso(int id)
        {
            var permiso = await _context.PermisosxRols.FindAsync(id);
            if (permiso == null)
                return NotFound();

            return permiso;
        }

        // 🔵 LISTAR PERMISOS DE MÓDULOS (idAccion = NULL)
        [HttpGet("modulos")]
        public async Task<ActionResult<IEnumerable<PermisosxRol>>> GetPermisosModulos(int idRol)
        {
            return await _context.PermisosxRols
                .Where(p => p.IdRol == idRol && p.IdAccion == null)
                .ToListAsync();
        }

        // 🔵 LISTAR PERMISOS DE ACCIONES (idAccion != NULL)
        [HttpGet("acciones")]
        public async Task<ActionResult<IEnumerable<PermisosxRol>>> GetPermisosAcciones(int idRol)
        {
            return await _context.PermisosxRols
                .Where(p => p.IdRol == idRol && p.IdAccion != null)
                .ToListAsync();
        }

        // 🔵 CREAR UN NUEVO PERMISO
        [HttpPost]
        public async Task<ActionResult<PermisosxRol>> CreatePermiso(PermisosxRol permiso)
        {
            _context.PermisosxRols.Add(permiso);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPermiso), new { id = permiso.IdPermiso }, permiso);
        }

        // 🔵 ACTUALIZAR UN PERMISO
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermiso(int id, PermisosxRol permiso)
        {
            if (id != permiso.IdPermiso)
            {
                return BadRequest("El ID de Permiso no existe.");
            }
            try
            {
                _context.Entry(permiso).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PermisoExists(id))
                {
                    return NotFound("Permiso no encontrado.");
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

        // 🔵 ELIMINAR UN PERMISO
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermiso(int id)
        {
            var permiso = await _context.PermisosxRols.FindAsync(id);
            if (permiso == null)
                return NotFound();

            _context.PermisosxRols.Remove(permiso);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PermisoExists(int id)
        {
            return _context.PermisosxRols.Any(e => e.IdPermiso == id);
        }
    }
}

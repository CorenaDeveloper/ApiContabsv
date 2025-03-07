using ApiContabsv.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_RolesController : ControllerBase
    {
        private readonly SeguridadContext _context;

        public DBSeguridad_RolesController(SeguridadContext context)
        {
            _context = context;
        }

        // 🔵 LISTAR TODOS LOS ROLES
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rol>>> GetRoles()
        {
            return await _context.Rols.ToListAsync();
        }

        // 🔵 OBTENER UN ROL POR ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Rol>> GetRol(int id)
        {
            var rol = await _context.Rols.FindAsync(id);
            if (rol == null)
                return NotFound();

            return rol;
        }

        // 🔵 CREAR UN NUEVO ROL
        [HttpPost]
        public async Task<ActionResult<Rol>> CreateRol(Rol rol)
        {
            rol.FechaCreado = DateTime.Now;
            _context.Rols.Add(rol);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRol), new { id = rol.Id }, rol);
        }

        // 🔵 ACTUALIZAR UN ROL
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRol(int id, Rol rol)
        {
            if (id != rol.Id)
                return BadRequest();

            var existingRol = await _context.Rols.FindAsync(id);
            if (existingRol == null)
                return NotFound();

            existingRol.Nombre = rol.Nombre;
            existingRol.Estado = rol.Estado;
            existingRol.AppId = rol.AppId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔵 ELIMINAR UN ROL
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRol(int id)
        {
            var rol = await _context.Rols.FindAsync(id);
            if (rol == null)
                return NotFound();

            _context.Rols.Remove(rol);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

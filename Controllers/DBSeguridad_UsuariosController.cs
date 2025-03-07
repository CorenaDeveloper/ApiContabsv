
using ApiContabsv.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_UsuariosController : ControllerBase
    {
        private readonly SeguridadContext _context;

        public DBSeguridad_UsuariosController(SeguridadContext context)
        {
            _context = context;
        }

        // 🔵 LISTAR TODAS LAS USUARIO  
        [HttpGet("Usuarios")]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUds()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // 🔵 OBTENER UNA USUARIO POR ID
        [HttpGet("Usuarios/{id}")]
        public async Task<ActionResult<Usuario>> GetUd(int id)
        {
            var add = await _context.Usuarios.FindAsync(id);

            if (add == null)
                return NotFound();

            return add;
        }

        // 🔵 CREAR UNA NUEVA USUARIO
        [HttpPost("Usuarios")]
        public async Task<ActionResult<Usuario>> CreateUds(Usuario add)
        {
            add.FechaCreacion = DateTime.Now;
            _context.Usuarios.Add(add);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUds), new { id = add.IdUsuario }, add);
        }

        // 🔵 ACTUALIZAR UNA USUARIO
        [HttpPut("Usuarios/{id}")]
        public async Task<IActionResult> UpdatUds(int id, Usuario uds)
        {
            if (id != uds.IdUsuario)
                return BadRequest();

            var existingUds = await _context.Usuarios.FindAsync(id);
            if (existingUds == null)
                return NotFound();

            existingUds.Nombre = uds.Nombre;
            existingUds.Apellido = uds.Apellido;
            existingUds.Email = uds.Email;
            existingUds.Usuario1 = uds.Usuario1;
            existingUds.Contraseña = uds.Contraseña;
            existingUds.FechaCreacion = DateTime.Now;
            existingUds.Estado = uds.Estado;
            existingUds.IdCliente = uds.IdCliente;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 🔵 ELIMINAR UNA USUARIO
        [HttpDelete("Usuarios/{id}")]
        public async Task<IActionResult> DeleteUds(int id)
        {
            var uds = await _context.Usuarios.FindAsync(id);
            if (uds == null)
                return NotFound();

            _context.Usuarios.Remove(uds);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}

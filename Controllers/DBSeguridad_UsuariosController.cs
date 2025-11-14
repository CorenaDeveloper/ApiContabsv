
using ApiContabsv.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiContabsv.DTO.DB_SeguridadDTO;
using Swashbuckle.AspNetCore.Annotations;
using ApiContabsv.Services;

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


        [HttpGet("Usuarios")]
        [SwaggerOperation(Summary = "Lista todos los usuarios con sus apps")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsuarios()
        {
            try
            {
                var usuarios = await (from u in _context.Usuarios
                                      join r in _context.Rols on u.IdRol equals r.Id into rolGroup
                                      from r in rolGroup.DefaultIfEmpty()
                                      join a in _context.Apps on r.AppId equals a.Id into appGroup
                                      from a in appGroup.DefaultIfEmpty()
                                      select new
                                      {
                                          u.IdUsuario,
                                          u.Nombre,
                                          u.Apellido,
                                          u.Email,
                                          Usuario = u.Usuario1,
                                          u.Estado,
                                          u.IdCliente,
                                          u.FechaCreacion,
                                          Rol = r != null ? new
                                          {
                                              IdRol = r.Id,
                                              Nombre = r.Nombre,
                                              Estado = r.Estado
                                          } : null,
                                          App = a != null ? new
                                          {
                                              IdApp = a.Id,
                                              Nombre = a.Nombre,
                                              Url = a.Url,
                                              Estado = a.Estado
                                          } : null
                                      }).ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpGet("Usuarios/{id}")]
        [SwaggerOperation(Summary = "Consulta Usuario por Id")]
        public async Task<ActionResult<object>> GetUsuario(int id)
        {
            try
            {
                var usuario = await (from u in _context.Usuarios
                                     where u.IdUsuario == id
                                     join r in _context.Rols on u.IdRol equals r.Id into rolGroup
                                     from r in rolGroup.DefaultIfEmpty()
                                     join a in _context.Apps on r.AppId equals a.Id into appGroup
                                     from a in appGroup.DefaultIfEmpty()
                                     select new
                                     {
                                         u.IdUsuario,
                                         u.Nombre,
                                         u.Apellido,
                                         u.Email,
                                         Usuario = u.Usuario1,
                                         u.Estado,
                                         u.IdCliente,
                                         u.FechaCreacion,
                                         Rol = r != null ? new
                                         {
                                             IdRol = r.Id,
                                             Nombre = r.Nombre,
                                             Estado = r.Estado
                                         } : null,
                                         App = a != null ? new
                                         {
                                             IdApp = a.Id,
                                             Nombre = a.Nombre,
                                             Url = a.Url,
                                             Estado = a.Estado
                                         } : null
                                     }).FirstOrDefaultAsync();

                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado.");
                }

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }


        [HttpPost("Usuarios")]
        [SwaggerOperation(Summary = "Crea un nuevo usuario")]
        public async Task<ActionResult<CreateUsuarioDTO>> CreateUds(CreateUsuarioDTO a)
        {
            try
            {
                var pass = HashPasswordSHA256.HashPassword(a.Pass);

                var b = new Usuario
                {
                    IdUsuario = 0,
                    Nombre = a.Nombre,
                    Apellido = a.Apellido,
                    Email = a.Email,
                    Usuario1 = a.Usuario,
                    Contraseña = pass,
                    FechaCreacion = DateTime.Now,
                    Estado = true,
                    IdCliente = a.idCliente,
                    IdRol = a.idRol
                };

                _context.Add(b);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUsuario), new { id = b.IdUsuario }, b);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }

        }

        [HttpPut("Usuarios/{id}")]
        public async Task<IActionResult> UpdatUds(int id, UpdateUsuarioDTO uds)
        {
            if (id != uds.IdUsuario)
                return BadRequest();

            var existingUds = await _context.Usuarios.FindAsync(id);
            if (existingUds == null)
                return NotFound();

            existingUds.Nombre = uds.Nombre;
            existingUds.Apellido = uds.Apellido;
            existingUds.Email = uds.Email;
            existingUds.Usuario1 = uds.Usuario;
            existingUds.FechaCreacion = DateTime.Now;
            existingUds.Estado = uds.Estado;
            existingUds.IdRol = uds.IdRol;
            existingUds.IdCliente = uds.IdCliente;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("Usuarios/{id}/CambiarPassword")]
        [SwaggerOperation(Summary = "Cambia la contraseña de un usuario")]
        public async Task<IActionResult> CambiarPassword(int id, CambiarPasswordDTO request)
        {
            try
            {
                if (id != request.IdUsuario)
                    return BadRequest("El ID del usuario no coincide");

                // Validar que los campos no estén vacíos
                if (string.IsNullOrEmpty(request.PasswordActual) || string.IsNullOrEmpty(request.PasswordNuevo))
                    return BadRequest("La contraseña actual y nueva son requeridas");

                // Validar longitud mínima de nueva contraseña
                if (request.PasswordNuevo.Length < 6)
                    return BadRequest("La nueva contraseña debe tener al menos 6 caracteres");

                // Buscar el usuario
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                    return NotFound("Usuario no encontrado");

                // Validar contraseña actual
                var passwordActualHasheada = HashPasswordSHA256.HashPassword(request.PasswordActual);
                if (usuario.Contraseña != passwordActualHasheada)
                    return BadRequest("La contraseña actual es incorrecta");

                // Hashear nueva contraseña
                var passwordNuevaHasheada = HashPasswordSHA256.HashPassword(request.PasswordNuevo);

                // Actualizar contraseña
                usuario.Contraseña = passwordNuevaHasheada;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Contraseña actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }
        }

        [HttpDelete("Usuarios/{id}")]
        [SwaggerOperation(Summary = "Elimina un Usuario")]
        public async Task<IActionResult> DeleteUds(int id)
        {
            try
            {
                var uds = await _context.Usuarios.FindAsync(id);
                if (uds == null)
                {
                    return BadRequest("Usuario no encontrado");
                };

                _context.Usuarios.Remove(uds);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception.";
                return StatusCode(500, $"Error interno: {ex.Message}. Detalles: {inner}");
            }

        }

    }
}

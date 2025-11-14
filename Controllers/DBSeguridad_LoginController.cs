using ApiContabsv.Models.Contabsv;
using ApiContabsv.Models.Seguridad;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_LoginController : Controller
    {
        private readonly SeguridadContext _context;
        private readonly ContabsvContext _contabsv_context;
        public DBSeguridad_LoginController(SeguridadContext context, ContabsvContext contextConta)
        {
            _context = context;
            _contabsv_context = contextConta;
        }


        public class LoginRequest
        {
            public string Usuario { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                string hashedPassword = HashPasswordSHA256.HashPassword(request.Password);
                var u = await _context.Usuarios
                        .FirstOrDefaultAsync(u => u.Usuario1 == request.Usuario && u.Contraseña == hashedPassword);

                if (u == null)
                {
                    return Unauthorized(new { message = "Usuario o contraseña incorrectos" });
                }

                var idCliente = u.IdCliente;

                var c = await _contabsv_context.Clientes
                        .FirstOrDefaultAsync(c => c.IdCliente == idCliente);

                if (c == null)
                {
                    return NotFound(new { message = "Cliente no encontrado" });
                }

                var r = new
                {
                    u.IdUsuario,
                    u.Nombre,
                    u.Apellido,
                    u.Email,
                    u.Estado,
                    c.IdCliente,
                    c.PersonaJuridica
                };

                return Ok(r);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        [HttpGet("Login/Permisos")]
        public async Task<IActionResult> PermisosXUsuario(int idUsuario)
        {
            try
            {
                var jsonOutput = new SqlParameter
                {
                    ParameterName = "@json",
                    SqlDbType = SqlDbType.VarChar,
                    Size = -1, // -1 para VARCHAR(MAX)
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                 "EXEC sp_GetPermisosUsuario @IdUsuario, @json OUTPUT",
                   new SqlParameter("@IdUsuario", idUsuario),
                   jsonOutput
                );

                var jsonResult = jsonOutput.Value?.ToString();

                if (!string.IsNullOrEmpty(jsonResult))
                {
                    return Content(jsonResult, "application/json");
                }
                else
                {
                    return StatusCode(404, "Lista: No se encontró ningún resultado");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

    }
}
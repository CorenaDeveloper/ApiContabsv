using ApiContabsv.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_LoginController : Controller
    {
        private readonly SeguridadContext _context;

        public DBSeguridad_LoginController(SeguridadContext context)
        {
            _context = context;
        }

        // 🔹 LOGIN DE USUARIO
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Encriptar la contraseña ingresada
                string hashedPassword = HashPasswordSHA256.HashPassword(request.Password);

                // Buscar usuario en la base de datos
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Usuario1 == request.Usuario && u.Contraseña == hashedPassword);

                if (usuario == null)
                    return Unauthorized(new { message = "Usuario o contraseña incorrectos" });

                // Retornar usuario o token en caso de que agregues autenticación más adelante
                return Ok(new { message = "Login exitoso", usuario.IdUsuario, usuario.Nombre, usuario.Email, usuario.IdCliente });
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Error interno: {ex.Message}");
            }

        }
    }

    // 🔹 Modelo para recibir el login
    public class LoginRequest
    {
        public string Usuario { get; set; }
        public string Password { get; set; }
    }

    // 🔹 Clase de hash SHA-256 (estática para uso en cualquier parte)
    public static class HashPasswordSHA256
    {
        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
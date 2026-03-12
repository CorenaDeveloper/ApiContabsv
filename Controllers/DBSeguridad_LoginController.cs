using ApiContabsv.Models.Contabsv;
using ApiContabsv.Models.Seguridad;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBSeguridad_LoginController : Controller
    {
        private const int MaxIntentos = 5;
        private const int MinutosBloqueo = 15;

        private readonly SeguridadContext _context;
        private readonly ContabsvContext _contabsv_context;
        private readonly IEmailService _emailService;
        private readonly IDispositivoService _dispositivoService;

        public DBSeguridad_LoginController(
            SeguridadContext context,
            ContabsvContext contextConta,
            IEmailService emailService,
            IDispositivoService dispositivoService)
        {
            _context = context;
            _contabsv_context = contextConta;
            _emailService = emailService;
            _dispositivoService = dispositivoService;
        }

        public class LoginRequest
        {
            public string Usuario { get; set; }
            public string Password { get; set; }
            public string TokenDispositivo { get; set; }
        }

        public class VerificarCodigoRequest
        {
            public int IdUsuario { get; set; }
            public string Codigo { get; set; }
            public string TokenDispositivo { get; set; }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var u = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Usuario1 == request.Usuario);

                if (u == null)
                    return Unauthorized(new { message = "Usuario o contraseña incorrectos" });

                if (u.BloqueadoHasta.HasValue && u.BloqueadoHasta > DateTime.Now)
                {
                    var minutosRestantes = (int)Math.Ceiling((u.BloqueadoHasta.Value - DateTime.Now).TotalMinutes);
                    return Unauthorized(new { message = $"Usuario bloqueado. Intenta nuevamente en {minutosRestantes} minuto(s)." });
                }

                string hashedPassword = HashPasswordSHA256.HashPassword(request.Password);

                if (u.Contraseña != hashedPassword)
                {
                    u.IntentosFallidos++;

                    if (u.IntentosFallidos >= MaxIntentos)
                    {
                        u.BloqueadoHasta = DateTime.Now.AddMinutes(MinutosBloqueo);
                        u.IntentosFallidos = 0;
                        await _context.SaveChangesAsync();
                        return Unauthorized(new { message = $"Demasiados intentos fallidos. Usuario bloqueado por {MinutosBloqueo} minutos." });
                    }

                    var intentosRestantes = MaxIntentos - u.IntentosFallidos;
                    await _context.SaveChangesAsync();
                    return Unauthorized(new { message = $"Usuario o contraseña incorrectos. Te quedan {intentosRestantes} intento(s)." });
                }

                // Login exitoso - resetear contadores
                u.IntentosFallidos = 0;
                u.BloqueadoHasta = null;
                await _context.SaveChangesAsync();

                // Verificar si el dispositivo es confiable
                var esConfiable = !string.IsNullOrEmpty(request.TokenDispositivo)
                    && await _dispositivoService.EsDispositivoConfiableAsync(u.IdUsuario, request.TokenDispositivo);

                if (!esConfiable)
                {
                    // Generar y guardar código de verificación
                    var codigo = new Random().Next(100000, 999999).ToString();
                    u.CodigoVerificacion = codigo;
                    u.CodigoExpiracion = DateTime.Now.AddMinutes(10);
                    await _context.SaveChangesAsync();

                    // Enviar correo
                    var html = $@"
                        <h2>Verificación de nuevo dispositivo</h2>
                        <p>Hola {u.Nombre}, detectamos un acceso desde un dispositivo nuevo.</p>
                        <p>Tu código de verificación es:</p>
                        <h1 style='color:#2d6a4f;letter-spacing:8px'>{codigo}</h1>
                        <p>Este código expira en 10 minutos.</p>
                        <p>Si no fuiste tú, cambia tu contraseña inmediatamente.</p>";

                    await _emailService.SendEmailAsync(u.Email, $"{u.Nombre} {u.Apellido}",
                        "Código de verificación - ContabSV", html);

                    return Ok(new { requiereVerificacion = true, idUsuario = u.IdUsuario });
                }

                return await BuildLoginResponse(u);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPost("Login/VerificarCodigo")]
        public async Task<IActionResult> VerificarCodigo([FromBody] VerificarCodigoRequest request)
        {
            try
            {
                var u = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.IdUsuario == request.IdUsuario);

                if (u == null)
                    return Unauthorized(new { message = "Usuario no encontrado" });

                if (u.CodigoVerificacion != request.Codigo || u.CodigoExpiracion < DateTime.Now)
                    return Unauthorized(new { message = "Código inválido o expirado" });

                // Limpiar código y registrar dispositivo
                u.CodigoVerificacion = null;
                u.CodigoExpiracion = null;
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(request.TokenDispositivo))
                    await _dispositivoService.RegistrarDispositivoAsync(u.IdUsuario, request.TokenDispositivo);

                return await BuildLoginResponse(u);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        private async Task<IActionResult> BuildLoginResponse(Usuario u)
        {
            var c = await _contabsv_context.Clientes
                .FirstOrDefaultAsync(c => c.IdCliente == u.IdCliente);

            if (c == null)
                return NotFound(new { message = "Cliente no encontrado" });

            return Ok(new
            {
                requiereVerificacion = false,
                u.IdUsuario,
                u.Nombre,
                u.Apellido,
                u.Email,
                u.Estado,
                c.IdCliente,
                c.PersonaJuridica,
                c.UserDte,
                c.Ambiente,
                c.ProcesaInventario
            });
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
                    Size = -1,
                    Direction = ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_GetPermisosUsuario @IdUsuario, @json OUTPUT",
                    new SqlParameter("@IdUsuario", idUsuario),
                    jsonOutput
                );

                var jsonResult = jsonOutput.Value?.ToString();

                if (!string.IsNullOrEmpty(jsonResult))
                    return Content(jsonResult, "application/json");

                return StatusCode(404, "Lista: No se encontró ningún resultado");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
using ApiContabsv.Models.Dte;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_TestController : ControllerBase
    {
        private readonly IHaciendaService _haciendaService;
        private readonly dteContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DBDte_TestController> _logger;

        public DBDte_TestController(
            IHaciendaService haciendaService,
            dteContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<DBDte_TestController> logger)
        {
            _haciendaService = haciendaService;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// PROBAR AUTENTICACIÓN CON HACIENDA
        /// </summary>
        [HttpPost("hacienda-auth")]
        public async Task<ActionResult> TestHaciendaAuth([FromBody] TestHaciendaAuthRequestDTO request)
        {
            try
            {
                var authResult = await _haciendaService.AuthenticateUser(
                    request.UserHacienda,
                    request.PassHacienda,
                    request.Ambiente ?? "00"
                );

                return Ok(new
                {
                    success = authResult.Success,
                    hasToken = !string.IsNullOrEmpty(authResult.Token),
                    token = authResult.Success ? "TOKEN_OBTENIDO" : null, 
                    tokenType = authResult.TokenType,
                    error = authResult.Success ? null : authResult.Error,  
                    errorDetails = authResult.Success ? null : authResult.ErrorDetails, 
                    requestSent = new
                    {
                        user = request.UserHacienda,
                        passwordLength = request.PassHacienda?.Length ?? 0,
                        ambiente = request.Ambiente
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// VER CREDENCIALES DE UN USUARIO
        /// </summary>
        [HttpGet("user-credentials/{nit}")]
        public async Task<ActionResult> GetUserCredentials(string nit)
        {
            var user = await _context.Users
                .Where(u => u.Nit == nit)
                .Select(u => new {
                    u.Id,
                    u.Nit,
                    PasswordPriLength = u.PasswordPri != null ? u.PasswordPri.Length : 0,
                    PasswordPriPreview = u.PasswordPri != null ? u.PasswordPri.Substring(0, Math.Min(4, u.PasswordPri.Length)) + "***" : null,
                    JwtSecretLength = u.JwtSecret != null ? u.JwtSecret.Length : 0,
                    HasPasswordPri = !string.IsNullOrEmpty(u.PasswordPri),
                    HasJwtSecret = !string.IsNullOrEmpty(u.JwtSecret)
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                found = user != null,
                data = user,
                timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// HEALTH CHECK GENERAL
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult> HealthCheck()
        {
            try
            {
                // Test BD
                var userCount = await _context.Users.CountAsync();

                return Ok(new
                {
                    status = "healthy",
                    database = new
                    {
                        connected = true,
                        userCount = userCount
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }
    }

    // DTOs
    public class TestHaciendaAuthRequestDTO
    {
        public string UserHacienda { get; set; } = "";
        public string PassHacienda { get; set; } = "";
        public string? Ambiente { get; set; } = "00";
    }
}
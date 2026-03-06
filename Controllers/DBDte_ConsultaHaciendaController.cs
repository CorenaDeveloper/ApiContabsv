using ApiContabsv.Models.Dte;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_ConsultaHaciendaController : ControllerBase
    {
        private readonly IHaciendaService _haciendaService;
        private readonly dteContext _context;

        public DBDte_ConsultaHaciendaController(
            IHaciendaService haciendaService,
            dteContext context)
        {
            _haciendaService = haciendaService;
            _context = context;
        }

        /// <summary>
        /// CONSULTAR DTE EN HACIENDA POR CÓDIGO DE GENERACIÓN
        /// Igual que registrar CCF/FCF pero para consultar DTEs recibidos (compras).
        /// GET /DBDte_ConsultaHacienda/consultar?userId=5&codGen=070161AF-DA97-40C1-BDB3-5977C49E8972&ambiente=01
        /// </summary>
        [HttpGet("consultar")]
        public async Task<ActionResult> ConsultarDTE( [FromQuery] int userId, [FromQuery] string codGen,[FromQuery] string? tdte, [FromQuery] string ambiente)
        {
            if (userId <= 0)
                return BadRequest(new { success = false, error = "userId es requerido" });

            if (string.IsNullOrWhiteSpace(codGen) || codGen.Trim().Length != 36)
                return BadRequest(new { success = false, error = "codGen debe tener 36 caracteres" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return BadRequest(new { success = false, error = $"Usuario con ID {userId} no encontrado" });

            var resultado = await _haciendaService.ConsultarDTE(user.Nit, codGen.Trim().ToUpper(), ambiente, tdte ?? "03");

            if (resultado == null)
                return Ok(new { success = false, error = "No se pudo consultar en Hacienda" });

            return Ok(new { success = true, data = resultado });
        }
    }
}
using ApiContabsv.Models.Dte;
using ApiContabsv.DTO.DB_DteDTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_DocAnuladosController : ControllerBase
    {
        private readonly dteContext _context;
        private readonly ILogger<DBDte_DocAnuladosController> _logger;

        public DBDte_DocAnuladosController(
            dteContext context,
            ILogger<DBDte_DocAnuladosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// LISTAR TODOS LOS DOCUMENTOS INVALIDADOS/ANULADOS DE UN USUARIO
        /// GET /DBDte_DocAnulados/user/{userId}?startDate=2026-01-01&endDate=2026-01-31&ambiente=00
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult> GetDocumentosAnulados(
            int userId,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] string ambiente = "")
        {
            try
            {
                // Parsear fechas
                DateTime? parsedStartDate = null;
                DateTime? parsedEndDate = null;

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out DateTime start))
                    parsedStartDate = start;

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out DateTime end))
                    parsedEndDate = end;

                // Query base: filtra por userId, ambiente y status INVALIDADO
                var query = _context.DteDocuments
                    .Where(d => d.UserId == userId
                             && d.Status == "INVALIDADO"
                             && d.Ambiente == ambiente);

                // Filtro por fecha inicio
                if (parsedStartDate.HasValue)
                    query = query.Where(d => d.CreatedAt >= parsedStartDate.Value);

                // Filtro por fecha fin (hasta fin del día)
                if (parsedEndDate.HasValue)
                {
                    var endOfDay = parsedEndDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(d => d.CreatedAt <= endOfDay);
                }

                var documents = await query
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new
                    {
                        d.Id,
                        d.DteId,
                        d.UserId,
                        d.DocumentType,
                        d.Status,
                        d.JsonContent,
                        d.CreatedAt,
                        d.UpdatedAt,
                        d.Ambiente,
                        d.ReceptionStamp,
                        d.HaciendaResponse,
                        d.ResponseCode,
                        d.ErrorMessage,
                        d.ErrorDetails
                    })
                    .ToListAsync();

                // Enriquecer con número de control y monto desde dte_details
                var dteIds = documents.Select(d => d.DteId).ToList();
                var details = await _context.DteDetails
                    .Where(d => dteIds.Contains(d.DteId))
                    .ToDictionaryAsync(d => d.DteId);

                var result = documents.Select(d =>
                {
                    details.TryGetValue(d.DteId, out var det);
                    return new
                    {
                        d.Id,
                        d.DteId,
                        d.UserId,
                        d.DocumentType,
                        TipoDocumentoNombre = d.DocumentType switch
                        {
                            "01" => "Factura Consumidor Final",
                            "03" => "Crédito Fiscal (CCF)",
                            "05" => "Nota de Crédito",
                            "06" => "Nota de Débito",
                            "11" => "Factura de Exportación",
                            _ => $"Tipo {d.DocumentType}"
                        },
                        d.Status,
                        ControlNumber = det?.ControlNumber ?? "",
                        TotalAmount = det?.TotalAmount ?? 0,
                        d.CreatedAt,
                        d.UpdatedAt,
                        d.Ambiente,
                        d.ReceptionStamp,
                        d.HaciendaResponse,
                        d.ResponseCode,
                        d.ErrorMessage,
                        d.ErrorDetails,
                        ReceptorNombre = ObtenerCampoJson(d.JsonContent, "receptor", "nombre"),
                        ReceptorNumDoc = ObtenerCampoJson(d.JsonContent, "receptor", "numDocumento"),
                        NumeroControl = det?.ControlNumber ?? ObtenerIdentificacion(d.JsonContent, "numeroControl"),
                        CodigoGeneracion = d.DteId
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    total = result.Count,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo documentos anulados para userId={UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno obteniendo documentos anulados",
                    error = ex.Message
                });
            }
        }

        // ─── Helpers para extraer datos básicos del JSON ───

        private static string ObtenerCampoJson(string? jsonContent, string seccion, string campo)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonContent)) return "";
                using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
                if (doc.RootElement.TryGetProperty(seccion, out var sec) &&
                    sec.TryGetProperty(campo, out var val))
                    return val.GetString() ?? "";
            }
            catch { }
            return "";
        }

        private static string ObtenerIdentificacion(string? jsonContent, string campo)
        {
            {
                try
                {
                    if (string.IsNullOrEmpty(jsonContent)) return "";
                    using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
                    if (doc.RootElement.TryGetProperty("identificacion", out var id) &&
                        id.TryGetProperty(campo, out var val))
                        return val.GetString() ?? "";
                }
                catch { }
                return "";
            }
        }
    }
}
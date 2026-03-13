using ApiContabsv.Models.Contabsv;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DBDte_EmailController : ControllerBase
    {
        private readonly ContabsvContext _contabsvContext;
        private readonly IEmailClienteService _emailClienteService;

        public DBDte_EmailController(ContabsvContext contabsvContext, IEmailClienteService emailClienteService)
        {
            _contabsvContext = contabsvContext;
            _emailClienteService = emailClienteService;
        }

        [HttpPost("EnviarDocumento")]
        public async Task<IActionResult> EnviarDocumento([FromForm] EnviarDocumentoRequest request)
        {
            try
            {
                var cliente = await _contabsvContext.Clientes.FirstOrDefaultAsync(c => c.IdCliente == request.IdCliente);
                if (cliente == null)
                    return NotFound(new { success = false, message = "Cliente no encontrado" });

                byte[]? pdfBytes = null;
                if (request.Pdf != null)
                {
                    using var ms = new MemoryStream();
                    await request.Pdf.CopyToAsync(ms);
                    pdfBytes = ms.ToArray();
                }

                string? jsonString = null;
                if (request.Json != null)
                {
                    using var ms = new MemoryStream();
                    await request.Json.CopyToAsync(ms);
                    jsonString = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                }

                var html = $@"
                    <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto'>
                        <h2 style='color:#0d6efd'>{request.TipoDocumento}</h2>
                        <p>Estimado(a), adjunto encontrará su documento tributario electrónico.</p>
                        <p><b>Número de control:</b> {request.NumeroControl}</p>
                        <hr/>
                        <p style='color:#888;font-size:12px'>Enviado por {cliente.NombreComercial ?? cliente.NombreRazonSocial} a través de ContabSV.</p>
                    </div>";

                var enviado = await _emailClienteService.EnviarEmailAsync(
                    cliente,
                    request.CorreoDestino,
                    request.NombreDestinatario ?? "Cliente",
                    $"{request.TipoDocumento} - {request.NumeroControl}",
                    html,
                    pdfBytes,
                    jsonString,
                    request.NombreArchivo
                );

                if (enviado)
                    return Ok(new { success = true });

                return StatusCode(500, new { success = false, message = "Error al enviar el correo" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class EnviarDocumentoRequest
    {
        public int IdCliente { get; set; }
        public string CorreoDestino { get; set; } = "";
        public string? NombreDestinatario { get; set; }
        public string TipoDocumento { get; set; } = "";
        public string NumeroControl { get; set; } = "";
        public string NombreArchivo { get; set; } = "documento";
        public IFormFile? Pdf { get; set; }
        public IFormFile? Json { get; set; }
    }
}
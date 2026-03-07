
using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ApiExplorerSettings(GroupName = "contabilidad")]
    public class DBContabilidad_ImportarDteController : ControllerBase
    {
        private readonly IImportarDteService _importarDteService;
        private readonly ILogger<DBContabilidad_ImportarDteController> _logger;

        public DBContabilidad_ImportarDteController(
            IImportarDteService importarDteService,
            ILogger<DBContabilidad_ImportarDteController> logger)
        {
            _importarDteService = importarDteService;
            _logger = logger;
        }

        /// <summary>
        /// PASO 1 — PREVISUALIZAR: Valida los JSONs DTE contra Hacienda sin guardar nada.
        /// Devuelve el estado de cada documento para que el usuario revise antes de confirmar.
        /// POST /DBContabilidad_ImportarDte/previsualizar
        /// </summary>
        [HttpPost("previsualizar")]
        [SwaggerOperation(
            Summary = "PREVISUALIZAR IMPORTACIÓN MASIVA DE DTEs",
            Description = "Valida cada JSON DTE contra Hacienda y devuelve el estado de cada documento. " +
                          "NO guarda nada en base de datos. Usar antes de confirmar la importación."
        )]
        [SwaggerResponse(200, "Previsualización completada", typeof(ImportarDteMasivoResponse))]
        [SwaggerResponse(400, "Solicitud incorrecta")]
        [SwaggerResponse(500, "Error interno")]
        public async Task<ActionResult<ImportarDteMasivoResponse>> Previsualizar(
            [FromBody] ImportarDteMasivoRequest request)
        {
            if (request.IdCliente <= 0)
                return BadRequest(new { success = false, error = "IdCliente es requerido." });

            if (request.Documentos == null || request.Documentos.Count == 0)
                return BadRequest(new { success = false, error = "Debe enviar al menos un documento." });

            if (request.Documentos.Count > 300)
                return BadRequest(new { success = false, error = "Máximo 300 documentos por importación." });

            try
            {
                var resultado = await _importarDteService.ValidarDocumentos(request);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en previsualización masiva para cliente {IdCliente}", request.IdCliente);
                return StatusCode(500, new { success = false, error = $"Error interno: {ex.Message}" });
            }
        }

        /// <summary>
        /// PASO 2 — CONFIRMAR: Valida y guarda los DTEs en el Libro de Compras.
        /// Crea proveedores automáticamente si no existen.
        /// POST /DBContabilidad_ImportarDte/confirmar
        /// </summary>
        [HttpPost("confirmar")]
        [SwaggerOperation(
            Summary = "CONFIRMAR IMPORTACIÓN MASIVA DE DTEs",
            Description = "Guarda los DTEs validados en el Libro de Compras. " +
                          "Crea automáticamente los proveedores que no existan en el sistema. " +
                          "Solo guarda documentos con estado VALIDO o SIN_SELLO (si el usuario lo acepta)."
        )]
        [SwaggerResponse(200, "Importación completada", typeof(ImportarDteMasivoResponse))]
        [SwaggerResponse(400, "Solicitud incorrecta")]
        [SwaggerResponse(500, "Error interno")]
        public async Task<ActionResult<ImportarDteMasivoResponse>> Confirmar(
            [FromBody] ConfirmarImportacionRequest request)
        {
            if (request.IdCliente <= 0)
                return BadRequest(new { success = false, error = "IdCliente es requerido." });

            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { success = false, error = "Debe enviar al menos un documento para confirmar." });

            if (request.Items.Count > 300)
                return BadRequest(new { success = false, error = "Máximo 300 documentos por importación." });

            try
            {
                var resultado = await _importarDteService.ValidarYGuardar(request);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en confirmación masiva para cliente {IdCliente}", request.IdCliente);
                return StatusCode(500, new { success = false, error = $"Error interno: {ex.Message}" });
            }
        }
    }
}
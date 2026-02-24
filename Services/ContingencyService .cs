using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Dte;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApiContabsv.Services
{
    public interface IContingencyService
    {
        /// <summary>
        /// Determina si un error debe ir a contingencia.
        /// RECHAZADO por MH → NO. Error de red/timeout/MH caído → SÍ.
        /// </summary>
        bool ShouldGoToContingency(HaciendaTransmissionResult? result, Exception? ex = null);

        /// <summary>
        /// Guarda el documento en contingency_document (status=PENDIENTE)
        /// y actualiza dte_document a status=CONTINGENCIA.
        /// No requiere cambios en la BD — el dteId viaja dentro del JsonContent.
        /// </summary>
        Task StoreInContingency(
            string dteId,
            int userId,
            string documentType,
            string signedJWT,
            string userNit,
            string ambiente,
            int version,
            HaciendaTransmissionResult? failedResult = null);

        /// <summary>
        /// Retransmite documentos en estado PENDIENTE.
        /// Llamado por el BackgroundService cada X minutos.
        /// </summary>
        Task RetransmitPendingDocuments();
    }

    public class ContingencyService : IContingencyService
    {
        private readonly dteContext _context;
        private readonly IHaciendaService _haciendaService;
        private readonly IDTEDocumentService _documentService;
        private readonly ILogger<ContingencyService> _logger;

        private const string STATUS_PENDIENTE = "PENDIENTE";
        private const string STATUS_PROCESADO = "PROCESADO";
        private const string STATUS_FALLIDO = "FALLIDO";
        private const string DTE_CONTINGENCIA = "CONTINGENCIA";
        private const string DTE_PROCESADO = "PROCESADO";

        public ContingencyService(
            dteContext context,
            IHaciendaService haciendaService,
            IDTEDocumentService documentService,
            ILogger<ContingencyService> logger)
        {
            _context = context;
            _haciendaService = haciendaService;
            _documentService = documentService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────
        // CLASIFICACIÓN DEL ERROR
        // ─────────────────────────────────────────────────────────────

        public bool ShouldGoToContingency(HaciendaTransmissionResult? result, Exception? ex = null)
        {
            // Si MH lo rechazó explícitamente → NO es contingencia, es definitivo
            if (result?.Status == "RECHAZADO")
                return false;

            // Si fue exitoso → no aplica
            if (result?.Success == true)
                return false;

            // Excepción de red o timeout → contingencia
            if (ex is HttpRequestException || ex is TaskCanceledException || ex is TimeoutException)
                return true;

            // Códigos de error que indican problema de comunicación
            if (result != null)
            {
                var code = (result.ResponseCode ?? "").ToUpper();
                var error = (result.Error ?? "").ToUpper();

                if (code == "HTTP_ERROR" || code == "INTERNAL_ERROR")
                    return true;

                if (error.Contains("CONEXI") || error.Contains("CONNECTION") ||
                    error.Contains("TIMEOUT") || error.Contains("DISPONIBLE") ||
                    error.Contains("SERVICE UNAVAILABLE") || error.Contains("503") ||
                    error.Contains("504"))
                    return true;
            }

            // Sin resultado y sin excepción → asumir error de comunicación
            if (result == null && ex == null)
                return true;

            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // GUARDAR EN CONTINGENCIA
        // El dteId lo guardamos DENTRO del JsonContent para no tocar la BD
        // ─────────────────────────────────────────────────────────────

        public async Task StoreInContingency(
            string dteId,
            int userId,
            string documentType,
            string signedJWT,
            string userNit,
            string ambiente,
            int version,
            HaciendaTransmissionResult? failedResult = null)
        {
            try
            {
                var payload = new ContingencyPayload
                {
                    DteId = dteId,       // clave para retransmitir y actualizar
                    SignedJWT = signedJWT,
                    UserNit = userNit,
                    Ambiente = ambiente,
                    Version = version,
                    OriginalError = failedResult?.Error
                };

                var contingencyDoc = new ContingencyDocument
                {
                    UserId = userId,
                    ContingencyId = Guid.NewGuid().ToString().ToUpper(),
                    DocumentType = documentType,
                    Status = STATUS_PENDIENTE,
                    JsonContent = JsonSerializer.Serialize(payload),
                    CreatedAt = DateTime.Now
                };

                _context.ContingencyDocuments.Add(contingencyDoc);
                await _context.SaveChangesAsync();

                // Actualizar el dte_document a CONTINGENCIA
                await _documentService.UpdateDocumentStatus(
                    dteId,
                    DTE_CONTINGENCIA,
                    errorMessage: failedResult?.Error ?? "Pendiente de retransmisión por contingencia",
                    errorDetails: failedResult?.ErrorDetails);

                _logger.LogInformation(
                    "Documento {DteId} guardado en contingencia (contingency_document.id={Id})",
                    dteId, contingencyDoc.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando documento {DteId} en contingencia", dteId);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // RETRANSMISIÓN PERIÓDICA
        // ─────────────────────────────────────────────────────────────

        public async Task RetransmitPendingDocuments()
        {
            var pending = await _context.ContingencyDocuments
                .Where(d => d.Status == STATUS_PENDIENTE)
                .OrderBy(d => d.CreatedAt)
                .Take(50)
                .ToListAsync();

            if (!pending.Any())
            {
                _logger.LogInformation("No hay documentos pendientes en contingencia.");
                return;
            }

            _logger.LogInformation("Retransmitiendo {Count} documentos en contingencia...", pending.Count);

            int ok = 0, fail = 0;
            foreach (var doc in pending)
            {
                try
                {
                    await RetransmitOne(doc);
                    ok++;
                }
                catch (Exception ex)
                {
                    fail++;
                    _logger.LogError(ex,
                        "Error retransmitiendo contingencia id={Id}", doc.Id);
                }
            }

            _logger.LogInformation(
                "Retransmisión completada — exitosos: {Ok}, fallidos: {Fail}", ok, fail);
        }

        private async Task RetransmitOne(ContingencyDocument doc)
        {
            if (string.IsNullOrEmpty(doc.JsonContent))
            {
                _logger.LogWarning("Contingencia id={Id} sin JsonContent, marcando FALLIDO", doc.Id);
                await SetStatus(doc, STATUS_FALLIDO);
                return;
            }

            ContingencyPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<ContingencyPayload>(doc.JsonContent);
            }
            catch
            {
                _logger.LogWarning("Contingencia id={Id} con JsonContent inválido, marcando FALLIDO", doc.Id);
                await SetStatus(doc, STATUS_FALLIDO);
                return;
            }

            if (payload == null || string.IsNullOrEmpty(payload.DteId))
            {
                _logger.LogWarning("Contingencia id={Id} sin DteId en payload, marcando FALLIDO", doc.Id);
                await SetStatus(doc, STATUS_FALLIDO);
                return;
            }

            _logger.LogInformation(
                "Retransmitiendo contingencia id={Id} dteId={DteId} tipo={Tipo}",
                doc.Id, payload.DteId, doc.DocumentType);

            var result = await _haciendaService.TransmitDocument(
                payload.SignedJWT,
                payload.UserNit,
                payload.Ambiente,
                doc.DocumentType,
                payload.Version);

            if (result?.Success == true)
            {
                // ✅ Exitoso — actualizar ambas tablas
                await SetStatus(doc, STATUS_PROCESADO);

                await _documentService.UpdateDocumentStatus(
                    payload.DteId,
                    DTE_PROCESADO,
                    receptionStamp: result.ReceptionStamp);

                _logger.LogInformation(
                    "Contingencia id={Id} retransmitida OK. Sello={Sello}",
                    doc.Id, result.ReceptionStamp);
            }
            else if (result?.Status == "RECHAZADO")
            {
                // ❌ MH lo rechazó definitivamente — no reintentar
                await SetStatus(doc, STATUS_FALLIDO);

                await _documentService.UpdateDocumentStatus(
                    payload.DteId,
                    "RECHAZADO",
                    errorMessage: result.Error,
                    errorDetails: result.ErrorDetails,
                    responseCode: result.ResponseCode);

                _logger.LogWarning(
                    "Contingencia id={Id} rechazada definitivamente por Hacienda.", doc.Id);
            }
            else
            {
                // ⏳ Sigue sin poder transmitirse — dejar en PENDIENTE para próximo ciclo
                _logger.LogWarning(
                    "Contingencia id={Id} sigue sin transmitirse. Error={Error}",
                    doc.Id, result?.Error ?? "sin respuesta");
            }
        }

        private async Task SetStatus(ContingencyDocument doc, string status)
        {
            doc.Status = status;
            _context.ContingencyDocuments.Update(doc);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Payload serializado en contingency_document.json_content.
    /// Contiene todo lo necesario para retransmitir sin campos extra en la BD.
    /// </summary>
    public class ContingencyPayload
    {
        public string DteId { get; set; } = "";  // UUID del dte_document
        public string SignedJWT { get; set; } = "";
        public string UserNit { get; set; } = "";
        public string Ambiente { get; set; } = "";
        public int Version { get; set; }
        public string? OriginalError { get; set; }
    }
}
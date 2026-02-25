using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Dte;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ApiContabsv.Services
{
    public interface IContingencyService
    {
        bool ShouldGoToContingency(HaciendaTransmissionResult? result, Exception? ex = null);

        Task StoreInContingency(
            string dteId,
            int userId,
            string documentType,
            string signedJWT,
            string userNit,
            string ambiente,
            int version,
            HaciendaTransmissionResult? failedResult = null);

        Task RetransmitPendingDocuments();
    }

    public class ContingencyService : IContingencyService
    {
        private readonly dteContext _context;
        private readonly IHaciendaService _haciendaService;
        private readonly IDTEDocumentService _documentService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ContingencyService> _logger;

        private const string STATUS_PENDIENTE = "PENDIENTE";
        private const string STATUS_PROCESADO = "PROCESADO";
        private const string STATUS_FALLIDO = "FALLIDO";
        private const string DTE_CONTINGENCIA = "CONTINGENCIA";
        private const string DTE_PROCESADO = "PROCESADO";

        // Tipo 5 = "No disponibilidad del sistema del MH" (el más común)
        private const int CONTINGENCY_TYPE = 5;
        private const string CONTINGENCY_REASON = "No disponibilidad del sistema del Ministerio de Hacienda";

        public ContingencyService(
            dteContext context,
            IHaciendaService haciendaService,
            IDTEDocumentService documentService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ContingencyService> logger)
        {
            _context = context;
            _haciendaService = haciendaService;
            _documentService = documentService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────
        // CLASIFICACIÓN DEL ERROR
        // ─────────────────────────────────────────────────────────────

        public bool ShouldGoToContingency(HaciendaTransmissionResult? result, Exception? ex = null)
        {
            if (result?.Status == "RECHAZADO") return false;
            if (result?.Success == true) return false;

            if (ex is HttpRequestException || ex is TaskCanceledException || ex is TimeoutException)
                return true;

            if (result != null)
            {
                var code = (result.ResponseCode ?? "").ToUpper();
                var error = (result.Error ?? "").ToUpper();

                if (code == "HTTP_ERROR" || code == "INTERNAL_ERROR") return true;

                if (error.Contains("ERROR HTTP") ||
                    error.Contains("CONEXI") ||
                    error.Contains("CONNECTION") ||
                    error.Contains("TIMEOUT") ||
                    error.Contains("DISPONIBLE") ||
                    error.Contains("UNAUTHORIZED") ||
                    error.Contains("SERVICE UNAVAILABLE") ||
                    error.Contains("503") ||
                    error.Contains("504"))
                    return true;
            }

            if (result == null && ex == null) return true;
            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // GUARDAR EN CONTINGENCIA
        // Guardamos el JSON ORIGINAL (sin firmar) para poder modificarlo
        // y refirmarlo al retransmitir (igual que el Go)
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
                // Obtener el JSON original sin firmar desde dte_document
                var dteDoc = await _context.DteDocuments
                    .FirstOrDefaultAsync(d => d.DteId == dteId);

                var payload = new ContingencyPayload
                {
                    DteId = dteId,
                    SignedJWT = signedJWT,
                    OriginalJson = dteDoc?.JsonContent ?? "",  // ← JSON sin firmar para refirmar
                    UserNit = userNit,
                    Ambiente = ambiente,
                    Version = version,
                    ContingencyType = CONTINGENCY_TYPE,
                    ContingencyReason = CONTINGENCY_REASON,
                    OriginalError = failedResult?.Error,
                    StoredAt = DateTime.Now
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

                await _documentService.UpdateDocumentStatus(
                    dteId,
                    DTE_CONTINGENCIA,
                    errorMessage: failedResult?.Error ?? "Pendiente de retransmisión por contingencia",
                    errorDetails: failedResult?.ErrorDetails);

                _logger.LogInformation(
                    "Documento {DteId} guardado en contingencia (id={Id})", dteId, contingencyDoc.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando documento {DteId} en contingencia", dteId);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // RETRANSMISIÓN PERIÓDICA
        // Flujo igual al Go:
        // 1. Agrupar por NIT
        // 2. Enviar Evento de Contingencia a MH
        // 3. Modificar JSON: tipoModelo=2, tipoOperacion=2, tipoContingencia, motivoContin
        // 4. Refirmar el JSON modificado
        // 5. Enviar lote a LoteReceptionUrl
        // 6. Consultar estado del lote y actualizar BD
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

            _logger.LogInformation("Retransmitiendo {Count} documentos...", pending.Count);

            // Agrupar por NIT de usuario
            var groups = new Dictionary<string, List<ContingencyDocument>>();
            foreach (var doc in pending)
            {
                var p = GetPayload(doc);
                if (p == null) continue;
                if (!groups.ContainsKey(p.UserNit))
                    groups[p.UserNit] = new List<ContingencyDocument>();
                groups[p.UserNit].Add(doc);
            }

            foreach (var (userNit, docs) in groups)
            {
                try { await RetransmitGroupByNit(userNit, docs); }
                catch (Exception ex)
                { _logger.LogError(ex, "Error retransmitiendo grupo NIT={NIT}", userNit); }
            }
        }

        private async Task RetransmitGroupByNit(string userNit, List<ContingencyDocument> docs)
        {
            var firstPayload = GetPayload(docs[0]);
            if (firstPayload == null) return;
            var ambiente = firstPayload.Ambiente;

            // 1. TOKEN DE HACIENDA
            var token = await GetHaciendaToken(userNit, ambiente);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No se pudo obtener token MH para NIT={NIT}", userNit);
                return;
            }

            // 2. DATOS DEL USUARIO Y SUCURSAL
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Nit == userNit);
            if (user == null) return;

            var branch = await _context.BranchOffices
                .Include(b => b.Addresses)
                .FirstOrDefaultAsync(b => b.UserId == user.Id && b.IsActive);

            // 3. ENVIAR EVENTO DE CONTINGENCIA (obligatorio antes de los documentos)
            var eventSent = await SendContingencyEvent(user, branch, docs, token, ambiente);
            if (!eventSent)
            {
                _logger.LogWarning("Evento de contingencia falló para NIT={NIT}, se reintentará en próximo ciclo", userNit);
                return;
            }

            // 4. FIRMADOR
            var signerEndpoint = await GetSignerEndpoint(user.Id);
            if (string.IsNullOrEmpty(signerEndpoint))
            {
                _logger.LogWarning("No hay firmador disponible para usuario {UserId}", user.Id);
                return;
            }

            // 5. MODIFICAR + REFIRMAR cada documento
            var signedDocs = new List<(ContingencyDocument doc, ContingencyPayload payload, string jwt)>();

            foreach (var doc in docs)
            {
                var payload = GetPayload(doc);
                if (payload == null || string.IsNullOrEmpty(payload.OriginalJson)) continue;

                try
                {
                    // Modificar JSON: tipoModelo=2, tipoOperacion=2, tipoContingencia, motivoContin
                    var modifiedJson = UpdateContingencyFields(
                        payload.OriginalJson,
                        payload.ContingencyType,
                        payload.ContingencyReason);

                    if (string.IsNullOrEmpty(modifiedJson)) continue;

                    // Refirmar con el JSON modificado
                    var newJwt = await SignDocument(user, modifiedJson, signerEndpoint);
                    if (string.IsNullOrEmpty(newJwt)) continue;

                    signedDocs.Add((doc, payload, newJwt));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error preparando doc contingencia id={Id}", doc.Id);
                }
            }

            if (!signedDocs.Any()) return;

            // 6. ENVIAR LOTE por LoteReceptionUrl
            var batchId = Guid.NewGuid().ToString().ToUpper();
            var mhBatchId = await SendBatch(
                batchId, userNit, docs[0].DocumentType,
                signedDocs.Select(x => x.jwt).ToList(),
                token, ambiente);

            if (string.IsNullOrEmpty(mhBatchId))
            {
                _logger.LogWarning("Fallo enviando lote para NIT={NIT}", userNit);
                return;
            }

            // 7. CONSULTAR ESTADO DEL LOTE y actualizar BD
            await VerifyBatchAndUpdate(mhBatchId, token, batchId, signedDocs, ambiente);
        }

        // ─────────────────────────────────────────────────────────────
        // EVENTO DE CONTINGENCIA (igual a PrepareAndSendContingencyEvent del Go)
        // ─────────────────────────────────────────────────────────────

        private async Task<bool> SendContingencyEvent(
            User user,
            BranchOffice? branch,
            List<ContingencyDocument> docs,
            string haciendaToken,
            string ambiente)
        {
            try
            {
                var firstDoc = docs.OrderBy(d => d.CreatedAt).First();
                DateTime startTime = firstDoc.CreatedAt ?? DateTime.Now.AddMinutes(-30);
                DateTime endTime = DateTime.Now;
                var address = branch?.Addresses?.FirstOrDefault();

                // Lista de DTEs afectados
                var dteDetails = new List<object>();
                int item = 1;
                foreach (var doc in docs)
                {
                    var p = GetPayload(doc);
                    if (p == null) continue;
                    dteDetails.Add(new
                    {
                        noItem = item++,
                        codigoGeneracion = p.DteId.ToUpper(),
                        tipoDoc = doc.DocumentType
                    });
                }

                var contingencyEvent = new
                {
                    identificacion = new
                    {
                        version = 3,
                        ambiente = ambiente,
                        codigoGeneracion = Guid.NewGuid().ToString().ToUpper(),
                        fTransmision = endTime.ToString("yyyy-MM-dd"),
                        hTransmision = endTime.ToString("HH:mm:ss")
                    },
                    emisor = new
                    {
                        nit = user.Nit,
                        nombre = user.BusinessName ?? user.CommercialName,
                        nombreResponsable = user.BusinessName ?? user.CommercialName,
                        tipoDocResponsable = "36",
                        numeroDocResponsable = user.Nit,
                        tipoEstablecimiento = branch?.EstablishmentType ?? "02",
                        telefono = branch?.Phone ?? user.Phone,
                        correo = branch?.Email ?? user.Email,
                        codEstableMH = branch?.EstablishmentCodeMh ?? branch?.EstablishmentCode,
                        codPuntoVenta = branch?.PosCodeMh ?? branch?.PosCode
                    },
                    detalleDTE = dteDetails.ToArray(),
                    motivo = new
                    {
                        fInicio = startTime.ToString("yyyy-MM-dd"),
                        fFin = endTime.ToString("yyyy-MM-dd"),
                        hInicio = startTime.AddSeconds(-60).ToString("HH:mm:ss"),
                        hFin = endTime.AddSeconds(10).ToString("HH:mm:ss"),
                        tipoContingencia = CONTINGENCY_TYPE,
                        motivoContingencia = CONTINGENCY_REASON
                    }
                };

                // Firmar el evento
                var signerEndpoint = await GetSignerEndpoint(user.Id);
                if (string.IsNullOrEmpty(signerEndpoint)) return false;

                var eventJson = JsonSerializer.Serialize(contingencyEvent);
                var signedEvent = await SignDocument(user, eventJson, signerEndpoint);
                if (string.IsNullOrEmpty(signedEvent)) return false;

                // POST a ContingencyUrl
                var contingencyUrl = GetUrl("ContingencyUrl", ambiente);
                if (string.IsNullOrEmpty(contingencyUrl))
                {
                    _logger.LogWarning("ContingencyUrl no configurada en appsettings");
                    return false;
                }

                var requestBody = JsonSerializer.Serialize(new
                {
                    nit = user.Nit,
                    documento = signedEvent
                });

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", haciendaToken);

                var response = await httpClient.PostAsync(
                    contingencyUrl,
                    new StringContent(requestBody, Encoding.UTF8, "application/json"));

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Evento contingencia → {Status}: {Body}",
                    response.StatusCode, responseBody);

                if (!response.IsSuccessStatusCode) return false;
                if (responseBody.Contains("no superadas", StringComparison.OrdinalIgnoreCase)) return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando evento de contingencia");
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // MODIFICAR JSON DEL DTE (igual a UpdateContingencyIdentification del Go)
        // ─────────────────────────────────────────────────────────────

        private string? UpdateContingencyFields(string? originalJson, int contingencyType, string reason)
        {
            if (string.IsNullOrEmpty(originalJson)) return null;
            try
            {
                var doc = JsonNode.Parse(originalJson);
                if (doc == null) return null;

                var identificacion = doc["identificacion"] as JsonObject;
                if (identificacion == null) return null;

                identificacion["tipoModelo"] = 2;
                identificacion["tipoOperacion"] = 2;
                identificacion["tipoContingencia"] = contingencyType;
                identificacion["motivoContin"] = reason;

                return doc.ToJsonString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error modificando JSON para contingencia");
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // FIRMAR DOCUMENTO
        // ─────────────────────────────────────────────────────────────

        private async Task<string?> SignDocument(User user, string jsonContent, string signerEndpoint)
        {
            try
            {
                var dteJson = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                var request = new
                {
                    nit = user.Nit,
                    activo = true,
                    passwordPri = user.PasswordPri,
                    dteJson = dteJson
                };

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                if (!string.IsNullOrEmpty(user.JwtSecret))
                {
                    var jwtToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.JwtSecret));
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");
                }

                var response = await httpClient.PostAsync(
                    signerEndpoint,
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode) return null;

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (responseObj.TryGetProperty("status", out var status) &&
                    status.GetString() == "OK" &&
                    responseObj.TryGetProperty("body", out var body))
                    return body.GetString();

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error firmando documento en contingencia");
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // ENVIAR LOTE (LoteReceptionUrl)
        // ─────────────────────────────────────────────────────────────

        private async Task<string?> SendBatch(
            string batchId,
            string userNit,
            string documentType,
            List<string> signedDocs,
            string haciendaToken,
            string ambiente)
        {
            try
            {
                var loteUrl = GetUrl("BatchReceptionUrl", ambiente);
                if (string.IsNullOrEmpty(loteUrl))
                {
                    _logger.LogWarning("LoteReceptionUrl no configurada");
                    return null;
                }

                // Versión del lote: FCF=1, resto=2 (igual al Go)
                var version = documentType == "01" ? 1 : 2;

                var batchRequest = new
                {
                    ambiente = ambiente,
                    idEnvio = batchId,
                    version = version,
                    nitEmisor = userNit,
                    documentos = signedDocs.ToArray()
                };

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(45);
                httpClient.DefaultRequestHeaders.Add("Authorization", haciendaToken);

                var jsonBody = JsonSerializer.Serialize(batchRequest);
                _logger.LogInformation("Lote request → URL={Url} Body={Body}", loteUrl, jsonBody);

                var response = await httpClient.PostAsync(
                    loteUrl,
                    new StringContent(jsonBody, Encoding.UTF8, "application/json"));

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Lote enviado → {Status}: {Body}", response.StatusCode, responseBody);

                if (!response.IsSuccessStatusCode) return null;

                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);

                // MH responde con codigoLote (campo confirmado en modelo Go)
                foreach (var field in new[] { "codigoLote", "codigoMensaje", "batchCode", "codigo" })
                {
                    if (responseJson.TryGetProperty(field, out var val))
                        return val.GetString();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando lote de contingencia");
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // CONSULTAR ESTADO DEL LOTE Y ACTUALIZAR (polling cada 10s, max 2min)
        // ─────────────────────────────────────────────────────────────

        private async Task VerifyBatchAndUpdate(
            string mhBatchId,
            string haciendaToken,
            string batchId,
            List<(ContingencyDocument doc, ContingencyPayload payload, string jwt)> signedDocs,
            string ambiente)
        {
            var loteConsultUrl = GetUrl("BatchConsultUrl", ambiente);
            if (string.IsNullOrEmpty(loteConsultUrl))
            {
                _logger.LogWarning("LoteReceptionConsultUrl no configurada");
                return;
            }

            var deadline = DateTime.Now.AddMinutes(2);
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", haciendaToken);

            while (DateTime.Now < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                try
                {
                    var resp = await httpClient.GetAsync($"{loteConsultUrl}/{mhBatchId}");
                    var body = await resp.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(body)) continue;

                    var json = JsonSerializer.Deserialize<JsonElement>(body);

                    // PROCESADOS
                    if (json.TryGetProperty("procesados", out var procesados) &&
                        procesados.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in procesados.EnumerateArray())
                        {
                            var codigoGen = item.TryGetProperty("codigoGeneracion", out var cg) ? cg.GetString() : null;
                            var sello = item.TryGetProperty("selloRecibido", out var sr) ? sr.GetString() : null;

                            var match = signedDocs.FirstOrDefault(x =>
                                string.Equals(x.payload.DteId, codigoGen, StringComparison.OrdinalIgnoreCase));

                            if (match.doc != null)
                            {
                                await SetStatus(match.doc, STATUS_PROCESADO);
                                await _documentService.UpdateDocumentStatus(
                                    match.payload.DteId, DTE_PROCESADO, receptionStamp: sello);
                                _logger.LogInformation("Lote: {DteId} PROCESADO sello={Sello}",
                                    match.payload.DteId, sello);
                            }
                        }
                    }

                    // RECHAZADOS
                    if (json.TryGetProperty("rechazados", out var rechazados) &&
                        rechazados.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in rechazados.EnumerateArray())
                        {
                            var codigoGen = item.TryGetProperty("codigoGeneracion", out var cg) ? cg.GetString() : null;
                            var mensaje = item.TryGetProperty("descripcionMsg", out var dm) ? dm.GetString() : null;

                            var match = signedDocs.FirstOrDefault(x =>
                                string.Equals(x.payload.DteId, codigoGen, StringComparison.OrdinalIgnoreCase));

                            if (match.doc != null)
                            {
                                await SetStatus(match.doc, STATUS_FALLIDO);
                                await _documentService.UpdateDocumentStatus(
                                    match.payload.DteId, "RECHAZADO", errorMessage: mensaje);
                                _logger.LogWarning("Lote: {DteId} RECHAZADO: {Msg}",
                                    match.payload.DteId, mensaje);
                            }
                        }
                    }

                    bool hayResultados =
                        (json.TryGetProperty("procesados", out var lp) && lp.GetArrayLength() > 0) ||
                        (json.TryGetProperty("rechazados", out var lr) && lr.GetArrayLength() > 0);

                    if (hayResultados) return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consultando estado lote {BatchId}", mhBatchId);
                }
            }

            _logger.LogWarning("Timeout consultando estado del lote {BatchId}", mhBatchId);
        }

        // ─────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────

        private ContingencyPayload? GetPayload(ContingencyDocument doc)
        {
            try { return JsonSerializer.Deserialize<ContingencyPayload>(doc.JsonContent ?? ""); }
            catch { return null; }
        }

        private async Task<string?> GetHaciendaToken(string userNit, string ambiente)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Nit == userNit);
            if (user == null) return null;

            if (!string.IsNullOrEmpty(user.HaciendaToken) &&
                user.TokenExpiresAt.HasValue &&
                user.TokenExpiresAt > DateTime.Now)
                return user.HaciendaToken;

            var auth = await _haciendaService.AuthenticateUser(user.Nit, user.JwtSecret, ambiente);
            if (auth.Success && !string.IsNullOrEmpty(auth.Token))
            {
                var tok = auth.Token.StartsWith("Bearer ") ? auth.Token : $"Bearer {auth.Token}";
                user.HaciendaToken = tok;
                user.TokenExpiresAt = DateTime.Now.AddHours(4);
                await _context.SaveChangesAsync();
                return tok;
            }
            return null;
        }

        private async Task<string?> GetSignerEndpoint(int userId)
        {
            var assignment = await _context.SignerAssignments
                .Include(sa => sa.Signer)
                .Where(sa => sa.UserId == userId &&
                             sa.Signer.IsActive &&
                             sa.Signer.HealthStatus == "Healthy")
                .OrderByDescending(sa => sa.IsPrimary)
                .ThenBy(sa => sa.Signer.CurrentLoad)
                .FirstOrDefaultAsync();

            return assignment?.Signer.EndpointUrl;
        }

        private string GetUrl(string key, string ambiente)
        {
            var section = ambiente == "00" ? "TestingUrls" : "ProductionUrls";
            return _configuration.GetValue<string>($"HaciendaSettings:{section}:{key}") ?? "";
        }

        private async Task SetStatus(ContingencyDocument doc, string status)
        {
            doc.Status = status;
            _context.ContingencyDocuments.Update(doc);
            await _context.SaveChangesAsync();
        }
    }

    public class ContingencyPayload
    {
        public string DteId { get; set; } = "";
        public string SignedJWT { get; set; } = "";
        public string OriginalJson { get; set; } = "";  // ← JSON sin firmar para refirmar
        public string UserNit { get; set; } = "";
        public string Ambiente { get; set; } = "";
        public int Version { get; set; }
        public int ContingencyType { get; set; } = 5;
        public string ContingencyReason { get; set; } = "No disponibilidad del sistema del Ministerio de Hacienda";
        public string? OriginalError { get; set; }
        public DateTime StoredAt { get; set; } = DateTime.Now;
    }
}
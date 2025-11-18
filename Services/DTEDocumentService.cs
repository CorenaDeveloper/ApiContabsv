using ApiContabsv.Models.Dte;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApiContabsv.Services
{
    public interface IDTEDocumentService
    {
        Task<int> SaveDocument(SaveDocumentRequest request);
        Task<bool> UpdateDocumentStatus(string dteId, string status, string? receptionStamp = null);
        Task<DTEDocumentResponse?> GetDocument(string dteId);
        Task<List<DTEDocumentResponse>> GetDocumentsByUser(int userId, int page = 1, int pageSize = 20);
        Task<int> GetNextSequenceNumber(int userId, string documentType, string establishmentCode, string posCode);
    }

    public class DTEDocumentService : IDTEDocumentService
    {
        private readonly dteContext _context;
        private readonly ILogger<DTEDocumentService> _logger;

        public DTEDocumentService(dteContext context, ILogger<DTEDocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> SaveDocument(SaveDocumentRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. GUARDAR DTE_DETAILS
                var dteDetails = new DteDetail
                {
                    DteId = request.DteId,
                    UserId = request.UserId,
                    DocumentType = request.DocumentType,
                    GenerationType = request.GenerationType,
                    ControlNumber = request.ControlNumber,
                    TotalAmount = request.TotalAmount,
                    CreatedAt = DateTime.Now
                };

                _context.DteDetails.Add(dteDetails);

                // 2. GUARDAR DTE_DOCUMENT
                var dteDocument = new DteDocument
                {
                    UserId = request.UserId,
                    DteId = request.DteId,
                    DocumentType = request.DocumentType,
                    Status = request.Status ?? "FIRMADO",
                    JsonContent = request.JsonContent,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.DteDocuments.Add(dteDocument);

                // 3. ACTUALIZAR SECUENCIA DE NÚMEROS DE CONTROL
                await UpdateControlNumberSequence(request.UserId, request.DocumentType,
                    request.EstablishmentCode ?? "0001", request.PosCode ?? "001");

                // 4. GUARDAR TODO
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Documento DTE guardado exitosamente: {DteId} para usuario {UserId}",
                    request.DteId, request.UserId);

                return dteDetails.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error guardando documento DTE: {DteId} para usuario {UserId}",
                    request.DteId, request.UserId);
                throw;
            }
        }

        public async Task<bool> UpdateDocumentStatus(string dteId, string status, string? receptionStamp = null)
        {
            try
            {
                var document = await _context.DteDocuments
                    .FirstOrDefaultAsync(d => d.DteId == dteId);

                if (document == null)
                {
                    _logger.LogWarning("No se encontró documento con DTE ID: {DteId}", dteId);
                    return false;
                }

                document.Status = status;
                document.UpdatedAt = DateTime.Now;

                // Si viene sello de recepción, agregarlo al JSON
                if (!string.IsNullOrEmpty(receptionStamp) && !string.IsNullOrEmpty(document.JsonContent))
                {
                    var jsonDoc = JsonSerializer.Deserialize<JsonElement>(document.JsonContent);
                    var updatedJson = AddReceptionStampToJson(jsonDoc, receptionStamp);
                    document.JsonContent = JsonSerializer.Serialize(updatedJson);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando estado del documento: {DteId}", dteId);
                return false;
            }
        }

        public async Task<DTEDocumentResponse?> GetDocument(string dteId)
        {
            try
            {
                var document = await _context.DteDocuments
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DteId == dteId);

                if (document == null) return null;

                var details = await _context.DteDetails
                    .FirstOrDefaultAsync(d => d.DteId == dteId);

                return new DTEDocumentResponse
                {
                    Id = document.Id,
                    DteId = document.DteId,
                    UserId = document.UserId,
                    UserName = document.User?.CommercialName ?? "",
                    DocumentType = document.DocumentType,
                    Status = document.Status,
                    ControlNumber = details?.ControlNumber ?? "",
                    TotalAmount = details?.TotalAmount ?? 0,
                    JsonContent = document.JsonContent,
                    CreatedAt = document.CreatedAt,
                    UpdatedAt = document.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo documento: {DteId}", dteId);
                return null;
            }
        }

        public async Task<List<DTEDocumentResponse>> GetDocumentsByUser(int userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var skip = (page - 1) * pageSize;

                var documents = await _context.DteDocuments
                    .Where(d => d.UserId == userId)
                    .OrderByDescending(d => d.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(d => new DTEDocumentResponse
                    {
                        Id = d.Id,
                        DteId = d.DteId,
                        UserId = d.UserId,
                        DocumentType = d.DocumentType,
                        Status = d.Status,
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt
                    })
                    .ToListAsync();

                // Obtener detalles por separado para mejor performance
                var dteIds = documents.Select(d => d.DteId).ToList();
                var details = await _context.DteDetails
                    .Where(d => dteIds.Contains(d.DteId))
                    .ToDictionaryAsync(d => d.DteId, d => d);

                // Mapear detalles a documentos
                foreach (var doc in documents)
                {
                    if (details.ContainsKey(doc.DteId))
                    {
                        doc.ControlNumber = details[doc.DteId].ControlNumber;
                        doc.TotalAmount = details[doc.DteId].TotalAmount;
                    }
                }

                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo documentos del usuario: {UserId}", userId);
                return new List<DTEDocumentResponse>();
            }
        }

        public async Task<int> GetNextSequenceNumber(int userId, string documentType, string establishmentCode, string posCode)
        {
            try
            {
                var currentYear = DateTime.Now.Year;

                var sequence = await _context.ControlNumberSequences
                    .FirstOrDefaultAsync(s => s.UserId == userId
                                           && s.DocumentType == documentType
                                           && s.EstablishmentCode == establishmentCode
                                           && s.PosCode == posCode
                                           && s.Year == currentYear);

                if (sequence == null)
                {
                    // Crear nueva secuencia para este año
                    sequence = new ControlNumberSequence
                    {
                        UserId = userId,
                        DocumentType = documentType,
                        EstablishmentCode = establishmentCode,
                        PosCode = posCode,
                        SequenceNumber = 1,
                        Year = currentYear,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.ControlNumberSequences.Add(sequence);
                    await _context.SaveChangesAsync();
                    return 1;
                }

                return (int)sequence.SequenceNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo secuencia para usuario {UserId}", userId);
                return 1;
            }
        }

        private async Task UpdateControlNumberSequence(int userId, string documentType, string establishmentCode, string posCode)
        {
            var currentYear = DateTime.Now.Year;

            var sequence = await _context.ControlNumberSequences
                .FirstOrDefaultAsync(s => s.UserId == userId
                                       && s.DocumentType == documentType
                                       && s.EstablishmentCode == establishmentCode
                                       && s.PosCode == posCode
                                       && s.Year == currentYear);

            if (sequence != null)
            {
                sequence.SequenceNumber++;
                sequence.UpdatedAt = DateTime.Now;
            }
        }

        private JsonElement AddReceptionStampToJson(JsonElement originalJson, string receptionStamp)
        {
            var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(originalJson.GetRawText());

            if (jsonDict != null)
            {
                jsonDict["selloRecibido"] = receptionStamp;
            }

            return JsonSerializer.SerializeToElement(jsonDict);
        }
    }

    // DTOs
    public class SaveDocumentRequest
    {
        public string DteId { get; set; } = "";
        public int UserId { get; set; }
        public string DocumentType { get; set; } = "";
        public int GenerationType { get; set; } = 1;
        public string ControlNumber { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public string? JsonContent { get; set; }
        public string? EstablishmentCode { get; set; }
        public string? PosCode { get; set; }
    }

    public class DTEDocumentResponse
    {
        public int Id { get; set; }
        public string DteId { get; set; } = "";
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string DocumentType { get; set; } = "";
        public string Status { get; set; } = "";
        public string ControlNumber { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string? JsonContent { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
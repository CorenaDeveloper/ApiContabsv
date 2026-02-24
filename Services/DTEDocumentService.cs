using ApiContabsv.DTO.DB_DteDTO;
using ApiContabsv.Models.Contabilidad;
using ApiContabsv.Models.Dte;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Text.Json;

namespace ApiContabsv.Services
{
    public interface IDTEDocumentService
    {
        Task<int> SaveDocument(SaveDocumentRequest request);
        Task<bool> UpdateDocumentStatus(string dteId, string status, string? receptionStamp = null, string? errorMessage = null, string? errorDetails = null, string? haciendaResponse = null, string? responseCode = null);
        Task<DTEDocumentResponse?> GetDocument(string dteId, int userdte, string ambiente);
        Task<List<DTEDocumentResponse>> GetDocumentsByUser(int userId, DateTime? startDate = null, DateTime? endDate = null, string DocumentType = "", string ambiente = "");
        Task<int> GetNextSequenceNumber(int userId, string documentType, string establishmentCode, string posCode, string ambiente);
    }

    public class DTEDocumentService : IDTEDocumentService
    {
        private readonly dteContext _context;
        private readonly ContabilidadContext _contabilidadContext;
        private readonly ILogger<DTEDocumentService> _logger;

        public DTEDocumentService(dteContext context, ILogger<DTEDocumentService> logger, ContabilidadContext contabilidadContext)
        {
            _context = context;
            _logger = logger;
            _contabilidadContext = contabilidadContext;
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
                    Transmission = request.Transmission ?? "NORMAL",
                    JsonContent = request.JsonContent,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Ambiente = request.Ambiente ?? ""
                };

                _context.DteDocuments.Add(dteDocument);

                // 3. ACTUALIZAR SECUENCIA DE NÚMEROS DE CONTROL
                await UpdateControlNumberSequence(request.UserId, request.DocumentType,
                    request.EstablishmentCode ?? "0001", request.PosCode ?? "001", request.Ambiente ?? "");

                // 4. GUARDAR TODO
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return dteDetails.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateDocumentStatus(string dteId,string status,string? receptionStamp = null,string? errorMessage = null,string? errorDetails = null,string? haciendaResponse = null,string? responseCode = null)
        {
            try
            {
                var document = await _context.DteDocuments
                    .FirstOrDefaultAsync(d => d.DteId == dteId);

                if (document == null)
                {
                    return false;
                }

                document.Status = status;
                document.UpdatedAt = DateTime.Now;
                document.ReceptionStamp = receptionStamp;
                document.ErrorMessage = errorMessage;
                document.ErrorDetails = errorDetails;
                document.HaciendaResponse = haciendaResponse;
                document.ResponseCode = responseCode;

                // Si el status es CONTINGENCIA, marcar transmission como CONTINGENCIA
                if (status == "CONTINGENCIA")
                    document.Transmission = "CONTINGENCIA";
                // Si se recibe sello → fue procesado exitosamente → marcar NORMAL
                // (si vino de contingencia y fue retransmitido, el transmission ya quedó CONTINGENCIA,
                //  lo dejamos así para mantener el historial)
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
                return false;
            }
        }

        public async Task<DTEDocumentResponse?> GetDocument(string dteId, int userdte, string ambiente)
        {
            try
            {
                var document = await _context.DteDocuments
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DteId == dteId && d.UserId == userdte && d.Ambiente == ambiente);

                if (document == null) return null;

                var details = await _context.DteDetails
                    .FirstOrDefaultAsync(d => d.DteId == dteId);

                string? tipoEstablecimiento = null;
                string? codEstable = null;

                if (!string.IsNullOrEmpty(document.JsonContent))
                {
                    try
                    {
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(document.JsonContent);
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("emisor", out var emisor))
                        {
                            if (emisor.TryGetProperty("tipoEstablecimiento", out var tipoEst))
                                tipoEstablecimiento = tipoEst.GetString();

                            if (emisor.TryGetProperty("codEstable", out var cod))
                                codEstable = cod.GetString();
                        }
                    }
                    catch(Exception ex) {
                        return null;
                    }
                }

                var user = await _context.Users
                    .Include(u => u.BranchOffices)
                    .ThenInclude(b => b.Addresses)
                    .FirstOrDefaultAsync(u => u.Id == userdte);

                BranchOffice? branch = null;

                if (!string.IsNullOrEmpty(codEstable))
                {
                    // Primero intentar por código de establecimiento exacto
                    branch = user?.BranchOffices
                        .FirstOrDefault(b => b.IsActive && b.EstablishmentCode == codEstable);
                }

                if (branch == null && !string.IsNullOrEmpty(tipoEstablecimiento))
                {
                    // Si no encontró por código, buscar por tipo
                    branch = user?.BranchOffices
                        .FirstOrDefault(b => b.IsActive && b.EstablishmentType == tipoEstablecimiento);
                }

                branch ??= user?.BranchOffices.FirstOrDefault(b => b.IsActive);

                var address = branch?.Addresses?.FirstOrDefault();

                string? nombreMunicipio = null;
                string? nombreDepartamento = null;

                if (address != null)
                {
                    nombreMunicipio = await _contabilidadContext.Municipios
                        .Where(m => m.Codigo == address.Municipality)
                        .Select(m => m.Nombre)
                        .FirstOrDefaultAsync();

                    nombreDepartamento = await _contabilidadContext.Departamentos
                        .Where(d => d.Codigodep == address.Department)
                        .Select(d => d.Nombre)
                        .FirstOrDefaultAsync();
                }

                return new DTEDocumentResponse
                {
                    Id = document.Id,
                    DteId = document.DteId,
                    UserId = document.UserId,
                    UserName = document.User?.CommercialName ?? "",
                    DocumentType = document.DocumentType,
                    Status = document.Status,
                    Transmission = document.Transmission,
                    ControlNumber = details?.ControlNumber ?? "",
                    TotalAmount = details?.TotalAmount ?? 0,
                    JsonContent = document.JsonContent,
                    CreatedAt = document.CreatedAt,
                    UpdatedAt = document.UpdatedAt,
                    ErrorMessage = document.ErrorMessage,
                    ErrorDetails = document.ErrorDetails,
                    ResponseCode = document.ResponseCode,
                    ReceptionStamp = document.ReceptionStamp,
                    Ambiente = document.Ambiente,

                    // Datos del usuario
                    UserNit = user?.Nit,
                    UserNrc = user?.Nrc,
                    UserBusinessName = user?.BusinessName,
                    UserCommercialName = user?.CommercialName,
                    UserEmail = user?.Email,
                    UserPhone = user?.Phone,
                    UserEconomicActivity = user?.EconomicActivity,
                    UserEconomicActivityDesc = user?.EconomicActivityDesc,

                    // Datos de sucursal y dirección (basados en el establecimiento del documento)
                    BranchEstablishmentCode = branch?.EstablishmentCode,
                    BranchPosCode = branch?.PosCode,
                    BranchEstablishmentType = branch?.EstablishmentType,
                    BranchPhone = branch?.Phone,
                    BranchEmail = branch?.Email,
                    AddressDepartment = address?.Department,
                    NameDepartment = nombreDepartamento ,
                    NameMunicipality = nombreMunicipio,
                    AddressMunicipality = address?.Municipality,
                    AddressComplement = address?.Address1 +
                        (!string.IsNullOrEmpty(address?.Complement) ? ", " + address.Complement : "")
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<DTEDocumentResponse>> GetDocumentsByUser(int userId, DateTime? startDate = null, DateTime? endDate = null, string DocumentType = "", string ambiente = "")
        {
            try
            {

                var query = _context.DteDocuments
                    .Where(d => d.UserId == userId && d.DocumentType == DocumentType && d.Ambiente == ambiente);

                // AGREGAR FILTROS POR FECHA
                if (startDate.HasValue)
                {
                    query = query.Where(d => d.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(d => d.CreatedAt <= endOfDay);
                }

                var documents = await query
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new DTEDocumentResponse
                    {
                        Id = d.Id,
                        DteId = d.DteId,
                        UserId = d.UserId,
                        DocumentType = d.DocumentType,
                        Status = d.Status,
                        Transmission = d.Transmission,
                        JsonContent = d.JsonContent,
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt,
                        UserName = d.User.CommercialName,
                        ErrorDetails = d.ErrorDetails,
                        ErrorMessage = d.ErrorMessage,
                        HaciendaResponse = d.HaciendaResponse,
                        ResponseCode = d.ResponseCode,
                        ReceptionStamp = d.ReceptionStamp,
                        Ambiente = d.Ambiente
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
                return new List<DTEDocumentResponse>();
            }
        }

        // Obtener el siguiente número de secuencia para un usuario y tipo de documento específico en cada ambiente 
        public async Task<int> GetNextSequenceNumber(int userId, string documentType, string establishmentCode, string posCode, string ambiente)
        {
            try
            {
                var currentYear = DateTime.Now.Year;

                var sequence = await _context.ControlNumberSequences
                    .FirstOrDefaultAsync(s => s.UserId == userId
                                           && s.DocumentType == documentType
                                           && s.EstablishmentCode == establishmentCode
                                           && s.PosCode == posCode
                                           && s.Year == currentYear
                                           && s.Ambiente == ambiente);

                if (sequence == null)
                {
                    sequence = new ControlNumberSequence
                    {
                        UserId = userId,
                        DocumentType = documentType,
                        EstablishmentCode = establishmentCode,
                        PosCode = posCode,
                        SequenceNumber = 1,
                        Year = currentYear,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        Ambiente = ambiente
                    };

                    _context.ControlNumberSequences.Add(sequence);
                    await _context.SaveChangesAsync();
                    return 1;
                }

                return (int)sequence.SequenceNumber;
            }
            catch (Exception ex)
            {
                return 1;
            }
        }

        private async Task UpdateControlNumberSequence(int userId, string documentType, string establishmentCode, string posCode, string ambiente)
        {
            var currentYear = DateTime.Now.Year;

            var sequence = await _context.ControlNumberSequences
                .FirstOrDefaultAsync(s => s.UserId == userId
                                       && s.DocumentType == documentType
                                       && s.EstablishmentCode == establishmentCode
                                       && s.PosCode == posCode
                                       && s.Year == currentYear
                                       && s.Ambiente == ambiente); 

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

}
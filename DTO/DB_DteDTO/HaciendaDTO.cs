using System.Text.Json.Serialization;

namespace ApiContabsv.DTO.DB_DteDTO
{
    public class SaveDocumentRequest
    {
        public string DteId { get; set; } = "";
        public int UserId { get; set; }
        public string DocumentType { get; set; } = "";
        public int GenerationType { get; set; } = 1;
        public string ControlNumber { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public string? Transmission { get; set; }
        public string? JsonContent { get; set; }
        public string? EstablishmentCode { get; set; }
        public string? PosCode { get; set; }
        public string? Ambiente { get; set; }
    }

    public class DTEDocumentResponse
    {
        public int Id { get; set; }
        public string DteId { get; set; } = "";
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string DocumentType { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Transmission { get; set; }
        public string ControlNumber { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string? JsonContent { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorDetails { get; set; }
        public string? HaciendaResponse { get; set; }
        public string? ResponseCode { get; set; }
        public string? ReceptionStamp { get; set; }
        public string? Ambiente { get; set; }
        // --- DATOS DEL USUARIO EMISOR ---
        public string? UserNit { get; set; }
        public string? UserNrc { get; set; }
        public string? UserBusinessName { get; set; }
        public string? UserCommercialName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public string? UserEconomicActivity { get; set; }
        public string? UserEconomicActivityDesc { get; set; }

        // --- DATOS DE SUCURSAL Y DIRECCIÓN ---
        public string? BranchEstablishmentCode { get; set; }
        public string? BranchPosCode { get; set; }
        public string? BranchEstablishmentType { get; set; }
        public string? BranchPhone { get; set; }
        public string? BranchEmail { get; set; }
        public string? AddressDepartment { get; set; }
        public string? NameDepartment { get; set; }
        public string? NameMunicipality { get; set; }
        public string? AddressMunicipality { get; set; }
        public string? AddressComplement { get; set; }
    }

    public class HaciendaTransmissionResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? ReceptionStamp { get; set; }
        public string? ResponseCode { get; set; }
        public string? Message { get; set; }
        public string? RawResponse { get; set; }
        public string? Error { get; set; }
        public string? ErrorDetails { get; set; }
    }

    public class HaciendaConsultaResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? ErrorDetails { get; set; }

        // Estado en Hacienda
        public string? Estado { get; set; }
        public string? SelloRecibido { get; set; }
        public string? FhProcesamiento { get; set; }
        public string? CodigoMsg { get; set; }
        public string? DescripcionMsg { get; set; }

        // Identificación
        public string? CodigoGeneracion { get; set; }
        public string? NumeroControl { get; set; }
        public string? TipoDte { get; set; }
        public string? FechaEmision { get; set; }

        // Emisor = Proveedor (para precargar formulario de compras)
        public string? NombreEmisor { get; set; }
        public string? NitEmisor { get; set; }
        public string? NrcEmisor { get; set; }
        public string? CodActividad { get; set; }
        public string? DescActividad { get; set; }
        public string? TelefonoEmisor { get; set; }
        public string? EmailEmisor { get; set; }
        public string? DireccionEmisor { get; set; }

        // Receptor
        public string? NombreReceptor { get; set; }
        public string? NitReceptor { get; set; }
        public string? NrcReceptor { get; set; }

        // Montos
        public decimal? TotalNoSujetas { get; set; }
        public decimal? TotalExentas { get; set; }
        public decimal? TotalGravadas { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? TotalDescuento { get; set; }
        public decimal? TotalIva { get; set; }
        public decimal? TotalPagar { get; set; }
        public int? CondicionOperacion { get; set; }
    }
    public class HaciendaAuthResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? TokenType { get; set; }
        public string? Error { get; set; }
        public string? ErrorDetails { get; set; }
    }

    // Modelos para respuestas de Hacienda
    public class HaciendaResponse
    {
        [JsonPropertyName("version")]
        public int? Version { get; set; }

        [JsonPropertyName("ambiente")]
        public string? Ambiente { get; set; }

        [JsonPropertyName("versionApp")]
        public int? VersionApp { get; set; }

        [JsonPropertyName("estado")]
        public string? Estado { get; set; }

        [JsonPropertyName("codigoGeneracion")]
        public string? CodigoGeneracion { get; set; }

        [JsonPropertyName("selloRecibido")]
        public string? SelloRecibido { get; set; }

        [JsonPropertyName("fhProcesamiento")]
        public string? FhProcesamiento { get; set; }

        [JsonPropertyName("clasificaMsg")]
        public string? ClasificaMsg { get; set; }

        [JsonPropertyName("codigoMsg")]
        public string? CodigoMsg { get; set; }

        [JsonPropertyName("descripcionMsg")]
        public string? Descripcion { get; set; }

        // IMPORTANTE: Para manejar observaciones de documentos rechazados
        [JsonPropertyName("observaciones")]
        public string[]? Observaciones { get; set; }
    }

    public class HaciendaAuthResponse
    {
        public string? Status { get; set; }
        public HaciendaAuthBody? Body { get; set; }
    }

    public class HaciendaAuthBody
    {
        [JsonPropertyName("user")]
        public string? User { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("tokenType")]
        public string? TokenType { get; set; }
    }

    public class HaciendaUserCredentials
    {
        public string? UserHacienda { get; set; }
        public string? PassHacienda { get; set; }
    }
}

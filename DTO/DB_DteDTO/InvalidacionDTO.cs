using System.ComponentModel.DataAnnotations;

namespace ApiContabsv.DTO.DB_DteDTO
{
    /// <summary>
    /// DTO para la invalidación de documentos DTE
    /// </summary>
    public class InvalidacionDocumentoDto
    {
        /// <summary>
        /// Código de generación del documento a invalidar
        /// </summary>
        [Required(ErrorMessage = "El código de generación es requerido")]
        [StringLength(36, MinimumLength = 36, ErrorMessage = "El código de generación debe tener 36 caracteres")]
        [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
            ErrorMessage = "El código de generación debe ser un UUID válido")]
        public string GenerationCode { get; set; } = string.Empty;

        /// <summary>
        /// Información del motivo de invalidación
        /// </summary>
        [Required(ErrorMessage = "Los datos del motivo son requeridos")]
        public MotivoInvalidacionDto Reason { get; set; } = new MotivoInvalidacionDto();

        /// <summary>
        /// Código de generación del documento de reemplazo (opcional, solo para tipo 1 y 3)
        /// </summary>
        [StringLength(36, MinimumLength = 36, ErrorMessage = "El código de generación de reemplazo debe tener 36 caracteres")]
        [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
            ErrorMessage = "El código de generación de reemplazo debe ser un UUID válido")]
        public string? ReplacementGenerationCode { get; set; }

        /// <summary>
        /// ID del usuario que solicita la invalidación
        /// </summary>
        [Required(ErrorMessage = "El ID del usuario es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario debe ser mayor a 0")]
        public int UserId { get; set; }

        /// <summary>
        /// ID del cliente emisor
        /// </summary>
        [Required(ErrorMessage = "El ID del cliente es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del cliente debe ser mayor a 0")]
        public int ClienteId { get; set; }

        /// <summary>
        /// Ambiente de ejecución (00 = Pruebas, 01 = Producción)
        /// </summary>
        public string? Environment { get; set; }
    }

    /// <summary>
    /// DTO para el motivo de invalidación
    /// </summary>
    public class MotivoInvalidacionDto
    {
        /// <summary>
        /// Tipo de invalidación:
        /// 1 = Reemplazo
        /// 2 = Anulación
        /// 3 = Invalidación definitiva
        /// </summary>
        [Required(ErrorMessage = "El tipo de invalidación es requerido")]
        [Range(1, 3, ErrorMessage = "El tipo de invalidación debe ser 1 (Reemplazo), 2 (Anulación) o 3 (Invalidación definitiva)")]
        public int Type { get; set; }

        /// <summary>
        /// Nombre del responsable de la invalidación
        /// </summary>
        [Required(ErrorMessage = "El nombre del responsable es requerido")]
        [StringLength(60, MinimumLength = 1, ErrorMessage = "El nombre del responsable debe tener entre 1 y 60 caracteres")]
        public string ResponsibleName { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento del responsable (13 = DUI, 36 = NIT, 02 = Carnet de residente, etc.)
        /// </summary>
        [Required(ErrorMessage = "El tipo de documento del responsable es requerido")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "El tipo de documento debe tener 2 caracteres")]
        public string ResponsibleDocType { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento del responsable
        /// </summary>
        [Required(ErrorMessage = "El número de documento del responsable es requerido")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "El número de documento debe tener entre 8 y 20 caracteres")]
        public string ResponsibleNumDoc { get; set; } = string.Empty;

        /// <summary>
        /// Nombre del solicitante de la invalidación
        /// </summary>
        [Required(ErrorMessage = "El nombre del solicitante es requerido")]
        [StringLength(60, MinimumLength = 1, ErrorMessage = "El nombre del solicitante debe tener entre 1 y 60 caracteres")]
        public string RequestorName { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento del solicitante (13 = DUI, 36 = NIT, 02 = Carnet de residente, etc.)
        /// </summary>
        [Required(ErrorMessage = "El tipo de documento del solicitante es requerido")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "El tipo de documento debe tener 2 caracteres")]
        public string RequestorDocType { get; set; } = string.Empty;

        /// <summary>
        /// Número de documento del solicitante
        /// </summary>
        [Required(ErrorMessage = "El número de documento del solicitante es requerido")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "El número de documento debe tener entre 8 y 20 caracteres")]
        public string RequestorNumDoc { get; set; } = string.Empty;

        /// <summary>
        /// Motivo de la invalidación (requerido solo para tipo 3 - Invalidación definitiva)
        /// </summary>
        [StringLength(150, ErrorMessage = "El motivo no puede exceder 150 caracteres")]
        public string? Reason { get; set; }

        /// <summary>
        /// Validación personalizada para verificar que el motivo sea requerido para tipo 3
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = string.Empty;

            // Para tipo 3 (invalidación definitiva), el motivo es requerido
            if (Type == 3 && string.IsNullOrWhiteSpace(Reason))
            {
                errorMessage = "El motivo es requerido para invalidación definitiva (tipo 3)";
                return false;
            }

            // Para tipo 2 (anulación), no debe tener código de reemplazo
            // Esta validación se hace a nivel del DTO principal

            return true;
        }
    }

    /// <summary>
    /// DTO para la respuesta de invalidación
    /// </summary>
    public class InvalidacionResponseDto
    {
        public string Estado { get; set; } = string.Empty;
        public string FechaHora { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? SelloRecibido { get; set; }
        public List<string> Observaciones { get; set; } = new List<string>();
        public string? CodigoGeneracion { get; set; }
        public string? NumeroControl { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiContabsv.DTO.DB_DteDTO
{
    public class InvalidacionDocumentoDto
    {
        [JsonPropertyName("generation_code")]
        [Required]
        [StringLength(36)]
        [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", ErrorMessage = "Debe ser un UUID válido")]
        public string GenerationCode { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        [Required]
        public MotivoInvalidacionDto Reason { get; set; } = new();

        [JsonPropertyName("replacement_generation_code")]
        [StringLength(36)]
        [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", ErrorMessage = "Debe ser un UUID válido")]
        public string? ReplacementGenerationCode { get; set; }

        [JsonPropertyName("user_id")]
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "UserId debe ser mayor que 0")]
        public int UserId { get; set; }

        [JsonPropertyName("cliente_id")]
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ClienteId debe ser mayor que 0")]
        public int ClienteId { get; set; }

        [JsonPropertyName("environment")]
        public string? Environment { get; set; }
    }

    public class MotivoInvalidacionDto
    {
        [JsonPropertyName("type")]
        [Required]
        [Range(1, 3, ErrorMessage = "Tipo debe ser 1, 2 o 3")]
        public int Type { get; set; }

        [JsonPropertyName("responsible_name")]
        [Required]
        [StringLength(60, MinimumLength = 1)]
        public string ResponsibleName { get; set; } = string.Empty;

        [JsonPropertyName("responsible_doc_type")]
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string ResponsibleDocType { get; set; } = string.Empty;

        [JsonPropertyName("responsible_num_doc")]
        [Required]
        [StringLength(20, MinimumLength = 8)]
        public string ResponsibleNumDoc { get; set; } = string.Empty;

        [JsonPropertyName("requestor_name")]
        [Required]
        [StringLength(60, MinimumLength = 1)]
        public string RequestorName { get; set; } = string.Empty;

        [JsonPropertyName("requestor_doc_type")]
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string RequestorDocType { get; set; } = string.Empty;

        [JsonPropertyName("requestor_num_doc")]
        [Required]
        [StringLength(20, MinimumLength = 8)]
        public string RequestorNumDoc { get; set; } = string.Empty;

        [JsonPropertyName("reason_field")]  // ← CORRECTO como GO
        [StringLength(150)]
        public string? Reason { get; set; }

        public bool IsValid(out string error)
        {
            error = string.Empty;

            // Tipo 3 requiere motivo
            if (Type == 3 && string.IsNullOrWhiteSpace(Reason))
            {
                error = "Tipo 3 (invalidación definitiva) requiere motivo";
                return false;
            }

            return true;
        }
    }
}
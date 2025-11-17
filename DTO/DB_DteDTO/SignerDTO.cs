using System.ComponentModel.DataAnnotations;

namespace ApiContabsv.DTO.DB_DteDTO
{

    /// <summary>
    /// DTO para crear/registrar un nuevo servicio de firmador
    /// </summary>
    public class CreateSignerDTO
    {
        /// <summary>
        /// Nombre descriptivo del firmador
        /// </summary>
        /// <example>FirmadorPrincipal</example>
        [Required(ErrorMessage = "El nombre del firmador es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string SignerName { get; set; } = string.Empty;

        /// <summary>
        /// Ruta al certificado digital (.p12/.pfx) o carpeta donde están los certificados
        /// </summary>
        /// <example>C:\CERTIFICADOS</example>
        [Required(ErrorMessage = "La ruta del certificado es obligatoria")]
        [StringLength(500, ErrorMessage = "La ruta no puede exceder 500 caracteres")]
        public string CertificatePath { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña del certificado (opcional si ya está configurada en el servicio)
        /// </summary>
        [StringLength(255, ErrorMessage = "La contraseña no puede exceder 255 caracteres")]
        public string? CertificatePassword { get; set; }

        /// <summary>
        /// URL del endpoint del servicio de firmado
        /// </summary>
        /// <example>http://localhost:8114/firmardocumento/</example>
        [StringLength(500, ErrorMessage = "La URL no puede exceder 500 caracteres")]
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Estado activo del firmador
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Máximo número de documentos que puede firmar concurrentemente
        /// </summary>
        /// <example>5</example>
        [Range(1, 50, ErrorMessage = "El máximo de firmas concurrentes debe estar entre 1 y 50")]
        public int? MaxConcurrentSigns { get; set; } = 5;

        /// <summary>
        /// Prioridad del firmador (1 = alta, 5 = baja)
        /// </summary>
        /// <example>1</example>
        [Range(1, 5, ErrorMessage = "La prioridad debe estar entre 1 (alta) y 5 (baja)")]
        public int? Priority { get; set; } = 1;
    }

    /// <summary>
    /// DTO de respuesta después de crear/obtener un firmador
    /// </summary>
    public class SignerResponseDTO
    {
        public int Id { get; set; }
        public string SignerName { get; set; } = string.Empty;
        public string CertificatePath { get; set; } = string.Empty;
        public string? CertificatePassword { get; set; }
        public string? EndpointUrl { get; set; }
        public bool IsActive { get; set; }
        public int? MaxConcurrentSigns { get; set; }
        public int? CurrentLoad { get; set; }
        public int? Priority { get; set; }
        public string? HealthStatus { get; set; }
        public long? TotalDocumentsSigned { get; set; }
        public int? AvgResponseTimeMs { get; set; }
        public DateTime? LastUsed { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Message { get; set; } = "Operación exitosa";
    }

    /// <summary>
    /// DTO para asignar firmadores a usuarios
    /// </summary>
    public class CreateSignerAssignmentDTO
    {
        /// <summary>
        /// ID del usuario al que se asigna el firmador
        /// </summary>
        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UserId { get; set; }

        /// <summary>
        /// ID del firmador a asignar
        /// </summary>
        [Required(ErrorMessage = "El ID del firmador es obligatorio")]
        public int SignerId { get; set; }

        /// <summary>
        /// Indica si este es el firmador principal para el usuario
        /// </summary>
        public bool IsPrimary { get; set; } = false;
    }

    /// <summary>
    /// DTO de respuesta para asignaciones de firmadores
    /// </summary>
    public class SignerAssignmentResponseDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SignerId { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? LastUsed { get; set; }
        public SignerResponseDTO? Signer { get; set; }
        public string Message { get; set; } = "Asignación exitosa";
    }

    /// <summary>
    /// DTO para probar el estado de un firmador
    /// </summary>
    public class SignerHealthCheckDTO
    {
        public int SignerId { get; set; }
        public string SignerName { get; set; } = string.Empty;
        public string? EndpointUrl { get; set; }
        public bool IsHealthy { get; set; }
        public int ResponseTimeMs { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
    }

    /// <summary>
    /// DTO para actualizar la carga de un firmador
    /// </summary>
    public class UpdateSignerLoadDTO
    {
        /// <summary>
        /// ID del firmador
        /// </summary>
        public int SignerId { get; set; }

        /// <summary>
        /// Nueva carga actual
        /// </summary>
        public int CurrentLoad { get; set; }

        /// <summary>
        /// Operación: increment, decrement, set
        /// </summary>
        public string Operation { get; set; } = "set";
    }

    /// <summary>
    /// DTO para estadísticas de firmadores
    /// </summary>
    public class SignerStatsDTO
    {
        public int TotalSigners { get; set; }
        public int ActiveSigners { get; set; }
        public int HealthySigners { get; set; }
        public int TotalCurrentLoad { get; set; }
        public double AverageResponseTime { get; set; }
        public long TotalDocumentsSigned { get; set; }
    }

    /// <summary>
    /// DTO para probar firmado
    /// </summary>
    public class TestSigningDTO
    {
        /// <summary>
        /// NIT del cliente (para buscar certificado)
        /// </summary>
        /// <example>06141809151020</example>
        [Required]
        public string Nit { get; set; } = string.Empty;

        /// <summary>
        /// ID del usuario (para usar firmador asignado)
        /// </summary>
        /// <example>1</example>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Contraseña del certificado (opcional)
        /// </summary>
        public string? CertificatePassword { get; set; }
    }
}

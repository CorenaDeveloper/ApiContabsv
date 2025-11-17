using System.ComponentModel.DataAnnotations;

namespace ApiContabsv.DTO.DB_DteDTO
{
    /// <summary>
    /// DTO para crear un nuevo usuario en el sistema DTE
    /// Solo contiene los campos necesarios para registro
    /// </summary>
    public class CreateUserDTO
    {
        /// <summary>
        /// ID del cliente en la base contabsv (NULL si es cliente externo)
        /// </summary>
        /// <example>123</example>
        public int? ClienteId { get; set; }

        /// <summary>
        /// Indica si este usuario es el cliente principal (true) o sub-cliente (false)
        /// </summary>
        /// <example>true</example>
        public bool IsMaster { get; set; } = false;

        /// <summary>
        /// ID del usuario padre (solo para sub-clientes)
        /// </summary>
        /// <example>5</example>
        public int? ParentUserId { get; set; }

        /// <summary>
        /// Número de Identificación Tributaria (NIT) - Único y requerido
        /// Formato: 14 dígitos + DV (verificador)
        /// </summary>
        /// <example>12345678901234</example>
        [Required(ErrorMessage = "El NIT es obligatorio")]
        [StringLength(17, MinimumLength = 4, ErrorMessage = "El NIT o DUI debe tener entre 4 y 17 caracteres")]
        [RegularExpression(@"^\d{4,17}$", ErrorMessage = "El NIT solo puede contener números")]
        public string Nit { get; set; } = string.Empty;

        /// <summary>
        /// Número de Registro de Contribuyente (NRC) - Único y requerido
        /// Formato: 6-8 dígitos
        /// </summary>
        /// <example>123456-7</example>
        [Required(ErrorMessage = "El NRC es obligatorio")]
        [StringLength(10, MinimumLength = 6, ErrorMessage = "El NRC debe tener entre 6 y 10 caracteres")]
        public string Nrc { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña del certificado digital (.p12) proporcionado por Hacienda
        /// Requerido para firmar documentos electrónicos
        /// </summary>
        /// <example>MiPassword123!</example>
        [Required(ErrorMessage = "La contraseña del certificado es obligatoria")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 255 caracteres")]
        public string PasswordPri { get; set; } = string.Empty;

        /// <summary>
        /// Nombre comercial de la empresa
        /// </summary>
        /// <example>Empresa ABC S.A. de C.V.</example>
        [Required(ErrorMessage = "El nombre comercial es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre comercial no puede exceder 150 caracteres")]
        public string CommercialName { get; set; } = string.Empty;

        /// <summary>
        /// Código de actividad económica (6 dígitos)
        /// Según catálogo de Hacienda
        /// </summary>
        /// <example>620100</example>
        [Required(ErrorMessage = "La actividad económica es obligatoria")]
        [StringLength(9, MinimumLength = 1, ErrorMessage = "La actividad económica debe tener de 1 a 9 dígitos")]
        [RegularExpression(@"^\d{1,9}$", ErrorMessage = "La actividad económica un codigo rango d e 1a 9")]
        public string EconomicActivity { get; set; } = string.Empty;

        /// <summary>
        /// Descripción de la actividad económica
        /// </summary>
        /// <example>Programación informática</example>
        [Required(ErrorMessage = "La descripción de actividad económica es obligatoria")]
        [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
        public string EconomicActivityDesc { get; set; } = string.Empty;

        /// <summary>
        /// Razón social de la empresa (nombre legal)
        /// </summary>
        /// <example>EMPRESA ABC SOCIEDAD ANÓNIMA DE CAPITAL VARIABLE</example>
        [Required(ErrorMessage = "La razón social es obligatoria")]
        [StringLength(200, ErrorMessage = "La razón social no puede exceder 200 caracteres")]
        public string BusinessName { get; set; } = string.Empty;

        /// <summary>
        /// Correo electrónico de contacto - Único y requerido
        /// </summary>
        /// <example>contacto@empresaabc.com</example>
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Teléfono de contacto - Único y requerido
        /// Formato sugerido: +503 XXXX-XXXX
        /// </summary>
        /// <example>+503 2234-5678</example>
        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(30, ErrorMessage = "El teléfono no puede exceder 30 caracteres")]
        [RegularExpression(@"^[\+]?[0-9\s\-\(\)]{8,30}$", ErrorMessage = "El formato del teléfono no es válido")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Incluir año en el número de control del DTE
        /// true: DTE-01-00000000-202500000000001
        /// false: DTE-01-00000000-000000000000001
        /// </summary>
        /// <example>false</example>
        public bool YearInDte { get; set; } = false;

        /// <summary>
        /// Duración en días del token JWT de autenticación
        /// Valor por defecto: 14 días
        /// </summary>
        /// <example>14</example>
        [Range(1, 365, ErrorMessage = "La duración del token debe estar entre 1 y 365 días")]
        public int TokenLifetime { get; set; } = 14;
    }

    /// <summary>
    /// DTO de respuesta después de crear un usuario
    /// Excluye información sensible como contraseñas
    /// </summary>
    public class UserResponseDTO
    {
        public int Id { get; set; }
        public int? ClienteId { get; set; }
        public bool IsMaster { get; set; }
        public int? ParentUserId { get; set; }
        public string Nit { get; set; } = string.Empty;
        public string Nrc { get; set; } = string.Empty;
        public bool Status { get; set; }
        public string AuthType { get; set; } = string.Empty;
        public string CommercialName { get; set; } = string.Empty;
        public string EconomicActivity { get; set; } = string.Empty;
        public string EconomicActivityDesc { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool YearInDte { get; set; }
        public int TokenLifetime { get; set; }
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Mensaje de éxito
        /// </summary>
        public string Message { get; set; } = "Usuario creado exitosamente";
    }
}

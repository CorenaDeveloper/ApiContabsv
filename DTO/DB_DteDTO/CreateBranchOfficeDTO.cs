using System.ComponentModel.DataAnnotations;

namespace ApiContabsv.DTO.DB_DteDTO
{
    /// <summary>
    /// DTO para crear una nueva sucursal/establecimiento con su dirección
    /// </summary>
    public class CreateBranchOfficeDTO
    {
        /// <summary>
        /// ID del usuario al que pertenece la sucursal
        /// </summary>
        /// <example>1</example>
        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UserId { get; set; }

        /// <summary>
        /// Código interno del establecimiento (opcional)
        /// </summary>
        /// <example>C001</example>
        [StringLength(10, ErrorMessage = "El código del establecimiento no puede exceder 10 caracteres")]
        public string? EstablishmentCode { get; set; }

        /// <summary>
        /// Código oficial del establecimiento asignado por Ministerio de Hacienda (4 dígitos)
        /// </summary>
        /// <example>M001</example>
        [StringLength(4, MinimumLength = 4, ErrorMessage = "El código MH debe tener exactamente 4 caracteres")]
        public string? EstablishmentCodeMh { get; set; }

        /// <summary>
        /// Email específico de esta sucursal (opcional)
        /// </summary>
        /// <example>sucursal1@empresa.com</example>
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        public string? Email { get; set; }

        /// <summary>
        /// Teléfono específico de esta sucursal (opcional)
        /// </summary>
        /// <example>2234-5678</example>
        [StringLength(30, ErrorMessage = "El teléfono no puede exceder 30 caracteres")]
        public string? Phone { get; set; }

        /// <summary>
        /// Tipo de establecimiento según catálogo Hacienda
        /// 02 = Casa Matriz, 01 = Sucursal, 20 = Punto de Venta
        /// </summary>
        /// <example>02</example>
        [Required(ErrorMessage = "El tipo de establecimiento es obligatorio")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "El tipo de establecimiento debe tener exactamente 2 dígitos")]
        [RegularExpression(@"^(01|02|20)$", ErrorMessage = "Tipo de establecimiento debe ser: 01 (Sucursal), 02 (Casa Matriz), 20 (Punto de Venta)")]
        public string EstablishmentType { get; set; } = string.Empty;

        /// <summary>
        /// Código interno del punto de venta (opcional)
        /// </summary>
        /// <example>P001</example>
        [StringLength(15, ErrorMessage = "El código POS no puede exceder 15 caracteres")]
        public string? PosCode { get; set; }

        /// <summary>
        /// Código oficial del punto de venta asignado por Ministerio de Hacienda (4 dígitos)
        /// </summary>
        /// <example>P000</example>
        [StringLength(4, MinimumLength = 4, ErrorMessage = "El código POS MH debe tener exactamente 4 caracteres")]
        public string? PosCodeMh { get; set; }

        /// <summary>
        /// Información de la dirección de la sucursal
        /// </summary>
        [Required(ErrorMessage = "La dirección es obligatoria")]
        public CreateAddressDTO Address { get; set; } = new CreateAddressDTO();
    }

    /// <summary>
    /// DTO para la dirección de la sucursal
    /// </summary>
    public class CreateAddressDTO
    {
        /// <summary>
        /// Código del departamento (2 dígitos)
        /// Ejemplo: 03 = Sonsonate, 06 = San Salvador
        /// </summary>
        /// <example>03</example>
        [Required(ErrorMessage = "El código del departamento es obligatorio")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "El código del departamento debe tener exactamente 2 dígitos")]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "El código del departamento debe ser numérico")]
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// Código del municipio (2 dígitos)
        /// Ejemplo: 18 = Sonsonate, 01 = San Salvador
        /// </summary>
        /// <example>18</example>
        [Required(ErrorMessage = "El código del municipio es obligatorio")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "El código del municipio debe tener exactamente 2 dígitos")]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "El código del municipio debe ser numérico")]
        public string Municipality { get; set; } = string.Empty;

        /// <summary>
        /// Dirección completa y detallada
        /// </summary>
        /// <example>Barrio el Angel, calle el Angel, casa 26 Sonsonate</example>
        [Required(ErrorMessage = "La dirección completa es obligatoria")]
        [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Información adicional de la dirección (opcional)
        /// </summary>
        /// <example>Frente al parque central</example>
        [StringLength(100, ErrorMessage = "El complemento no puede exceder 100 caracteres")]
        public string? Complement { get; set; }
    }

    /// <summary>
    /// DTO de respuesta después de crear una sucursal
    /// </summary>
    public class BranchOfficeResponseDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? EstablishmentCode { get; set; }
        public string? EstablishmentCodeMh { get; set; }
        public string? Email { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string EstablishmentType { get; set; } = string.Empty;
        public string? PosCode { get; set; }
        public string? PosCodeMh { get; set; }
        public bool IsActive { get; set; }
        public AddressResponseDTO Address { get; set; } = new AddressResponseDTO();
        public string Message { get; set; } = "Sucursal creada exitosamente";
    }

    /// <summary>
    /// DTO de respuesta para la dirección
    /// </summary>
    public class AddressResponseDTO
    {
        public int Id { get; set; }
        public string Department { get; set; } = string.Empty;
        public string Municipality { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Complement { get; set; }
    }
}

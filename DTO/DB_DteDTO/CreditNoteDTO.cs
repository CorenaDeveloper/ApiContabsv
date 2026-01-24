using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiContabsv.DTO.DB_DteDTO
{
    /// <summary>
    /// DTO PARA CREAR NOTA DE CRÉDITO (CLASES PROPIAS)
    /// </summary>
    public class CreateCreditNoteRequestDTO
    {
        [JsonPropertyName("clientId")]
        [Required(ErrorMessage = "El ID del cliente es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del cliente debe ser mayor a 0")]
        public int ClientId { get; set; }

        [JsonPropertyName("userId")]
        [Required(ErrorMessage = "El ID del usuario es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario debe ser mayor a 0")]
        public int UserId { get; set; }

        [JsonPropertyName("items")]
        [Required(ErrorMessage = "Los items son requeridos")]
        [MinLength(1, ErrorMessage = "Debe incluir al menos 1 item")]
        public List<CreditNoteItemRequestDTO> Items { get; set; } = new();

        [JsonPropertyName("receiver")]
        [Required(ErrorMessage = "El receptor es requerido")]
        public CreditNoteReceiverRequestDTO? Receiver { get; set; }

        [JsonPropertyName("modelType")]
        public int? ModelType { get; set; } = 1;

        [JsonPropertyName("summary")]
        [Required(ErrorMessage = "El resumen es requerido")]
        public CreditNoteSummaryRequestDTO? Summary { get; set; }

        [JsonPropertyName("environment")]
        [RegularExpression(@"^(00|01)$", ErrorMessage = "El ambiente debe ser '00' (prueba) o '01' (producción)")]
        public string? Environment { get; set; } = "00";

        [JsonPropertyName("sendToHacienda")]
        public bool? SendToHacienda { get; set; } = true;

        [JsonPropertyName("relatedDocs")]
        public List<CreditNoteRelatedDocRequestDTO>? RelatedDocs { get; set; }

        [JsonPropertyName("thirdPartySale")]
        public CreditNoteThirdPartySaleRequestDTO? ThirdPartySale { get; set; }
    }

    /// <summary>
    /// ITEM DE NOTA DE CRÉDITO (CLASE PROPIA)
    /// </summary>
    public class CreditNoteItemRequestDTO
    {
        [JsonPropertyName("type")]
        [Required(ErrorMessage = "El tipo de item es requerido")]
        [Range(1, 4, ErrorMessage = "El tipo debe ser 1-4")]
        public int Type { get; set; } = 2;

        [JsonPropertyName("description")]
        [Required(ErrorMessage = "La descripción es requerida")]
        [StringLength(1000, MinimumLength = 1)]
        public string Description { get; set; } = "";

        [JsonPropertyName("quantity")]
        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unit_measure")]
        [Required(ErrorMessage = "La unidad de medida es requerida")]
        [Range(1, 99)]
        public int UnitMeasure { get; set; }

        [JsonPropertyName("unit_price")]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discount")]
        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; } = 0;

        [JsonPropertyName("code")]
        [StringLength(25)]
        public string? Code { get; set; }

        [JsonPropertyName("non_subject_sale")]
        [Range(0, double.MaxValue)]
        public decimal NonSubjectSale { get; set; } = 0;

        [JsonPropertyName("exempt_sale")]
        [Range(0, double.MaxValue)]
        public decimal ExemptSale { get; set; } = 0;

        [JsonPropertyName("taxed_sale")]
        [Range(0, double.MaxValue)]
        public decimal TaxedSale { get; set; } = 0;

        [JsonPropertyName("suggested_price")]
        [Range(0, double.MaxValue)]
        public decimal SuggestedPrice { get; set; } = 0;

        [JsonPropertyName("non_taxed")]
        [Range(0, double.MaxValue)]
        public decimal NonTaxed { get; set; } = 0;

        [JsonPropertyName("taxes")]
        public List<string>? Taxes { get; set; }
    }

    /// <summary>
    /// RECEPTOR DE NOTA DE CRÉDITO (CLASE PROPIA)
    /// </summary>
    public class CreditNoteReceiverRequestDTO
    {
        [JsonPropertyName("name")]
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(250, MinimumLength = 1)]
        public string Name { get; set; } = "";

        [JsonPropertyName("document_type")]
        [StringLength(2)]
        public string? DocumentType { get; set; }

        [JsonPropertyName("document_number")]
        [StringLength(25)]
        public string? DocumentNumber { get; set; }

        [JsonPropertyName("nrc")]
        [StringLength(10)]
        public string? Nrc { get; set; }

        [JsonPropertyName("activity_code")]
        [StringLength(6)]
        public string? ActivityCode { get; set; }

        [JsonPropertyName("activity_description")]
        [StringLength(150)]
        public string? ActivityDescription { get; set; }

        [JsonPropertyName("address")]
        public CreditNoteAddressRequestDTO? Address { get; set; }

        [JsonPropertyName("phone")]
        [StringLength(25)]
        public string? Phone { get; set; }

        [JsonPropertyName("email")]
        [StringLength(100)]
        public string? Email { get; set; }
    }

    /// <summary>
    /// DIRECCIÓN PARA NOTA DE CRÉDITO (CLASE PROPIA)
    /// </summary>
    public class CreditNoteAddressRequestDTO
    {
        [JsonPropertyName("department")]
        [Required]
        [StringLength(2)]
        public string Department { get; set; } = "";

        [JsonPropertyName("municipality")]
        [Required]
        [StringLength(2)]
        public string Municipality { get; set; } = "";

        [JsonPropertyName("complement")]
        [Required]
        [StringLength(200)]
        public string Complement { get; set; } = "";
    }

    /// <summary>
    /// RESUMEN DE NOTA DE CRÉDITO (CLASE PROPIA)
    /// </summary>
    public class CreditNoteSummaryRequestDTO
    {
        [JsonPropertyName("total_non_subject")]
        [Range(0, double.MaxValue)]
        public decimal TotalNonSubject { get; set; } = 0;

        [JsonPropertyName("total_exempt")]
        [Range(0, double.MaxValue)]
        public decimal TotalExempt { get; set; } = 0;

        [JsonPropertyName("total_taxed")]
        [Range(0, double.MaxValue)]
        public decimal TotalTaxed { get; set; } = 0;

        [JsonPropertyName("sub_total")]
        [Required(ErrorMessage = "El subtotal es requerido")]
        [Range(0, double.MaxValue)]
        public decimal SubTotal { get; set; } = 0;

        [JsonPropertyName("non_subject_discount")]
        [Range(0, double.MaxValue)]
        public decimal NonSubjectDiscount { get; set; } = 0;

        [JsonPropertyName("exempt_discount")]
        [Range(0, double.MaxValue)]
        public decimal ExemptDiscount { get; set; } = 0;

        [JsonPropertyName("taxed_discount")]
        [Range(0, double.MaxValue)]
        public decimal TaxedDiscount { get; set; } = 0;

        [JsonPropertyName("discount_percentage")]
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; } = 0;

        [JsonPropertyName("total_discount")]
        [Range(0, double.MaxValue)]
        public decimal TotalDiscount { get; set; } = 0;

        [JsonPropertyName("sub_total_sales")]
        [Required(ErrorMessage = "El subtotal de ventas es requerido")]
        [Range(0, double.MaxValue)]
        public decimal SubTotalSales { get; set; }

        [JsonPropertyName("total_operation")]
        [Required(ErrorMessage = "El total de operación es requerido")]
        [Range(0, double.MaxValue)]
        public decimal TotalOperation { get; set; }

        [JsonPropertyName("total_to_pay")]
        [Required(ErrorMessage = "El total a pagar es requerido")]
        [Range(0, double.MaxValue)]
        public decimal TotalToPay { get; set; }

        [JsonPropertyName("operation_condition")]
        [Required(ErrorMessage = "La condición de operación es requerida")]
        [Range(1, 3)]
        public int OperationCondition { get; set; } = 1;

        [JsonPropertyName("iva_perception")]
        [Range(0, double.MaxValue)]
        public decimal IvaPerception { get; set; } = 0;

        [JsonPropertyName("iva_retention")]
        [Range(0, double.MaxValue)]
        public decimal IvaRetention { get; set; } = 0;

        [JsonPropertyName("income_retention")]
        [Range(0, double.MaxValue)]
        public decimal IncomeRetention { get; set; } = 0;

        [JsonPropertyName("balance_in_favor")]
        [Range(0, double.MaxValue)]
        public decimal BalanceInFavor { get; set; } = 0;

        [JsonPropertyName("total_iva")]
        public decimal TotalIva { get; set; } = 0;
    }

    /// <summary>
    /// DOCUMENTO RELACIONADO PARA NOTA DE CRÉDITO (CLASE PROPIA)
    /// </summary>
    public class CreditNoteRelatedDocRequestDTO
    {
        [JsonPropertyName("document_type")]
        [Required(ErrorMessage = "El tipo de documento es requerido")]
        [StringLength(2)]
        public string DocumentType { get; set; } = "";

        [JsonPropertyName("generation_type")]
        [Range(1, 2)]
        public int GenerationType { get; set; } = 1;

        [JsonPropertyName("document_number")]
        [Required(ErrorMessage = "El número de documento es requerido")]
        [StringLength(100)]
        public string DocumentNumber { get; set; } = "";

        [JsonPropertyName("emission_date")]
        public DateTime EmissionDate { get; set; }
    }

    /// <summary>
    /// VENTA A TERCEROS PARA NOTA DE CRÉDITO (CLASE PROPIA)
    /// </summary>
    public class CreditNoteThirdPartySaleRequestDTO
    {
        [JsonPropertyName("nit")]
        [Required(ErrorMessage = "El NIT es requerido")]
        [RegularExpression(@"^([0-9]{14}|[0-9]{9})$")]
        public string Nit { get; set; } = "";

        [JsonPropertyName("name")]
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(250, MinimumLength = 1)]
        public string Name { get; set; } = "";
    }
}
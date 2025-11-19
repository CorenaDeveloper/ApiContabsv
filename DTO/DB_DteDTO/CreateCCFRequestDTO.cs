using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiContabsv.DTO.DB_DteDTO
{
    public class CreateCCFRequestDTO
    {
        [Required]
        public int ClientId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public List<CCFItemRequestDTO> Items { get; set; }

        [Required]
        public CCFReceiverRequestDTO Receiver { get; set; }

        [Required]
        public CCFSummaryRequestDTO Summary { get; set; }

        public int? ModelType { get; set; } = 1;

        public string? Environment { get; set; } = "00";

        public bool SendToHacienda { get; set; } = true;

        public List<RelatedDocRequestDTO>? RelatedDocs { get; set; }

        public ThirdPartySaleRequestDTO? ThirdPartySale { get; set; }
    }

    // ✅ ITEM CCF
    public class CCFItemRequestDTO
    {
        [Required]
        public int Type { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [Required]
        [JsonPropertyName("unit_measure")]
        public int UnitMeasure { get; set; }

        [Required]
        [JsonPropertyName("unit_price")]
        public decimal UnitPrice { get; set; }

        public decimal Discount { get; set; } = 0;

        public string? Code { get; set; }

        [JsonPropertyName("non_subject_sale")]
        public decimal NonSubjectSale { get; set; } = 0;

        [JsonPropertyName("exempt_sale")]
        public decimal ExemptSale { get; set; } = 0;

        [Required]
        [JsonPropertyName("taxed_sale")]
        public decimal TaxedSale { get; set; }

        [JsonPropertyName("suggested_price")]
        public decimal SuggestedPrice { get; set; } = 0;

        [JsonPropertyName("non_taxed")]
        public decimal NonTaxed { get; set; } = 0;

        // ✅ Array de códigos de impuestos
        [JsonPropertyName("taxes")]
        public List<string>? Taxes { get; set; }

        [JsonPropertyName("related_doc")]
        public string? RelatedDoc { get; set; }
    }

    // ✅ RECEPTOR CCF (con NRC obligatorio)
    public class CCFReceiverRequestDTO
    {
        [Required]
        [JsonPropertyName("document_type")]
        public string DocumentType { get; set; }

        [Required]
        [JsonPropertyName("document_number")]
        public string DocumentNumber { get; set; }

        [Required]
        [JsonPropertyName("nrc")]
        public string Nrc { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public AddressDTO Address { get; set; }

        public string? Phone { get; set; }

        [Required]
        public string Email { get; set; }

        [JsonPropertyName("activity_code")]
        public string? ActivityCode { get; set; }

        [JsonPropertyName("activity_description")]
        public string? ActivityDescription { get; set; }
    }

    // ✅ RESUMEN CCF
    public class CCFSummaryRequestDTO
    {
        [JsonPropertyName("total_non_subject")]
        public decimal TotalNonSubject { get; set; } = 0;

        [JsonPropertyName("total_exempt")]
        public decimal TotalExempt { get; set; } = 0;

        [Required]
        [JsonPropertyName("total_taxed")]
        public decimal TotalTaxed { get; set; }

        [Required]
        [JsonPropertyName("sub_total")]
        public decimal SubTotal { get; set; }

        [JsonPropertyName("non_subject_discount")]
        public decimal NonSubjectDiscount { get; set; } = 0;

        [JsonPropertyName("exempt_discount")]
        public decimal ExemptDiscount { get; set; } = 0;

        [JsonPropertyName("taxed_discount")]
        public decimal TaxedDiscount { get; set; } = 0;

        [JsonPropertyName("discount_percentage")]
        public decimal DiscountPercentage { get; set; } = 0;

        [JsonPropertyName("total_discount")]
        public decimal TotalDiscount { get; set; } = 0;

        [Required]
        [JsonPropertyName("sub_total_sales")]
        public decimal SubTotalSales { get; set; }

        [Required]
        [JsonPropertyName("total_operation")]
        public decimal TotalOperation { get; set; }

        [JsonPropertyName("total_non_taxed")]
        public decimal TotalNonTaxed { get; set; } = 0;

        [Required]
        [JsonPropertyName("total_to_pay")]
        public decimal TotalToPay { get; set; }

        [Required]
        [JsonPropertyName("operation_condition")]
        public int OperationCondition { get; set; }

        [JsonPropertyName("iva_retention")]
        public decimal IvaRetention { get; set; } = 0;

        [JsonPropertyName("iva_perception")]
        public decimal IvaPerception { get; set; } = 0;

        [JsonPropertyName("income_retention")]
        public decimal IncomeRetention { get; set; } = 0;

        [JsonPropertyName("balance_in_favor")]
        public decimal BalanceInFavor { get; set; } = 0;

        // ✅ AGREGAR: Array de impuestos en el resumen
        [JsonPropertyName("taxes")]
        public List<TaxDTO>? Taxes { get; set; }

        [Required]
        [JsonPropertyName("payment_types")]
        public List<PaymentTypeDTO> PaymentTypes { get; set; }
    }

    // ✅ AGREGAR: Clase para los impuestos del resumen
    public class TaxDTO
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public decimal Value { get; set; }
    }

    // ✅ DOCUMENTO RELACIONADO
    public class RelatedDocRequestDTO
    {
        [Required]
        [JsonPropertyName("document_type")]
        public string DocumentType { get; set; }

        [Required]
        [JsonPropertyName("document_number")]
        public string DocumentNumber { get; set; }

        [Required]
        [JsonPropertyName("emission_date")]
        public DateTime EmissionDate { get; set; }

        [Required]
        [JsonPropertyName("generation_type")]
        public int GenerationType { get; set; }
    }

    // ✅ VENTA A TERCEROS
    public class ThirdPartySaleRequestDTO
    {
        [Required]
        public string Nit { get; set; }

        [Required]
        public string Name { get; set; }
    }

    // ✅ DIRECCIÓN
    public class AddressDTO
    {
        [Required]
        public string Department { get; set; }

        [Required]
        public string Municipality { get; set; }

        [Required]
        public string Complement { get; set; }
    }

    // ✅ TIPO DE PAGO
    public class PaymentTypeDTO
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string? Reference { get; set; }

        public int? Term { get; set; }

        public string? Period { get; set; }
    }
}
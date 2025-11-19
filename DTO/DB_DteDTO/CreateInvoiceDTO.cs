using System.Text.Json.Serialization;
namespace ApiContabsv.DTO.DB_DteDTO
{
    public class CreateInvoiceRequestDTO
    {
        [JsonPropertyName("clientId")]
        public int ClientId { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("items")]
        public List<InvoiceItemRequestDTO> Items { get; set; } = new();

        [JsonPropertyName("receiver")]
        public ReceiverRequestDTO? Receiver { get; set; }

        [JsonPropertyName("modelType")]
        public int? ModelType { get; set; }

        [JsonPropertyName("summary")]
        public InvoiceSummaryRequestDTO? Summary { get; set; }

        [JsonPropertyName("certificatePassword")]
        public string? CertificatePassword { get; set; }

        [JsonPropertyName("environment")]
        public string? Environment { get; set; }

        [JsonPropertyName("sendToHacienda")]
        public bool? SendToHacienda { get; set; } = true;

        [JsonPropertyName("third_party_sale")]
        public object? ThirdPartySale { get; set; }

        [JsonPropertyName("related_docs")]
        public object[]? RelatedDocs { get; set; }

        [JsonPropertyName("other_docs")]
        public object[]? OtherDocs { get; set; }

        [JsonPropertyName("appendixes")]
        public object[]? Appendixes { get; set; }
    }

    public class SigningResult
    {
        public bool Success { get; set; }
        public string Response { get; set; } = "";
        public string? Error { get; set; }
    }

    public class InvoiceItemRequestDTO
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unit_measure")]
        public int UnitMeasure { get; set; }

        [JsonPropertyName("unit_price")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("non_subject_sale")]
        public decimal NonSubjectSale { get; set; }

        [JsonPropertyName("exempt_sale")]
        public decimal ExemptSale { get; set; }

        [JsonPropertyName("taxed_sale")]
        public decimal TaxedSale { get; set; }

        [JsonPropertyName("suggested_price")]
        public decimal SuggestedPrice { get; set; }

        [JsonPropertyName("non_taxed")]
        public decimal NonTaxed { get; set; }

        [JsonPropertyName("iva_item")]
        public decimal IvaItem { get; set; }
    }

    public class ReceiverRequestDTO
    {
        [JsonPropertyName("document_type")]
        public string? DocumentType { get; set; }

        [JsonPropertyName("document_number")]
        public string? DocumentNumber { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("address")]
        public AddressRequestDTO? Address { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    public class AddressRequestDTO
    {
        [JsonPropertyName("department")]
        public string Department { get; set; } = "";

        [JsonPropertyName("municipality")]
        public string Municipality { get; set; } = "";

        [JsonPropertyName("complement")]
        public string Complement { get; set; } = "";
    }

    public class InvoiceSummaryRequestDTO
    {
        [JsonPropertyName("total_non_subject")]
        public decimal TotalNonSubject { get; set; }

        [JsonPropertyName("total_exempt")]
        public decimal TotalExempt { get; set; }

        [JsonPropertyName("total_taxed")]
        public decimal TotalTaxed { get; set; }

        [JsonPropertyName("sub_total")]
        public decimal SubTotal { get; set; }

        [JsonPropertyName("non_subject_discount")]
        public decimal NonSubjectDiscount { get; set; }

        [JsonPropertyName("exempt_discount")]
        public decimal ExemptDiscount { get; set; }

        [JsonPropertyName("taxed_discount")]
        public decimal TaxedDiscount { get; set; }

        [JsonPropertyName("discount_percentage")]
        public decimal DiscountPercentage { get; set; }

        [JsonPropertyName("total_discount")]
        public decimal TotalDiscount { get; set; }

        [JsonPropertyName("sub_total_sales")]
        public decimal SubTotalSales { get; set; }

        [JsonPropertyName("total_operation")]
        public decimal TotalOperation { get; set; }

        [JsonPropertyName("total_non_taxed")]
        public decimal TotalNonTaxed { get; set; }

        [JsonPropertyName("total_to_pay")]
        public decimal TotalToPay { get; set; }

        [JsonPropertyName("operation_condition")]
        public int OperationCondition { get; set; }

        [JsonPropertyName("iva_retention")]
        public decimal IvaRetention { get; set; }

        [JsonPropertyName("total_iva")]
        public decimal TotalIva { get; set; }

        [JsonPropertyName("payment_types")]
        public List<PaymentTypeRequestDTO>? PaymentTypes { get; set; }
    }

    public class PaymentTypeRequestDTO
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}
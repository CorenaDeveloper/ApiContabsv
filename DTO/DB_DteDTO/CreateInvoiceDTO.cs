namespace ApiContabsv.DTO.DB_DteDTO
{
    public class CreateInvoiceRequestDTO
    {
        public int ClientId { get; set; }
        public int UserId { get; set; }
        public List<InvoiceItemRequestDTO> Items { get; set; } = new();
        public ReceiverRequestDTO? Receiver { get; set; }
        public int? ModelType { get; set; }
        public InvoiceSummaryRequestDTO? Summary { get; set; }
        public string? CertificatePassword { get; set; }
        public string? Environment { get; set; } // "00" = pruebas, "01" = producción
        public bool? SendToHacienda { get; set; } = true;

        public object? ThirdPartySale { get; set; }
        public object[]? RelatedDocs { get; set; }
        public object[]? OtherDocs { get; set; }
        public object[]? Appendixes { get; set; }
    }

    public class SigningResult
    {
        public bool Success { get; set; }
        public string Response { get; set; } = "";
        public string? Error { get; set; }
    }

    // Resto de DTOs existentes...
    public class InvoiceItemRequestDTO
    {
        public int Type { get; set; }
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public int UnitMeasure { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public string? Code { get; set; }
        public decimal NonSubjectSale { get; set; }
        public decimal ExemptSale { get; set; }
        public decimal TaxedSale { get; set; }
        public decimal SuggestedPrice { get; set; }
        public decimal NonTaxed { get; set; }
        public decimal IvaItem { get; set; }
    }

    public class ReceiverRequestDTO
    {
        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public string? Name { get; set; }
        public AddressRequestDTO? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class AddressRequestDTO
    {
        public string Department { get; set; } = "";
        public string Municipality { get; set; } = "";
        public string Complement { get; set; } = "";
    }

    public class InvoiceSummaryRequestDTO
    {
        public decimal TotalNonSubject { get; set; }
        public decimal TotalExempt { get; set; }
        public decimal TotalTaxed { get; set; }
        public decimal SubTotal { get; set; }
        public decimal NonSubjectDiscount { get; set; }
        public decimal ExemptDiscount { get; set; }
        public decimal TaxedDiscount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal SubTotalSales { get; set; }
        public decimal TotalOperation { get; set; }
        public decimal TotalNonTaxed { get; set; }
        public decimal TotalToPay { get; set; }
        public int OperationCondition { get; set; }
        public decimal IvaRetention { get; set; }
        public decimal TotalIva { get; set; }
        public List<PaymentTypeRequestDTO>? PaymentTypes { get; set; }
    }

    public class PaymentTypeRequestDTO
    {
        public string Code { get; set; } = "";
        public decimal Amount { get; set; }
    }

}

namespace ApiContabsv.DTO.DB_ContabsvDTO
{
    // Modelos
    public class WompiTransactionResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string CheckoutUrl { get; set; }
        public string Error { get; set; }
    }

    public class WompiTransactionStatus
    {
        public bool Success { get; set; }
        public string Status { get; set; }
        public string TransactionId { get; set; }
        public string RawResponse { get; set; }
    }
}

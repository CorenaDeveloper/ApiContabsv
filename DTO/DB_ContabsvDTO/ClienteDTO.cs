namespace ApiContabsv.DTO.DB_ContabsvDTO
{
    public class SmtpConfigDTO
    {
        public string SmtpServer { get; set; }
        public int? SmtpPort { get; set; }
        public string SmtpEmail { get; set; }
        public string SmtpPassword { get; set; }
        public bool? SmtpSsl { get; set; }
    }
}

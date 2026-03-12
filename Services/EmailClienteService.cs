using System.Net;
using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;
using ApiContabsv.Models.Contabsv;

namespace ApiContabsv.Services
{
    public interface IEmailClienteService
    {
        Task<bool> EnviarEmailAsync(Cliente cliente, string toEmail, string toName, string subject, string htmlContent, byte[]? pdfAdjunto = null, string? jsonAdjunto = null, string nombreArchivo = "documento");
    }

    public class EmailClienteService : IEmailClienteService
    {
        private readonly IConfiguration _configuration;

        public EmailClienteService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> EnviarEmailAsync(Cliente cliente, string toEmail, string toName, string subject, string htmlContent, byte[]? pdfAdjunto = null, string? jsonAdjunto = null, string nombreArchivo = "documento")
        {
            if (!string.IsNullOrEmpty(cliente.SmtpServer) && !string.IsNullOrEmpty(cliente.SmtpEmail))
                return await EnviarSmtpAsync(cliente, toEmail, toName, subject, htmlContent, pdfAdjunto, jsonAdjunto, nombreArchivo);

            return await EnviarSendGridAsync(toEmail, toName, subject, htmlContent, pdfAdjunto, jsonAdjunto, nombreArchivo);
        }

        private async Task<bool> EnviarSmtpAsync(Cliente cliente, string toEmail, string toName, string subject, string htmlContent, byte[]? pdfAdjunto, string? jsonAdjunto, string nombreArchivo)
        {
            try
            {
                using var smtp = new SmtpClient(cliente.SmtpServer, cliente.SmtpPort ?? 587)
                {
                    Credentials = new NetworkCredential(cliente.SmtpEmail, cliente.SmtpPassword),
                    EnableSsl = cliente.SmtpSsl ?? true
                };

                using var mensaje = new MailMessage
                {
                    From = new MailAddress(cliente.SmtpEmail, cliente.NombreComercial ?? cliente.NombreRazonSocial),
                    Subject = subject,
                    Body = htmlContent,
                    IsBodyHtml = true
                };

                mensaje.To.Add(new MailAddress(toEmail, toName));

                if (pdfAdjunto != null)
                {
                    var pdfStream = new MemoryStream(pdfAdjunto);
                    mensaje.Attachments.Add(new System.Net.Mail.Attachment(pdfStream, $"{nombreArchivo}.pdf", "application/pdf"));
                }

                if (!string.IsNullOrEmpty(jsonAdjunto))
                {
                    var jsonStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonAdjunto));
                    mensaje.Attachments.Add(new System.Net.Mail.Attachment(jsonStream, $"{nombreArchivo}.json", "application/json"));
                }

                await smtp.SendMailAsync(mensaje);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> EnviarSendGridAsync(string toEmail, string toName, string subject, string htmlContent, byte[]? pdfAdjunto, string? jsonAdjunto, string nombreArchivo)
        {
            try
            {
                var apiKey = _configuration["SendGrid:ApiKey"];
                var fromEmail = _configuration["SendGrid:FromEmail"];
                var fromName = _configuration["SendGrid:FromName"];

                var client = new SendGridClient(apiKey);
                var msg = new SendGridMessage
                {
                    From = new EmailAddress(fromEmail, fromName),
                    Subject = subject,
                    HtmlContent = htmlContent
                };
                msg.AddTo(new EmailAddress(toEmail, toName));

                if (pdfAdjunto != null)
                    msg.AddAttachment($"{nombreArchivo}.pdf", Convert.ToBase64String(pdfAdjunto), "application/pdf");

                if (!string.IsNullOrEmpty(jsonAdjunto))
                    msg.AddAttachment($"{nombreArchivo}.json", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonAdjunto)), "application/json");

                var response = await client.SendEmailAsync(msg);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
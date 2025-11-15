using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Configuration;

using MimeKit;

namespace OmniMonitor.Server.Services
{
    public interface IMailService
    {
        Task SendEmailAsync(
            List<string> recipients,
            string subject,
            string message,
            byte[]? pdfAttachment = null,
            string? pdfName = null);
    }

    public class MailService : IMailService
    {
        private readonly IConfiguration _config;

        public MailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(
            List<string> recipients,
            string subject,
            string message,
            byte[]? pdfAttachment = null,
            string? pdfName = null)
        {
            var email = new MimeMessage();

            var fromName = _config["Email:FromName"] ?? "OmniMonitor Reports";
            var fromAddress = _config["Email:FromAddress"];

            if (string.IsNullOrWhiteSpace(fromAddress))
                throw new Exception("Email 'FromAddress' is not configured.");

            email.From.Add(new MailboxAddress(fromName, fromAddress));

            foreach (var r in recipients)
                if (!string.IsNullOrWhiteSpace(r))
                    email.To.Add(new MailboxAddress("", r));

            email.Subject = subject;

            var builder = new BodyBuilder { TextBody = message };

            if (pdfAttachment != null && pdfName != null)
                builder.Attachments.Add(pdfName, pdfAttachment, new ContentType("application", "pdf"));

            email.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            var host = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var user = _config["Email:SmtpUser"];
            var pass = _config["Email:SmtpPass"];

            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(email);
            await client.DisconnectAsync(true);
        }
    }
}

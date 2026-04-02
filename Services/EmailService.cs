using System.Net;
using System.Net.Mail;

namespace TINH_FINAL_2256.Services
{
    /// <summary>
    /// D?ch v? g?i email
    /// </summary>
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var sender = _configuration["EmailSettings:SenderEmail"] ?? "";
                var senderPassword = _configuration["EmailSettings:SenderPassword"] ?? "";

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(sender, senderPassword);

                    var mailMessage = new MailMessage(sender, to, subject, body)
                    {
                        IsBodyHtml = isHtml
                    };

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email sent successfully to {to}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {to}: {ex.Message}");
                throw;
            }
        }
    }
}

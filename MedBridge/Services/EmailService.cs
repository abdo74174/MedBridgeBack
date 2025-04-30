using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MedBridge.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace MedBridge.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation($"Preparing to send email to {toEmail} with subject: {subject}");

            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                _logger.LogInformation("Sending email...");
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, $"SMTP error sending email to {toEmail}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {toEmail}");
                throw;
            }
        }
    }
}
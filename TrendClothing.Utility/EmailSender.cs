using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace TrendClothing.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly string _apiKey;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _logger = logger;
            _apiKey = configuration["Authentication:ResendApiKey"] ?? "";
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var client = new SmtpClient("smtp.resend.com", 587)
                {
                    Credentials = new NetworkCredential("resend", _apiKey),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("onboarding@resend.dev", "TrendClothing"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {Email} via Resend SMTP", email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Email send failed to {Email}: {Message}", email, ex.Message);
            }
        }
    }
}
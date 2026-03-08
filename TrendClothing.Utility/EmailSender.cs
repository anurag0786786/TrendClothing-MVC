using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Resend;

namespace TrendClothing.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly IResend _resend;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IResend resend, ILogger<EmailSender> logger)
        {
            _resend = resend;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var message = new EmailMessage();
                message.From = "TrendClothing <onboarding@resend.dev>";
                message.To.Add(email);
                message.Subject = subject;
                message.HtmlBody = htmlMessage;
                await _resend.EmailSendAsync(message);
            }
            catch (Exception ex)
            {
                // Email fail hone pe app crash nahi karega
                _logger.LogWarning("Email send failed to {Email}: {Message}", email, ex.Message);
            }
        }
    }
}
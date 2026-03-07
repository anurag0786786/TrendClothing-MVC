
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace TrendClothing.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly EmailSettings _emailSettings;
        public EmailSender(IConfiguration configuration, IOptions<EmailSettings> emailSettings)
        {
            _configuration = configuration;
            _emailSettings = emailSettings.Value;
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await Execute(email, subject, htmlMessage); // ✅ async/await properly
        }

        public async Task Execute(string email, string subject, string message)
        {
            try
            {
                string toEmail = string.IsNullOrEmpty(email)
                    ? _emailSettings.ToEmail
                    : email;

                MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(_emailSettings.UsernameEmail, "Trend Clothing")
                };

                mailMessage.To.Add(toEmail);
                mailMessage.Subject = "Trend Clothing : " + subject;
                mailMessage.Body = message;
                mailMessage.IsBodyHtml = true;
                mailMessage.Priority = MailPriority.High;

                using (SmtpClient smtpClient = new SmtpClient(
                    _emailSettings.PrimaryDomain,
                    _emailSettings.PrimaryPort))
                {
                    smtpClient.Credentials = new NetworkCredential(
                        _emailSettings.UsernameEmail,
                        _emailSettings.UsernamePassword
                    );
                    smtpClient.EnableSsl = true;
                    await smtpClient.SendMailAsync(mailMessage); // ✅ async version
                }
            }
            catch (Exception ex)
            {
                // ✅ Ab error log hoga — silent nahi rahega
                Console.WriteLine($"EMAIL ERROR: {ex.Message}");
                Console.WriteLine($"INNER: {ex.InnerException?.Message}");
                throw; // ✅ Error propagate hoga
            }
        }

    }
}

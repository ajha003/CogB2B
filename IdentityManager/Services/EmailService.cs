using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace IdentityManager.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var smtpServer = smtpSettings["Server"];
                var smtpPort = int.Parse(smtpSettings["Port"]!);
                var smtpUsername = smtpSettings["Username"];
                var smtpPassword = smtpSettings["Password"];
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"];

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail!, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var subject = "Password Reset Request";
            var body = GeneratePasswordResetEmailBody(resetLink);
            return await SendEmailAsync(email, subject, body, true);
        }

        public async Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            var subject = "Email Confirmation";
            var body = GenerateEmailConfirmationBody(confirmationLink);
            return await SendEmailAsync(email, subject, body, true);
        }

        private string GeneratePasswordResetEmailBody(string resetLink)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Password Reset Request</h2>");
            sb.AppendLine("<p>You have requested to reset your password.</p>");
            sb.AppendLine("<p>Click the link below to reset your password:</p>");
            sb.AppendLine($"<p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>");
            sb.AppendLine("<p>If you didn't request this, please ignore this email.</p>");
            sb.AppendLine("<p>This link will expire in 1 hour.</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string GenerateEmailConfirmationBody(string confirmationLink)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h2>Email Confirmation</h2>");
            sb.AppendLine("<p>Thank you for registering with us!</p>");
            sb.AppendLine("<p>Please confirm your email address by clicking the link below:</p>");
            sb.AppendLine($"<p><a href='{confirmationLink}' style='background-color: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Confirm Email</a></p>");
            sb.AppendLine("<p>If you didn't create an account, please ignore this email.</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
    }

    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task<bool> SendPasswordResetEmailAsync(string email, string resetLink);
        Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink);
    }
}
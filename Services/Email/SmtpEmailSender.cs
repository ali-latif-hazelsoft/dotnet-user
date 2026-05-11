using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace dotnet_user.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpSection = _configuration.GetSection("EmailSettings");

            string host = smtpSection["Host"] ?? "";
            int port = int.Parse(smtpSection["Port"] ?? "587");
            string fromEmail = smtpSection["FromEmail"] ?? "";
            string username = smtpSection["Username"] ?? "";
            string password = smtpSection["Password"] ?? "";
            bool enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");

            using var client = new SmtpClient(host, port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };

            message.To.Add(email);

            await client.SendMailAsync(message);
        }
    }
}

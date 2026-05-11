using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using dotnet_user.Dtos.Auth;
using Microsoft.Extensions.Options;

namespace dotnet_user.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettingsDto _emailSettings;

        public SmtpEmailSender(IOptions<EmailSettingsDto> emailSettingsOptions)
        {
            if (emailSettingsOptions == null)
            {
                throw new ArgumentNullException(nameof(emailSettingsOptions));
            }

            _emailSettings =
                emailSettingsOptions.Value
                ?? throw new InvalidOperationException("Email settings are missing.");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Recipient email is required.", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException("Email subject is required.", nameof(subject));
            }

            if (string.IsNullOrWhiteSpace(htmlMessage))
            {
                throw new ArgumentException("Email message is required.", nameof(htmlMessage));
            }

            using (SmtpClient client = new SmtpClient(_emailSettings.Host, _emailSettings.Port))
            {
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.EnableSsl = _emailSettings.EnableSsl;
                client.Credentials = new NetworkCredential(
                    _emailSettings.Username,
                    _emailSettings.Password
                );

                using (MailMessage message = new MailMessage())
                {
                    message.From = new MailAddress(_emailSettings.FromEmail);
                    message.Subject = subject;
                    message.Body = htmlMessage;
                    message.IsBodyHtml = true;
                    message.To.Add(email);

                    await client.SendMailAsync(message);
                }
            }
        }
    }
}

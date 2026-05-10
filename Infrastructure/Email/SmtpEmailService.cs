using Bachelor_s_Point.Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Bachelor_s_Point.Infrastructure.Email
{
    public class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "Bachelor's Point";
    }

    public class SmtpEmailService : ISmtpEmailService
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toAddress, string subject, string body, bool isHtml = true)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                throw new ArgumentException("Recipient address is required", nameof(toAddress));
            }

            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
            {
                throw new InvalidOperationException(
                    "SMTP is not configured. Set the Smtp section in appsettings.json.");
            }

            // SYSLIB0014: System.Net.Mail.SmtpClient is obsolete in .NET 6+; the
            // recommended replacement is MailKit. SmtpClient still works for
            // standard SMTP submission, so we suppress the warning rather than
            // pull in another dependency.
#pragma warning disable SYSLIB0014
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_options.UserName, _options.Password)
            };
#pragma warning restore SYSLIB0014

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            message.To.Add(toAddress);

            _logger.LogInformation("Sending email to {To} with subject \"{Subject}\"", toAddress, subject);

            await client.SendMailAsync(message);
        }
    }
}

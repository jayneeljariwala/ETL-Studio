using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ETL.Application.Interfaces.Services.Email;

namespace ETL.Infrastructure.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            // If no SMTP configured, we can log or just ignore (for development)
            // For simplicity, we'll just return without sending.
            _logger.LogWarning("Email not sent because SMTP host is not configured.");
            return;
        }

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = message,
            IsBodyHtml = false,
        };
        mailMessage.To.Add(email);

        try
        {
            using var smtp = new SmtpClient(_options.Host, _options.Port)
            {
                Credentials = new NetworkCredential(_options.UserName, _options.Password),
                EnableSsl = _options.EnableSsl,
            };

            await smtp.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent to {Email} with subject: {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", email, subject);
        }
    }
}
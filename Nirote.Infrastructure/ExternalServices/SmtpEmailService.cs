using System.Net;
using System.Net.Mail;
using Nirote.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nirote.Infrastructure.ExternalServices;

public class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromName;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        var smtp = config.GetSection("Smtp");
        _host = smtp["Host"] ?? "smtp.gmail.com";
        _port = int.Parse(smtp["Port"] ?? "587");
        _username = smtp["Username"] ?? "";
        _password = smtp["Password"] ?? "";
        _fromName = smtp["FromName"] ?? "House of Vastrikaa";
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody)
        => SendBulkAsync([to], subject, htmlBody);

    public async Task SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody)
    {
        var list = recipients.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        _logger.LogInformation("Sending coupon email to {Count} customers: {Subject}", list.Count, subject);

        using var client = new SmtpClient(_host, _port)
        {
            Credentials = new NetworkCredential(_username, _password),
            EnableSsl = true,
        };

        var failed = 0;
        foreach (var to in list)
        {
            try
            {
                using var msg = new MailMessage
                {
                    From = new MailAddress(_username, _fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                };
                msg.To.Add(to);
                await client.SendMailAsync(msg);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Failed to send email to {Email}", to);
            }
        }

        if (failed > 0)
            _logger.LogWarning("Coupon email: {Failed}/{Total} deliveries failed", failed, list.Count);
    }
}

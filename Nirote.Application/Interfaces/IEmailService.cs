namespace Nirote.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody);
}

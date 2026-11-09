using System.Collections.Concurrent;

namespace FunkoApi.Back.Services.Email;

public class MemoryEmailService(ILogger<MemoryEmailService> logger) : IEmailService {
    private static readonly ConcurrentBag<EmailMessage> _sentEmails = new();

    public static IReadOnlyList<EmailMessage> SentEmails => _sentEmails.ToList();

    public Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default) {
        logger.LogInformation(
            "[MOCK EMAIL] Para: {To}, Asunto: {Subject}", message.To, message.Subject);
        _sentEmails.Add(message);
        return Task.CompletedTask;
    }

    public Task SendHtmlEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
        => SendAsync(new EmailMessage {
            To = to,
            Subject = subject,
            Body = htmlBody,
            IsHtml = true
        }, cancellationToken);

    public static void Clear() {
        while (!_sentEmails.IsEmpty)
            _sentEmails.TryTake(out _);
    }
}

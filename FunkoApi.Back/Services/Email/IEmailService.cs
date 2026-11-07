namespace FunkoApi.Back.Services.Email;

public interface IEmailService {
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task SendHtmlEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public class EmailMessage {
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
}
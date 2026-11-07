using FunkoApi.Back.Config;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace FunkoApi.Back.Services.Email;

public class MailKitEmailService(
    IOptions<SmtpSettings> options,
    ILogger<MailKitEmailService> logger,
    ISmtpClient? client = null) : IEmailService {

    private readonly SmtpSettings _settings = options.Value;

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default) {

        var mimeMessage = new MimeMessage {
            Subject = message.Subject,
            Body = new TextPart(message.IsHtml ? TextFormat.Html : TextFormat.Plain) { Text = message.Body }
        };

        mimeMessage.From.Add(new MailboxAddress(_settings.DisplayName, _settings.From));
        mimeMessage.To.Add(new MailboxAddress("", message.To));

        logger.LogInformation(
            "Enviando email a {To} con asunto: {Subject}",
            message.To, message.Subject);

        try {
            using var smtpClient = client ?? new SmtpClient();

            await smtpClient.ConnectAsync(
                _settings.Host,
                _settings.Port,
                GetSecureSocket(_settings.Security),
                cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Username)) {
                await smtpClient.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password,
                    cancellationToken);
            }

            await smtpClient.SendAsync(mimeMessage, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);

            logger.LogInformation(
                "Email enviado exitosamente a {To}", message.To);
        } catch (Exception ex) {
            logger.LogError(ex, "Error enviando email a {To}: {Error}", message.To, ex.Message);
            throw;
        }
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

    private static SecureSocketOptions GetSecureSocket(string security) {
        return security.ToLowerInvariant() switch {
            "ssl" => SecureSocketOptions.SslOnConnect,
            "tls" => SecureSocketOptions.StartTls,
            "none" => SecureSocketOptions.None,
            _ => SecureSocketOptions.Auto
        };
    }
}

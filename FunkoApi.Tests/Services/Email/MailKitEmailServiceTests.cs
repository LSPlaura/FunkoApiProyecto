using FluentAssertions;
using FunkoApi.Back.Config;
using FunkoApi.Back.Services.Email;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Services.Email;

[TestFixture]
public class MailKitEmailServiceTests
{
    private Mock<ISmtpClient> _smtpClientMock = null!;
    private Mock<ILogger<MailKitEmailService>> _loggerMock = null!;
    private SmtpSettings _settings = null!;
    private MailKitEmailService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _smtpClientMock = new Mock<ISmtpClient>();
        _loggerMock = new Mock<ILogger<MailKitEmailService>>();
        _settings = new SmtpSettings
        {
            Host = "smtp.example.com", Port = 587, From = "remitente@example.com",
            DisplayName = "FunkoApi", Security = "tls"
        };
        _smtpClientMock.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _smtpClientMock.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _smtpClientMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(),
            It.IsAny<ITransferProgress>())).ReturnsAsync("OK");
        _smtpClientMock.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _service = new MailKitEmailService(Options.Create(_settings), _loggerMock.Object, _smtpClientMock.Object);
    }

    [TestCase("ssl", SecureSocketOptions.SslOnConnect, true)]
    [TestCase("TLS", SecureSocketOptions.StartTls, false)]
    [TestCase("none", SecureSocketOptions.None, true)]
    [TestCase("Auto", SecureSocketOptions.Auto, false)]
    [TestCase("desconocido", SecureSocketOptions.Auto, true)]
    public async Task SendAsync_MensajeValido_EnviaContenidoYDesconecta(
        string security, SecureSocketOptions socket, bool isHtml)
    {
        // Arrange
        _settings.Security = security;
        using var cancellation = new CancellationTokenSource();
        var email = new EmailMessage
        {
            To = "destinatario@example.com", Subject = "Notificación ñ",
            Body = "Contenido con precio 15 €", IsHtml = isHtml
        };

        // Act
        await _service.SendAsync(email, cancellation.Token);

        // Assert
        _smtpClientMock.Verify(c => c.ConnectAsync(_settings.Host, _settings.Port, socket,
            cancellation.Token), Times.Once);
        _smtpClientMock.Verify(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _smtpClientMock.Verify(c => c.SendAsync(It.Is<MimeMessage>(m =>
            m.Subject == email.Subject && m.From.Mailboxes.Single().Address == _settings.From
            && m.From.Mailboxes.Single().Name == _settings.DisplayName
            && m.To.Mailboxes.Single().Address == email.To
            && ((TextPart)m.Body!).Text == email.Body
            && ((TextPart)m.Body!).IsHtml == isHtml), cancellation.Token, null), Times.Once);
        _smtpClientMock.Verify(c => c.DisconnectAsync(true, cancellation.Token), Times.Once);
        _smtpClientMock.Verify(c => c.Dispose(), Times.Once);
        _loggerMock.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email enviado exitosamente")),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        _loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("usuario@example.com")]
    public async Task SendAsync_ConOSinUsuario_AutenticaSoloCuandoExiste(string? username)
    {
        // Arrange
        _settings.Username = username;
        _settings.Password = "clave-de-prueba";
        var email = new EmailMessage { To = "destinatario@example.com", Subject = "Prueba", Body = "Hola" };

        // Act
        await _service.SendAsync(email);

        // Assert
        _smtpClientMock.Verify(c => c.ConnectAsync(_settings.Host, _settings.Port,
            SecureSocketOptions.StartTls, CancellationToken.None), Times.Once);
        _smtpClientMock.Verify(c => c.AuthenticateAsync(username!, _settings.Password,
            CancellationToken.None), string.IsNullOrEmpty(username) ? Times.Never() : Times.Once());
        _smtpClientMock.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), CancellationToken.None, null), Times.Once);
        _smtpClientMock.Verify(c => c.DisconnectAsync(true, CancellationToken.None), Times.Once);
        _smtpClientMock.Verify(c => c.Dispose(), Times.Once);
    }

    [TestCase("conexion")]
    [TestCase("autenticacion")]
    [TestCase("envio")]
    [TestCase("desconexion")]
    public async Task SendAsync_FalloSmtp_RegistraYPropagaMismaExcepcionYLiberaCliente(string paso)
    {
        // Arrange
        _settings.Username = "usuario@example.com";
        _settings.Password = "clave-de-prueba";
        using var cancellation = new CancellationTokenSource();
        var error = new InvalidOperationException("Fallo simulado de " + paso);
        var email = new EmailMessage { To = "destinatario@example.com", Subject = "Prueba", Body = "Hola" };
        switch (paso)
        {
            case "conexion":
                _smtpClientMock.Setup(c => c.ConnectAsync(_settings.Host, _settings.Port,
                    SecureSocketOptions.StartTls, cancellation.Token)).ThrowsAsync(error);
                break;
            case "autenticacion":
                _smtpClientMock.Setup(c => c.AuthenticateAsync(_settings.Username, _settings.Password,
                    cancellation.Token)).ThrowsAsync(error);
                break;
            case "envio":
                _smtpClientMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), cancellation.Token, null))
                    .ThrowsAsync(error);
                break;
            case "desconexion":
                _smtpClientMock.Setup(c => c.DisconnectAsync(true, cancellation.Token)).ThrowsAsync(error);
                break;
        }

        // Act
        Func<Task> act = async () => await _service.SendAsync(email, cancellation.Token);

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(error);
        _smtpClientMock.Verify(c => c.ConnectAsync(_settings.Host, _settings.Port,
            SecureSocketOptions.StartTls, cancellation.Token), Times.Once);
        _smtpClientMock.Verify(c => c.AuthenticateAsync(_settings.Username, _settings.Password,
            cancellation.Token), paso == "conexion" ? Times.Never() : Times.Once());
        _smtpClientMock.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), cancellation.Token, null),
            paso == "conexion" || paso == "autenticacion" ? Times.Never() : Times.Once());
        _smtpClientMock.Verify(c => c.DisconnectAsync(true, cancellation.Token),
            paso == "desconexion" ? Times.Once() : Times.Never());
        _smtpClientMock.Verify(c => c.Dispose(), Times.Once);
        _loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error enviando email")),
            error, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        _loggerMock.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email enviado exitosamente")),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    [Test]
    public async Task SendHtmlEmailAsync_MensajeValido_EnviaHtmlYConservaDatosYToken()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var to = "destinatario@example.com";
        var subject = "Prueba HTML";
        var body = "<h1>Hola</h1>";

        // Act
        await _service.SendHtmlEmailAsync(to, subject, body, cancellation.Token);

        // Assert
        _smtpClientMock.Verify(c => c.SendAsync(It.Is<MimeMessage>(m =>
            m.To.Mailboxes.Single().Address == to && m.Subject == subject
            && ((TextPart)m.Body!).IsHtml && ((TextPart)m.Body!).Text == body), cancellation.Token, null), Times.Once);
        _smtpClientMock.Verify(c => c.ConnectAsync(_settings.Host, _settings.Port,
            SecureSocketOptions.StartTls, cancellation.Token), Times.Once);
        _smtpClientMock.Verify(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _smtpClientMock.Verify(c => c.DisconnectAsync(true, cancellation.Token), Times.Once);
        _smtpClientMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Test]
    public async Task SendAsync_ConexionCancelada_PropagaCancelacionSinAutenticarNiEnviar()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = new OperationCanceledException(cancellation.Token);
        _smtpClientMock.Setup(c => c.ConnectAsync(_settings.Host, _settings.Port,
            SecureSocketOptions.StartTls, cancellation.Token)).ThrowsAsync(error);
        var email = new EmailMessage { To = "destinatario@example.com", Subject = "Cancelado", Body = "Hola" };

        // Act
        Func<Task> act = async () => await _service.SendAsync(email, cancellation.Token);

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.Should().BeSameAs(error);
        _smtpClientMock.Verify(c => c.ConnectAsync(_settings.Host, _settings.Port,
            SecureSocketOptions.StartTls, cancellation.Token), Times.Once);
        _smtpClientMock.Verify(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _smtpClientMock.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(),
            It.IsAny<ITransferProgress>()), Times.Never);
        _smtpClientMock.Verify(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _smtpClientMock.Verify(c => c.Dispose(), Times.Once);
        _loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            error, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [TestCase("ssl", true)]
    [TestCase("TLS", false)]
    [TestCase("none", true)]
    [TestCase("Auto", false)]
    [TestCase("desconocido", true)]
    public async Task SendAsync_PuertoInvalido_RegistraErrorYPropagaExcepcionSinRed(string security, bool isHtml)
    {
        // Arrange
        var settings = new SmtpSettings
        {
            Host = "localhost", Port = -1, From = "remitente@example.com", Security = security
        };
        var loggerMock = new Mock<ILogger<MailKitEmailService>>();
        var service = new MailKitEmailService(Options.Create(settings), loggerMock.Object);
        var email = new EmailMessage
        {
            To = "destinatario@example.com", Subject = "Prueba", Body = "Contenido", IsHtml = isHtml
        };

        // Act
        Func<Task> act = async () => await service.SendAsync(email);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        loggerMock.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(email.To) && v.ToString()!.Contains(email.Subject)),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error enviando email")),
            It.IsAny<ArgumentOutOfRangeException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task SendHtmlEmailAsync_PuertoInvalido_DelegaEnvioYPropagaErrorSinRed()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MailKitEmailService>>();
        var service = new MailKitEmailService(Options.Create(new SmtpSettings
        {
            Host = "localhost", Port = -1, From = "remitente@example.com"
        }), loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.SendHtmlEmailAsync(
            "destinatario@example.com", "Prueba HTML", "<h1>Hola</h1>");

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        loggerMock.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("destinatario@example.com")
                && v.ToString()!.Contains("Prueba HTML")),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<ArgumentOutOfRangeException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}

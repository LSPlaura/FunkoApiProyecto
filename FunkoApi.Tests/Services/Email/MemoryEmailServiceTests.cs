using FluentAssertions;
using FunkoApi.Back.Services.Email;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Services.Email;

[TestFixture]
[NonParallelizable]
public class MemoryEmailServiceTests
{
    private Mock<ILogger<MemoryEmailService>> _loggerMock = null!;
    private MemoryEmailService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<MemoryEmailService>>();
        _service = new MemoryEmailService(_loggerMock.Object);
        MemoryEmailService.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        MemoryEmailService.Clear();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SendAsync_MensajeValido_AlmacenaCorreo(bool isHtml)
    {
        // Arrange
        var email = new EmailMessage {
            To = "test@example.com", Subject = "Nuevo Funko", Body = "Contenido", IsHtml = isHtml
        };

        // Act
        await _service.SendAsync(email);

        // Assert
        MemoryEmailService.SentEmails.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(email);
        VerifyLog();
    }

    [Test]
    public async Task SendHtmlEmailAsync_MensajeValido_AlmacenaCorreoHtml()
    {
        // Arrange
        const string body = "<h1>Nuevo Funko</h1>";

        // Act
        await _service.SendHtmlEmailAsync("test@example.com", "Notificación", body);

        // Assert
        var email = MemoryEmailService.SentEmails.Should().ContainSingle().Subject;
        email.To.Should().Be("test@example.com");
        email.Subject.Should().Be("Notificación");
        email.Body.Should().Be(body);
        email.IsHtml.Should().BeTrue();
        VerifyLog();
    }

    [Test]
    public async Task Clear_ConCorreos_VaciaColeccion()
    {
        // Arrange
        await _service.SendAsync(new EmailMessage { Subject = "Primero" });
        await _service.SendAsync(new EmailMessage { Subject = "Segundo" });

        // Act
        MemoryEmailService.Clear();

        // Assert
        MemoryEmailService.SentEmails.Should().BeEmpty();
        VerifyLog(2);
    }

    [Test]
    public async Task SentEmails_CopiaObtenida_NoCambiaAlEnviarOtroCorreo()
    {
        // Arrange
        var primero = new EmailMessage { To = "primero@example.com", Subject = "Primero" };
        await _service.SendAsync(primero);
        var copia = MemoryEmailService.SentEmails;

        // Act
        await _service.SendAsync(new EmailMessage { To = "segundo@example.com", Subject = "Segundo" });

        // Assert
        copia.Should().ContainSingle().Which.Should().BeSameAs(primero);
        MemoryEmailService.SentEmails.Should().HaveCount(2);
        VerifyLog(2);
    }

    [Test]
    public void Clear_SinCorreos_NoFallaNiGeneraLogsDeEnvio()
    {
        // Arrange / Act
        MemoryEmailService.Clear();

        // Assert
        MemoryEmailService.SentEmails.Should().BeEmpty();
        VerifyLog(0);
    }

    private void VerifyLog(int times = 1)
    {
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(times));
    }
}

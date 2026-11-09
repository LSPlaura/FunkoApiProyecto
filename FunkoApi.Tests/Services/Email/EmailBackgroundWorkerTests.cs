using FluentAssertions;
using FunkoApi.Back.Services.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Services.Email;

[TestFixture]
public class EmailBackgroundWorkerTests
{
    private EmailQueueService _queue = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private Mock<IServiceScopeFactory> _scopeFactoryMock = null!;
    private Mock<IServiceScope> _scopeMock = null!;
    private Mock<IServiceProvider> _providerMock = null!;
    private Mock<ILogger<EmailBackgroundWorker>> _loggerMock = null!;
    private TestEmailBackgroundWorker _worker = null!;

    [SetUp]
    public void SetUp()
    {
        _queue = new EmailQueueService();
        _emailServiceMock = new Mock<IEmailService>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _providerMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<EmailBackgroundWorker>>();
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_providerMock.Object);
        _providerMock.Setup(p => p.GetService(typeof(IEmailService))).Returns(_emailServiceMock.Object);
        _worker = new TestEmailBackgroundWorker(_queue, _scopeFactoryMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown() => _worker.Dispose();

    [Test]
    public async Task ExecuteAsync_TokenYaCancelado_NoProcesaMensajes()
    {
        // Arrange
        _queue.Enqueue(new EmailMessage());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        await _worker.RunAsync(cancellation.Token);

        // Assert
        _queue.Count.Should().Be(1);
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
        _emailServiceMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_CancelacionDuranteEspera_TerminaSinEnviar()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var ejecucion = _worker.RunAsync(cancellation.Token);

        // Act
        cancellation.Cancel();
        await ejecucion;

        // Assert
        ejecucion.IsCompletedSuccessfully.Should().BeTrue();
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
        _emailServiceMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ExecuteAsync_EnvioCorrectoOConError_ContinuaConSiguienteYLiberaScopes(bool fallaPrimero)
    {
        // Arrange
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var primero = new EmailMessage { To = "primero@example.com", Subject = "Primero" };
        var segundo = new EmailMessage { To = "segundo@example.com", Subject = "Segundo" };
        var error = new InvalidOperationException("Error simulado de envío");
        // Un scope distinto por mensaje, también cuando el primer envío falla.
        var segundoScopeMock = new Mock<IServiceScope>();
        var segundoProviderMock = new Mock<IServiceProvider>();
        segundoScopeMock.Setup(s => s.ServiceProvider).Returns(segundoProviderMock.Object);
        segundoProviderMock.Setup(p => p.GetService(typeof(IEmailService))).Returns(_emailServiceMock.Object);
        _scopeFactoryMock.SetupSequence(f => f.CreateScope())
            .Returns(_scopeMock.Object).Returns(segundoScopeMock.Object);
        _queue.Enqueue(primero);
        _queue.Enqueue(segundo);
        if (fallaPrimero)
            _emailServiceMock.Setup(s => s.SendAsync(primero, cancellation.Token)).ThrowsAsync(error);
        else
            _emailServiceMock.Setup(s => s.SendAsync(primero, cancellation.Token)).Returns(Task.CompletedTask);
        _emailServiceMock.Setup(s => s.SendAsync(segundo, cancellation.Token))
            .Callback(() => cancellation.Cancel()).Returns(Task.CompletedTask);

        // Act
        await _worker.RunAsync(cancellation.Token);

        // Assert
        _queue.Count.Should().Be(0);
        _emailServiceMock.Verify(s => s.SendAsync(primero, cancellation.Token), Times.Once);
        _emailServiceMock.Verify(s => s.SendAsync(segundo, cancellation.Token), Times.Once);
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Exactly(2));
        _providerMock.Verify(p => p.GetService(typeof(IEmailService)), Times.Once);
        segundoProviderMock.Verify(p => p.GetService(typeof(IEmailService)), Times.Once);
        _scopeMock.Verify(s => s.Dispose(), Times.Once);
        segundoScopeMock.Verify(s => s.Dispose(), Times.Once);
        _loggerMock.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error procesando email de la cola")),
            error, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), fallaPrimero ? Times.Once() : Times.Never());
    }

    [Test]
    public async Task ExecuteAsync_EnvioCancelado_LiberaScopeYNoProcesaSiguiente()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var primero = new EmailMessage { Subject = "Primero" };
        _queue.Enqueue(primero);
        _queue.Enqueue(new EmailMessage { Subject = "Segundo" });
        _emailServiceMock.Setup(s => s.SendAsync(primero, cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        // Act
        await _worker.RunAsync(cancellation.Token);

        // Assert
        _queue.Count.Should().Be(1);
        _emailServiceMock.Verify(s => s.SendAsync(primero, cancellation.Token), Times.Once);
        _emailServiceMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), cancellation.Token), Times.Once);
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once);
        _providerMock.Verify(p => p.GetService(typeof(IEmailService)), Times.Once);
        _scopeMock.Verify(s => s.Dispose(), Times.Once);
    }

    // Permite ejecutar el método protegido sin arrancar la API ni un host real.
    private class TestEmailBackgroundWorker(
        EmailQueueService queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailBackgroundWorker> logger) : EmailBackgroundWorker(queue, scopeFactory, logger)
    {
        public Task RunAsync(CancellationToken cancellationToken) => base.ExecuteAsync(cancellationToken);
    }
}

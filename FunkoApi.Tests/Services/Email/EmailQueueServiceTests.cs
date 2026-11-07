using FluentAssertions;
using FunkoApi.Back.Services.Email;
using NUnit.Framework;

namespace FunkoApi.Tests.Services.Email;

[TestFixture]
public class EmailQueueServiceTests
{
    [Test]
    public void Enqueue_MensajeValido_IncrementaContador()
    {
        // Arrange
        var queue = new EmailQueueService();
        var email = new EmailMessage { To = "test@example.com", Subject = "Test", Body = "Contenido" };

        // Act
        queue.Enqueue(email);

        // Assert
        queue.Count.Should().Be(1);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DequeueAsync_MensajeEncolado_ConservaDatosYLoRetira(bool isHtml)
    {
        // Arrange
        var queue = new EmailQueueService();
        var email = new EmailMessage
        {
            To = "test@example.com", Subject = "Notificación ñ", Body = "Contenido con precio 15 €", IsHtml = isHtml
        };
        queue.Enqueue(email);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var resultado = await queue.DequeueAsync(cancellation.Token);

        // Assert
        resultado.Should().BeSameAs(email);
        resultado.Should().BeEquivalentTo(email);
        queue.Count.Should().Be(0);
    }

    [Test]
    public async Task DequeueAsync_VariosMensajes_RespetaOrdenDeEntrada()
    {
        // Arrange
        var queue = new EmailQueueService();
        var primero = new EmailMessage { Subject = "Primero" };
        var segundo = new EmailMessage { Subject = "Segundo" };
        queue.Enqueue(primero);
        queue.Enqueue(segundo);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var resultadoPrimero = await queue.DequeueAsync(cancellation.Token);
        var pendientes = queue.Count;
        var resultadoSegundo = await queue.DequeueAsync(cancellation.Token);

        // Assert
        resultadoPrimero.Should().BeSameAs(primero);
        pendientes.Should().Be(1);
        resultadoSegundo.Should().BeSameAs(segundo);
        queue.Count.Should().Be(0);
    }

    [Test]
    public async Task DequeueAsync_ColaVacia_EsperaHastaQueSeEncolaMensaje()
    {
        // Arrange
        var queue = new EmailQueueService();
        var email = new EmailMessage { Subject = "Pendiente" };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var espera = queue.DequeueAsync(cancellation.Token);
        var estabaEsperando = !espera.IsCompleted;
        queue.Enqueue(email);
        var resultado = await espera;

        // Assert
        estabaEsperando.Should().BeTrue();
        resultado.Should().BeSameAs(email);
        queue.Count.Should().Be(0);
    }

    [Test]
    public async Task DequeueAsync_CancelacionDuranteEspera_NoConsumeMensajesPosteriores()
    {
        // Arrange
        var queue = new EmailQueueService();
        using var cancellation = new CancellationTokenSource();
        var espera = queue.DequeueAsync(cancellation.Token);

        // Act
        cancellation.Cancel();
        Func<Task> act = async () => await espera;

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        queue.Count.Should().Be(0);
        var email = new EmailMessage { Subject = "Después de cancelar" };
        queue.Enqueue(email);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        (await queue.DequeueAsync(timeout.Token)).Should().BeSameAs(email);
    }

    [Test]
    public async Task DequeueAsync_TokenYaCancelado_NoRetiraMensaje()
    {
        // Arrange
        var queue = new EmailQueueService();
        queue.Enqueue(new EmailMessage());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        Func<Task> act = async () => await queue.DequeueAsync(cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        queue.Count.Should().Be(1);
    }

    [Test]
    public void EmailMessage_SinAsignaciones_TieneValoresIniciales()
    {
        // Arrange / Act
        var email = new EmailMessage();

        // Assert
        email.To.Should().BeEmpty();
        email.Subject.Should().BeEmpty();
        email.Body.Should().BeEmpty();
        email.IsHtml.Should().BeTrue();
    }
}

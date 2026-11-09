using FluentAssertions;
using FunkoApi.Back.GraphQl.Queries;
using FunkoApi.Back.Repositories.Funko;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using NUnit.Framework;
using FunkoModel = FunkoApi.Back.Models.Funko;

namespace FunkoApi.Tests.GraphQl;

[TestFixture]
public class FunkoQueryTests
{
    private Mock<IFunkoRepository> _repositoryMock = null!;
    private Mock<ILogger<FunkoQuery>> _loggerMock = null!;
    private FunkoQuery _query = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IFunkoRepository>();
        _loggerMock = new Mock<ILogger<FunkoQuery>>();
        _query = new FunkoQuery(_loggerMock.Object);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetFunkos_ConOSinDatos_DevuelveColeccionDelRepositorio(bool conDatos)
    {
        // Arrange
        var funko = CrearFunko();
        var datos = conDatos ? new[] { funko } : Array.Empty<FunkoModel>();
        _repositoryMock.Setup(r => r.GetAll()).ReturnsAsync(datos);

        // Act
        var resultado = await _query.GetFunkos(_repositoryMock.Object);

        // Assert
        resultado.Should().Equal(datos);
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
        _repositoryMock.Verify(r => r.GetById(It.IsAny<long>()), Times.Never);
        VerifyLog(LogLevel.Information, 2);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetFunko_ExisteONo_ConservaResultadoDelRepositorio(bool existe)
    {
        // Arrange
        var funko = CrearFunko();
        _repositoryMock.Setup(r => r.GetById(1)).ReturnsAsync(existe ? funko : null);

        // Act
        var resultado = await _query.GetFunko(1, _repositoryMock.Object);

        // Assert
        if (existe) resultado.Should().BeSameAs(funko);
        else resultado.Should().BeNull();
        _repositoryMock.Verify(r => r.GetById(1), Times.Once);
        _repositoryMock.Verify(r => r.GetAll(), Times.Never);
        VerifyLog(LogLevel.Warning, existe ? 0 : 1);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Query_RepositorioFalla_PropagaMismaExcepcion(bool listar)
    {
        // Arrange
        var error = new InvalidOperationException("Error de lectura simulado");
        if (listar) _repositoryMock.Setup(r => r.GetAll()).ThrowsAsync(error);
        else _repositoryMock.Setup(r => r.GetById(1)).ThrowsAsync(error);

        // Act
        Func<Task> act = async () =>
        {
            if (listar) await _query.GetFunkos(_repositoryMock.Object);
            else await _query.GetFunko(1, _repositoryMock.Object);
        };

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(error);
        _repositoryMock.Verify(r => r.GetAll(), listar ? Times.Once() : Times.Never());
        _repositoryMock.Verify(r => r.GetById(1), listar ? Times.Never() : Times.Once());
    }

    private void VerifyLog(LogLevel level, int times)
    {
        _loggerMock.Verify(l => l.Log(level, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(times));
    }

    private static FunkoModel CrearFunko() => new(1, "Iron Man", 15m, 3, "imagen.png", "marvel00001", null,
        ObjectId.Parse("000000000000000000000001"), false, DateTime.UtcNow, DateTime.UtcNow);
}

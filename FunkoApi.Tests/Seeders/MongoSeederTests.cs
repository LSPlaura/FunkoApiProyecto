using FluentAssertions;
using FunkoApi.Back.Data.Seed.Mongo;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Seeders;

[TestFixture]
public class MongoSeederTests
{
    private Mock<IProveedorRepository> _repositoryMock = null!;
    private MongoSeeder _seeder = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IProveedorRepository>();
        _seeder = new MongoSeeder(_repositoryMock.Object, new Mock<ILogger<MongoSeeder>>().Object);
    }

    [Test]
    public async Task SeedAsync_SinProveedores_CreaLosTresProveedores()
    {
        // Arrange
        var creados = new List<Proveedor>();
        _repositoryMock.Setup(r => r.GetAll()).ReturnsAsync(Array.Empty<Proveedor>());
        _repositoryMock.Setup(r => r.Create(It.IsAny<Proveedor>()))
            .Callback<Proveedor>(p => creados.Add(p))
            .ReturnsAsync((Proveedor p) => p);

        // Act
        await _seeder.SeedAsync();

        // Assert
        creados.Select(p => p.Nombre).Should().Equal("Funko Corp", "Toy Master", "Geek Toys");
        creados.Should().OnlyContain(p => p.Id != ObjectId.Empty && !p.IsDeleted);
        creados.Select(p => p.Id).Should().OnlyHaveUniqueItems();
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.IsAny<Proveedor>()), Times.Exactly(3));
        _repositoryMock.Verify(r => r.Create(It.Is<Proveedor>(p => p.Nombre == "Funko Corp")), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.Is<Proveedor>(p => p.Nombre == "Toy Master")), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.Is<Proveedor>(p => p.Nombre == "Geek Toys")), Times.Once);
    }

    [Test]
    public async Task SeedAsync_ConProveedores_NoCreaDuplicados()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Proveedor(ObjectId.GenerateNewId(), "Existente", false, fecha, fecha);
        _repositoryMock.Setup(r => r.GetAll()).ReturnsAsync(new[] { proveedor });

        // Act
        await _seeder.SeedAsync();

        // Assert
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.IsAny<Proveedor>()), Times.Never);
    }

    [Test]
    public async Task SeedAsync_ErrorConsultando_PropagaExcepcion()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAll()).Throws(new InvalidOperationException("Error consultando"));
        Func<Task> accion = () => _seeder.SeedAsync();

        // Act
        var resultado = await accion.Should().ThrowAsync<InvalidOperationException>();

        // Assert
        resultado.Which.Message.Should().Be("Error consultando");
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.IsAny<Proveedor>()), Times.Never);
    }

    [Test]
    public async Task SeedAsync_ErrorCreando_PropagaExcepcionYNoContinua()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAll()).ReturnsAsync(Array.Empty<Proveedor>());
        _repositoryMock.Setup(r => r.Create(It.IsAny<Proveedor>()))
            .ThrowsAsync(new InvalidOperationException("Error creando"));
        Func<Task> accion = () => _seeder.SeedAsync();

        // Act
        var resultado = await accion.Should().ThrowAsync<InvalidOperationException>();

        // Assert
        resultado.Which.Message.Should().Be("Error creando");
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.IsAny<Proveedor>()), Times.Once);
    }
}

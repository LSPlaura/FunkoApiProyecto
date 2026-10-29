using FunkoApi.Back.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Data.Seed.MongoDriver;

[TestFixture]
public class MongoDbSeederTests
{
    [SetUp]
    public void SetUp()
    {
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockProveedoresCollection = new Mock<IMongoCollection<Proveedor>>();
        _mockCategoriasCollection = new Mock<IMongoCollection<Categoria>>();
        _mockFunkosCollection = new Mock<IMongoCollection<Funko>>();
        _mockLogger = new Mock<ILogger<MongoDbSeeder>>();

        _mockDatabase.Setup(db => db.GetCollection<Proveedor>("proveedores", null))
            .Returns(_mockProveedoresCollection.Object);
        _mockDatabase.Setup(db => db.GetCollection<Categoria>("categorias", null))
            .Returns(_mockCategoriasCollection.Object);
        _mockDatabase.Setup(db => db.GetCollection<Funko>("funkos", null)).Returns(_mockFunkosCollection.Object);

        _seeder = new MongoDbSeeder(_mockDatabase.Object, _mockLogger.Object);
    }

    private Mock<IMongoDatabase> _mockDatabase = null!;
    private Mock<IMongoCollection<Proveedor>> _mockProveedoresCollection = null!;
    private Mock<IMongoCollection<Categoria>> _mockCategoriasCollection = null!;
    private Mock<IMongoCollection<Funko>> _mockFunkosCollection = null!;
    private Mock<ILogger<MongoDbSeeder>> _mockLogger = null!;
    private MongoDbSeeder _seeder = null!;

    [TestFixture]
    public class CasosPositivos : MongoDbSeederTests
    {
        [Test]
        public async Task SeedAsync_ColeccionesVacias_InsertaDatosCorrectamente()
        {
            // Arrange
            _mockProveedoresCollection
                .Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<Proveedor>>(), It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _mockCategoriasCollection
                .Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<Categoria>>(), It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _mockFunkosCollection
                .Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<Funko>>(), It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            await _seeder.SeedAsync();

            // Assert
            _mockProveedoresCollection.Verify(
                c => c.InsertManyAsync(It.IsAny<IEnumerable<Proveedor>>(), It.IsAny<InsertManyOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            _mockCategoriasCollection.Verify(
                c => c.InsertManyAsync(It.IsAny<IEnumerable<Categoria>>(), It.IsAny<InsertManyOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
            _mockFunkosCollection.Verify(
                c => c.InsertManyAsync(It.IsAny<IEnumerable<Funko>>(), It.IsAny<InsertManyOptions>(),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SeedAsync_ColeccionesConDatos_NoInsertaNuevosDatos()
        {
            // Arrange
            _mockProveedoresCollection
                .Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<Proveedor>>(), It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            _mockCategoriasCollection
                .Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<Categoria>>(), It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            _mockFunkosCollection
                .Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<Funko>>(), It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            // Act
            await _seeder.SeedAsync();

            // Assert
            _mockProveedoresCollection.Verify(
                c => c.InsertManyAsync(It.IsAny<IEnumerable<Proveedor>>(), It.IsAny<InsertManyOptions>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _mockCategoriasCollection.Verify(
                c => c.InsertManyAsync(It.IsAny<IEnumerable<Categoria>>(), It.IsAny<InsertManyOptions>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _mockFunkosCollection.Verify(
                c => c.InsertManyAsync(It.IsAny<IEnumerable<Funko>>(), It.IsAny<InsertManyOptions>(),
                    It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [TestFixture]
    public class CasosNegativos : MongoDbSeederTests
    {
        [Test]
        public async Task SeedAsync_ConExcepcion_NoPropagaError()
        {
            // Arrange
            _mockProveedoresCollection
                .Setup(c => c.CountDocumentsAsync(It.IsAny<FilterDefinition<Proveedor>>(), It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act & Assert
            await _seeder.Invoking(s => s.SeedAsync())
                .Should().NotThrowAsync();
        }
    }
}
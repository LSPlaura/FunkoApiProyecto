using FluentAssertions;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using ProveedorModel = FunkoApi.Back.Models.Proveedor;

namespace FunkoApi.Tests.Repositories.Proveedor;

// Complementa los tests de integración: permite probar la respuesta no confirmada
// del driver sin cambiar la configuración de escritura de una base de datos real.
[TestFixture]
public class ProveedorMongoRepositoryUpdateTests
{
    private Mock<IMongoDatabase> _databaseMock = null!;
    private Mock<IMongoCollection<ProveedorModel>> _collectionMock = null!;
    private Mock<IMongoIndexManager<ProveedorModel>> _indexesMock = null!;
    private ProveedorMongoRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _databaseMock = new Mock<IMongoDatabase>();
        _collectionMock = new Mock<IMongoCollection<ProveedorModel>>();
        _indexesMock = new Mock<IMongoIndexManager<ProveedorModel>>();
        _collectionMock.SetupGet(c => c.Indexes).Returns(_indexesMock.Object);
        _databaseMock.Setup(d => d.GetCollection<ProveedorModel>("proveedores", null))
            .Returns(_collectionMock.Object);
        _repository = new ProveedorMongoRepository(_databaseMock.Object,
            new Mock<ILogger<ProveedorMongoRepository>>().Object);
    }

    [Test]
    public void Constructor_ColeccionDeProveedores_CreaIndiceDeNombreNoUnico() {
        // Arrange
        CreateIndexModel<ProveedorModel>? indice = null;
        _indexesMock.Setup(i => i.CreateOne(It.IsAny<CreateIndexModel<ProveedorModel>>(),
                It.IsAny<CreateOneIndexOptions>(), default))
            .Callback<CreateIndexModel<ProveedorModel>, CreateOneIndexOptions, CancellationToken>(
                (modelo, _, _) => indice = modelo)
            .Returns("idx_proveedor_nombre");
        _indexesMock.Invocations.Clear();
        _databaseMock.Invocations.Clear();

        // Act
        _ = new ProveedorMongoRepository(_databaseMock.Object,
            new Mock<ILogger<ProveedorMongoRepository>>().Object);

        // Assert
        indice.Should().NotBeNull();
        indice!.Options.Name.Should().Be("idx_proveedor_nombre");
        indice.Options.Unique.Should().NotBe(true);
        _indexesMock.Verify(i => i.CreateOne(It.IsAny<CreateIndexModel<ProveedorModel>>(),
            It.IsAny<CreateOneIndexOptions>(), default), Times.Once);
        _databaseMock.Verify(d => d.GetCollection<ProveedorModel>("proveedores", null), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Update_EscrituraConfirmadaONo_RetornaProveedorONull(bool confirmada)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new ProveedorModel(ObjectId.Parse("000000000000000000000001"),
            "Actualizado", false, fecha, fecha);
        var escrituraMock = new Mock<ReplaceOneResult>();
        escrituraMock.Setup(r => r.IsAcknowledged).Returns(confirmada);
        _collectionMock.Setup(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProveedorModel>>(),
            proveedor, It.IsAny<ReplaceOptions>(), default)).ReturnsAsync(escrituraMock.Object);

        // Act
        var resultado = await _repository.Update(proveedor);

        // Assert
        if (confirmada) resultado.Should().Be(proveedor);
        else resultado.Should().BeNull();
        _databaseMock.Verify(d => d.GetCollection<ProveedorModel>("proveedores", null), Times.Once);
        _collectionMock.Verify(c => c.ReplaceOneAsync(It.IsAny<FilterDefinition<ProveedorModel>>(),
            proveedor, It.IsAny<ReplaceOptions>(), default), Times.Once);
        escrituraMock.VerifyGet(r => r.IsAcknowledged, Times.Once);
    }
}

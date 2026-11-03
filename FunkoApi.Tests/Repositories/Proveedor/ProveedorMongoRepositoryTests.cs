using FluentAssertions;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using Testcontainers.MongoDb;

namespace FunkoApi.Tests.Repositories.Proveedor;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class ProveedorMongoRepositoryTests
{
    private MongoDbContainer _container = null!;
    private IMongoDatabase _database = null!;
    private ProveedorMongoRepository _repository = null!;
    private const string Id = "000000000000000000000001";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new MongoDbBuilder().WithImage("mongo:7").Build();
        await _container.StartAsync();
        _database = new MongoClient(_container.GetConnectionString()).GetDatabase("proveedores_test");
        _repository = new ProveedorMongoRepository(_database, new Mock<ILogger<ProveedorMongoRepository>>().Object);
    }

    [SetUp]
    public async Task SetUp() => await _database.GetCollection<Back.Models.Proveedor>("proveedores")
        .DeleteManyAsync(Builders<Back.Models.Proveedor>.Filter.Empty);

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _container.DisposeAsync();

    [Test]
    public async Task Create_ProveedorValido_PersisteProveedor()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var proveedor = new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);

        // Act
        var resultado = await _repository.Create(proveedor);

        // Assert
        resultado.Should().Be(proveedor);
        (await _repository.GetById(Id)).Should().Be(proveedor);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ConYSinDatos_RetornaProveedoresOrdenados(bool conDatos)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedorZeta = new Back.Models.Proveedor(ObjectId.Parse(Id), "Zeta", false, fecha, fecha);
        var proveedorAlpha = new Back.Models.Proveedor(ObjectId.Parse("000000000000000000000002"), "Alpha", false, fecha, fecha);
        if (conDatos)
        {
            await _repository.Create(proveedorZeta);
            await _repository.Create(proveedorAlpha);
        }

        // Act
        var resultado = (await _repository.GetAll()).ToList();

        // Assert
        if (conDatos) resultado.Select(p => p.Nombre).Should().Equal("Alpha", "Zeta");
        else resultado.Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetById_ExisteONoExiste_RetornaProveedorONull(bool existe)
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var proveedor = new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        if (existe) await _repository.Create(proveedor);

        // Act
        var resultado = await _repository.GetById(Id);

        // Assert
        if (existe) resultado.Should().Be(proveedor);
        else resultado.Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ExistsById_ExisteONoExiste_RetornaBooleano(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        if (existe) await _repository.Create(proveedor);

        // Act
        var resultado = await _repository.ExistsById(Id);

        // Assert
        resultado.Should().Be(existe);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SoftDelete_ExisteONoExiste_RetornaBooleano(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        if (existe) await _repository.Create(proveedor);

        // Act
        var resultado = await _repository.SoftDelete(Id);

        // Assert
        resultado.Should().Be(existe);
        if (existe) (await _repository.GetById(Id))!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task SoftDelete_YaEliminado_RetornaFalse()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko Corp", true, fecha, fecha);
        await _repository.Create(proveedor);

        // Act
        var resultado = await _repository.SoftDelete(Id);

        // Assert
        resultado.Should().BeFalse();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task HardDelete_ExisteONoExiste_RetornaBooleanoYEliminaRegistro(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        if (existe) await _repository.Create(proveedor);

        // Act
        var resultado = await _repository.HardDelete(proveedor);

        // Assert
        resultado.Should().Be(existe);
        (await _repository.GetAll()).Should().BeEmpty();
    }

    [Test]
    public async Task Update_ProveedorExistente_PersisteCambios()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var proveedor = new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        await _repository.Create(proveedor);
        var actualizado = proveedor with { Nombre = "Actualizado" };

        // Act
        var resultado = await _repository.Update(actualizado);

        // Assert
        resultado.Should().Be(actualizado);
        (await _repository.GetById(Id)).Should().Be(actualizado);
    }

    [Test]
    public async Task Update_Inexistente_NoInsertaProveedor()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);

        // Act
        var resultado = await _repository.Update(proveedor);

        // Assert
        // El Back comprueba IsAcknowledged, no MatchedCount: devuelve el objeto aunque no exista.
        resultado.Should().Be(proveedor);
        (await _repository.GetAll()).Should().BeEmpty();
    }

    [Test]
    public async Task Update_EliminadoLogicamente_NoModificaRegistro()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var proveedor = new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko Corp", true, fecha, fecha);
        await _repository.Create(proveedor);

        // Act
        await _repository.Update(proveedor with { Nombre = "Nuevo" });

        // Assert
        (await _repository.GetById(Id)).Should().Be(proveedor);
    }

    [TestCase("")]
    [TestCase("no-es-objectid")]
    public async Task GetById_IdInvalido_LanzaFormatException(string id)
    {
        // Arrange
        Func<Task> accion = () => _repository.GetById(id);

        // Act
        var resultado = accion.Should().ThrowAsync<FormatException>();

        // Assert
        await resultado;
    }
}

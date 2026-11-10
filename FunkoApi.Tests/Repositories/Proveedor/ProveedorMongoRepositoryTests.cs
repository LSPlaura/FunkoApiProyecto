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
    public async Task Constructor_IndiceExistente_NoLoDuplicaYPermiteNombresRepetidos() {
        // Arrange
        var collection = _database.GetCollection<Back.Models.Proveedor>("proveedores");
        var fecha = DateTime.UtcNow;
        await _repository.Create(new Back.Models.Proveedor(ObjectId.GenerateNewId(), "Funko", false, fecha, fecha));

        // Act
        var repository = new ProveedorMongoRepository(_database,
            new Mock<ILogger<ProveedorMongoRepository>>().Object);
        await repository.Create(new Back.Models.Proveedor(ObjectId.GenerateNewId(), "Funko", false, fecha, fecha));
        using var cursor = await collection.Indexes.ListAsync();
        var indices = await cursor.ToListAsync();

        // Assert
        var indice = indices.Should().ContainSingle(i => i["name"] == "idx_proveedor_nombre").Which;
        indice["key"].AsBsonDocument.Should().Equal(new BsonDocument("nombre", 1));
        indice.GetValue("unique", false).AsBoolean.Should().BeFalse();
        (await collection.CountDocumentsAsync(p => p.Nombre == "Funko")).Should().Be(2);
    }

    [TestCase(1, 2)]
    [TestCase(2, 2)]
    [TestCase(3, 2)]
    [TestCase(10, 2)]
    [TestCase(int.MaxValue, 1)]
    public async Task GetPaged_ConDatos_RetornaPaginaOrdenadaYTotal(int page, int size)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var nombres = new[] { "Zeta", "Alpha", "Gamma", "Beta", "Omega" };
        var proveedores = nombres.Select((nombre, i) => new Back.Models.Proveedor(
            ObjectId.Parse($"{i + 1:000000000000000000000000}"), nombre, i == 3, fecha, fecha));
        await _database.GetCollection<Back.Models.Proveedor>("proveedores").InsertManyAsync(proveedores);
        var esperados = nombres.OrderBy(n => n).Skip((page - 1) * size).Take(size).ToArray();

        // Act
        var resultado = await _repository.GetPaged(page, size);

        // Assert
        resultado.TotalItems.Should().Be(5);
        resultado.Items.Select(p => p.Nombre).Should().Equal(esperados);
    }

    [Test]
    public async Task GetPaged_NombresRepetidos_RespetaTamanoYTotal()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var idMayor = ObjectId.Parse("000000000000000000000002");
        await _repository.Create(new Back.Models.Proveedor(idMayor, "Funko", false, fecha, fecha));
        await _repository.Create(new Back.Models.Proveedor(ObjectId.Parse(Id), "Funko", false, fecha, fecha));

        // Act
        var resultado = await _repository.GetPaged(2, 1);

        // Assert
        resultado.TotalItems.Should().Be(2);
        resultado.Items.Should().ContainSingle().Which.Nombre.Should().Be("Funko");
    }

    [Test]
    public async Task GetPaged_SinDatos_RetornaPaginaVaciaYTotalCero()
    {
        // Arrange
        const int page = 1;

        // Act
        var resultado = await _repository.GetPaged(page, 10);

        // Assert
        resultado.Items.Should().BeEmpty();
        resultado.TotalItems.Should().Be(0);
    }

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

    [TestCase("", "GetById")]
    [TestCase("no-es-objectid", "GetById")]
    [TestCase("", "ExistsById")]
    [TestCase("no-es-objectid", "ExistsById")]
    [TestCase("", "SoftDelete")]
    [TestCase("no-es-objectid", "SoftDelete")]
    public async Task Operacion_IdInvalido_LanzaFormatException(string id, string operacion)
    {
        // Arrange
        Func<Task> accion = operacion switch
        {
            "GetById" => async () => await _repository.GetById(id),
            "ExistsById" => async () => await _repository.ExistsById(id),
            _ => async () => await _repository.SoftDelete(id)
        };

        // Act
        var resultado = accion.Should().ThrowAsync<FormatException>();

        // Assert
        await resultado;
    }
}

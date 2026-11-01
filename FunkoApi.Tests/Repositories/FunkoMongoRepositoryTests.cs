using MongoDB.Driver;
using FunkoApi.Back.Models;
using MongoDB.Bson;
using NUnit.Framework;
using Testcontainers.MongoDb;

namespace FunkoApi.Tests.Repositories;

/// <summary>
///     Tests de integración de FunkoRepositoryMongo contra MongoDB real
///     usando el driver nativo y un contenedor efímero.
/// </summary>
[TestFixture]
[Category("Integration")]
public class FunkoMongoRepositoryTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new MongoDbBuilder()
            .WithImage("mongo:7")
            .Build();
        await _container.StartAsync();
        _database = new MongoClient(_container.GetConnectionString()).GetDatabase("funkos_test");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        var collections = await _database.ListCollectionNamesAsync();
        while (await collections.MoveNextAsync())
            foreach (var name in collections.Current)
                await _database.DropCollectionAsync(name);
        _repository = new FunkoRepositoryMongo(_database);
    }

    private MongoDbContainer _container = null!;
    private IMongoDatabase _database = null!;
    private FunkoRepositoryMongo _repository = null!;

    [Test]
    public async Task Create_FunkoValido_SeGuardaEnMongo()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
        var funko = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);

        var created = await _repository.Create(funko);

        // Assert
        created.Should().Be(funko);
        (await _repository.GetAll()).Should().ContainSingle();
    }

    [Test]
    public async Task GetAll_ConDatos_RetornaFunkosOrdenadosPorNombre()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
        var funkoZeta = new Funko(1, "Zeta", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);
        var funkoAlpha = new Funko(2, "Alpha", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);
        await _repository.Create(funkoZeta);
        await _repository.Create(funkoAlpha);

        // Act
        var result = (await _repository.GetAll()).ToList();

        // Assert
        result.Select(f => f.Nombre).Should().ContainInOrder("Alpha", "Zeta");
    }

    [Test]
    public async Task GetById_Existente_RetornaFunko()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
        var funko = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);
        await _repository.Create(funko);

        // Act
        var result = await _repository.GetById(funko.Id);

        // Assert
        result.Should().Be(funko);
    }

    [Test]
    public async Task GetById_Inexistente_RetornaNull()
    {
        // Act
        var result = await _repository.GetById(999);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetByName_Existente_RetornaFunko()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
        var funko = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);
        await _repository.Create(funko);

        // Act
        var result = await _repository.GetByName(funko.Nombre);

        // Assert
        result.Should().Be(funko);
    }

    [Test]
    public async Task GetByName_Inexistente_RetornaNull()
    {
        // Act
        var result = await _repository.GetByName("Missing");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task Update_Existente_RetornaFunkoActualizado()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
        var funko = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);
        await _repository.Create(funko);
        var updated = funko with { Nombre = "Updated" };

        // Act
        var result = await _repository.Update(updated);

        // Assert
        result.Should().Be(updated);
        (await _repository.GetById(funko.Id))!.Nombre.Should().Be("Updated");
    }

    [Test]
    public async Task SoftDelete_Existente_RetornaTrueYMarcaElFunko()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
        var funko = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);
        await _repository.Create(funko);

        // Act
        var result = await _repository.SoftDelete(funko.Id);

        // Assert
        result.Should().BeTrue();
        (await _repository.GetById(funko.Id))!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task SoftDelete_Inexistente_RetornaFalse()
    {
        // Act
        var result = await _repository.SoftDelete(999);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HardDelete_Existente_RetornaTrueYEliminaElFunko()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
        var funko = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);
        await _repository.Create(funko);

        // Act
        var result = await _repository.HardDelete(funko);

        // Assert
        result.Should().BeTrue();
        (await _repository.GetAll()).Should().BeEmpty();
    }

    [Test]
    public async Task HardDelete_Inexistente_RetornaFalse()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
        var funko = new Funko(999, "Missing", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);

        // Act
        var result = await _repository.HardDelete(funko);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task ExitsName_Existente_RetornaTrue()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
        var funko = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedor.Id, proveedor, false, fecha, fecha);
        await _repository.Create(funko);

        // Act
        var result = await _repository.ExitsName("Iron Man");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task ExitsName_Inexistente_RetornaFalse()
    {
        // Act
        var result = await _repository.ExitsName("Missing");

        // Assert
        result.Should().BeFalse();
    }
}

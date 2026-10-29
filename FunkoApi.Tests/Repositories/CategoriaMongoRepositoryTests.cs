using FunkoApi.Back.Models;
using MongoDB.Driver;
using NUnit.Framework;
using Testcontainers.MongoDb;

namespace FunkoApi.Tests.Repositories;

/// <summary>
///     Tests de integración de CategoriaRepositoryMongo contra MongoDB real
///     usando el driver nativo y un contenedor efímero.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CategoriaMongoRepositoryTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new MongoDbBuilder()
            .WithImage("mongo:7")
            .Build();
        await _container.StartAsync();
        _database = new MongoClient(_container.GetConnectionString()).GetDatabase("categorias_test");
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
        _repository = new CategoriaRepositoryMongo(_database);
    }

    private MongoDbContainer _container = null!;
    private IMongoDatabase _database = null!;
    private CategoriaRepositoryMongo _repository = null!;

    [Test]
    public async Task GetAll_ConDatos_RetornaCategoriasOrdenadasPorNombre()
    {
        // Arrange
        var collection = _database.GetCollection<Categoria>("categorias");
        await collection.InsertManyAsync(new[] { TestData.Categoria(1, "Zeta"), TestData.Categoria(2, "Alpha") });
        var all = (await _repository.GetAll()).ToList();

        // Assert
        all.Select(x => x.Nombre).Should().ContainInOrder("Alpha", "Zeta");
    }

    [Test]
    public async Task GetById_Existente_RetornaCategoria()
    {
        // Arrange
        var collection = _database.GetCollection<Categoria>("categorias");
        var categoria = TestData.Categoria();
        await collection.InsertOneAsync(categoria);

        // Act
        var result = await _repository.GetById(1);

        // Assert
        result.Should().Be(categoria);
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
    public async Task GetByName_Existente_RetornaCategoria()
    {
        // Arrange
        var collection = _database.GetCollection<Categoria>("categorias");
        var categoria = TestData.Categoria();
        await collection.InsertOneAsync(categoria);

        // Act
        var result = await _repository.GetByName("Marvel");

        // Assert
        result.Should().Be(categoria);
    }

    [Test]
    public async Task GetByName_Inexistente_RetornaNull()
    {
        // Act
        var result = await _repository.GetByName("Missing");

        // Assert
        result.Should().BeNull();
    }
}
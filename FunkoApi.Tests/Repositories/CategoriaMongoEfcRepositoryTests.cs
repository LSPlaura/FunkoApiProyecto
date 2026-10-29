using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using NUnit.Framework;
using Testcontainers.MongoDb;

namespace FunkoApi.Tests.Repositories;

/// <summary>
///     Tests de integración de CategoriaRepositoryMongoEfc contra MongoDB
///     mediante Entity Framework Core y Testcontainers.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CategoriaMongoEfcRepositoryTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new MongoDbBuilder().WithImage("mongo:7").Build();
        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        await new MongoClient(_container.GetConnectionString())
            .DropDatabaseAsync("categorias_efc_test");

        _context = new AppDbContextMongo(new DbContextOptionsBuilder<AppDbContextMongo>()
            .UseMongoDB(_container.GetConnectionString(), "categorias_efc_test").Options);
        _context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        await _context.Database.EnsureCreatedAsync();
        _repository = new CategoriaRepositoryMongoEfc(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    private MongoDbContainer _container = null!;
    private AppDbContextMongo _context = null!;
    private CategoriaRepositoryMongoEfc _repository = null!;

    [Test]
    public async Task GetAll_ConDatos_RetornaCategoriasOrdenadasPorNombre()
    {
        // Arrange
        await _context.Categorias.AddRangeAsync(TestData.Categoria(1, "Zeta"), TestData.Categoria(2, "Alpha"));
        await _context.SaveChangesAsync();
        var all = (await _repository.GetAll()).ToList();

        // Assert
        all.Select(x => x.Nombre).Should().ContainInOrder("Alpha", "Zeta");
    }

    [Test]
    public async Task GetById_Existente_RetornaCategoria()
    {
        // Arrange
        await _context.Categorias.AddAsync(TestData.Categoria());
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetById(1);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Marvel");
    }

    [Test]
    public async Task GetById_Inexistente_LanzaExcepcion()
    {
        // Act
        var action = () => _repository.GetById(999);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task GetByName_Existente_RetornaCategoria()
    {
        // Arrange
        await _context.Categorias.AddAsync(TestData.Categoria());
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByName("Marvel");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Test]
    public async Task GetByName_Inexistente_LanzaExcepcion()
    {
        // Act
        var action = () => _repository.GetByName("Missing");

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
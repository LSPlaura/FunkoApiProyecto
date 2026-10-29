using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using NUnit.Framework;
using Testcontainers.MongoDb;

namespace FunkoApi.Tests.Repositories;

/// <summary>
///     Tests de integración de FunkoRepositoryMongoEfc contra MongoDB
///     mediante Entity Framework Core y Testcontainers.
/// </summary>
[TestFixture]
[Category("Integration")]
public class FunkoMongoEfcRepositoryTests
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
            .DropDatabaseAsync("funkos_efc_test");

        _context = new AppDbContextMongo(new DbContextOptionsBuilder<AppDbContextMongo>()
            .UseMongoDB(_container.GetConnectionString(), "funkos_efc_test").Options);
        _context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        await _context.Database.EnsureCreatedAsync();
        _repository = new FunkoRepositoryMongoEfc(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    private MongoDbContainer _container = null!;
    private AppDbContextMongo _context = null!;
    private FunkoRepositoryMongoEfc _repository = null!;

    [Test]
    public async Task Create_FunkoValido_SeGuardaEnMongo()
    {
        // Arrange
        var categoria = TestData.Categoria();
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        var funko = TestData.Funko(categoria: categoria);

        var created = await _repository.Create(funko);

        // Assert
        created.Should().Be(funko);
        (await _context.Funkos.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task GetAll_ConDatos_RetornaFunkosOrdenadosPorNombre()
    {
        // Arrange
        var categoria = TestData.Categoria();
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(TestData.Funko(1, "Zeta", categoria));
        await _repository.Create(TestData.Funko(2, "Alpha", categoria));

        // Act
        var result = (await _repository.GetAll()).ToList();

        // Assert
        result.Select(f => f.Nombre).Should().ContainInOrder("Alpha", "Zeta");
    }

    [Test]
    public async Task GetById_Existente_RetornaFunko()
    {
        // Arrange
        var categoria = TestData.Categoria();
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(TestData.Funko(1, "Iron Man", categoria));

        // Act
        var result = await _repository.GetById(1);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Iron Man");
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
    public async Task GetByName_Existente_RetornaFunko()
    {
        // Arrange
        var categoria = TestData.Categoria();
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(TestData.Funko(1, "Iron Man", categoria));

        // Act
        var result = await _repository.GetByName("Iron Man");

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

    [Test]
    public async Task Update_Existente_RetornaFunkoActualizado()
    {
        // Arrange
        var categoria = TestData.Categoria();
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        var funko = await _repository.Create(TestData.Funko(1, "Iron Man", categoria));
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.Update(funko with { Nombre = "Updated", Categoria = categoria });

        // Assert
        result.Nombre.Should().Be("Updated");
        (await _repository.GetById(1))!.Nombre.Should().Be("Updated");
    }

    [Test]
    public async Task SoftDelete_Existente_RetornaTrueYMarcaElFunko()
    {
        // Arrange
        var categoria = TestData.Categoria();
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(TestData.Funko(categoria: categoria));

        // Act
        var result = await _repository.SoftDelete(1);

        // Assert
        result.Should().BeTrue();
        (await _context.Funkos.AsNoTracking().SingleAsync()).IsDeleted.Should().BeTrue();
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
        var categoria = TestData.Categoria();
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();

        var funko = await _repository.Create(TestData.Funko(categoria: categoria));

        // Act
        var result = await _repository.HardDelete(funko);

        // Assert
        result.Should().BeTrue();
        (await _context.Funkos.CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task ExitsName_Existente_RetornaTrue()
    {
        // Arrange
        var categoria = TestData.Categoria();
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();

        await _repository.Create(TestData.Funko(categoria: categoria));

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
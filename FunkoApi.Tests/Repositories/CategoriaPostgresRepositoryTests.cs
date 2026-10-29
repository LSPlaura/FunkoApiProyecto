using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.Repositories.Get.Categoria;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace FunkoApi.Tests.Repositories;

/// <summary>
///     Tests de integración de CategoriaRepository contra PostgreSQL real
///     usando un contenedor efímero de Testcontainers.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CategoriaPostgresRepositoryTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("categorias_test").WithUsername("test").WithPassword("test").Build();
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
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString()).Options);
        await _context.Database.EnsureCreatedAsync();
        _repository = new CategoriaRepository(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    private PostgreSqlContainer _container = null!;
    private AppDbContext _context = null!;
    private CategoriaRepository _repository = null!;

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
        result.Should().Be(TestData.Categoria());
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
        result.Should().Be(TestData.Categoria());
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
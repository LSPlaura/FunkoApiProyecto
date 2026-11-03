using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.Infrastructure;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Categoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace FunkoApi.Tests.Repositories;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class CategoriaRepositoryTests
{
    private PostgreSqlContainer _container = null!;
    private AppDbContext _context = null!;
    private ICategoriaRepository _repository = null!;
    private ServiceProvider? _provider;
    private bool _databaseCreated;
    private const string Id = "cat00000001";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder().WithImage("postgres:17-alpine")
            .WithDatabase("categorias_test").WithUsername("test").WithPassword("test").Build();
        await _container.StartAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        _databaseCreated = false;
        _context = null!;
        _provider = null;
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString()).Options);
        await _context.Database.EnsureCreatedAsync();
        _databaseCreated = true;
        var servicios = new ServiceCollection();
        servicios.AddSingleton(_context);
        servicios.AddLogging();
        servicios.AddRepositories(new ConfigurationBuilder().Build());
        _provider = servicios.BuildServiceProvider();
        _repository = _provider.GetRequiredService<ICategoriaRepository>();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_context is null) return;
        try
        {
            if (_databaseCreated) await _context.Database.EnsureDeletedAsync();
        }
        finally
        {
            _provider?.Dispose();
            await _context.DisposeAsync();
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_container is not null) await _container.DisposeAsync();
    }

    [Test]
    public async Task Create_CategoriaValida_PersisteYActualizaFechaCreacion()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        var antes = DateTime.UtcNow;

        // Act
        var resultado = await _repository.Create(categoria);

        // Assert
        resultado.Id.Should().Be(Id);
        resultado.Nombre.Should().Be(categoria.Nombre);
        resultado.CreatedAt.Should().BeOnOrAfter(antes);
        (await _repository.GetById(Id)).Should().BeEquivalentTo(resultado,
            o => o.Using<DateTime>(c => c.Subject.Should().BeCloseTo(c.Expectation, TimeSpan.FromMilliseconds(1))).WhenTypeIs<DateTime>());
    }

    [Test]
    public async Task Create_SinId_GeneraIdentificador()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Anime", false, fecha, fecha);

        // Act
        var resultado = await _repository.Create(categoria);

        // Assert
        resultado.Id.Should().HaveLength(11);
        (await _repository.GetById(resultado.Id)).Should().NotBeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ConYSinDatos_RetornaCategoriasOrdenadas(bool conDatos)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoriaZeta = new Categoria("Zeta", false, fecha, fecha, Id);
        var categoriaAlpha = new Categoria("Alpha", false, fecha, fecha, "cat00000002");
        if (conDatos)
        {
            await _repository.Create(categoriaZeta);
            await _repository.Create(categoriaAlpha);
        }

        // Act
        var resultado = (await _repository.GetAll()).ToList();

        // Assert
        if (conDatos) resultado.Select(c => c.Nombre).Should().Equal("Alpha", "Zeta");
        else resultado.Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetById_ExisteONoExiste_RetornaCategoriaONull(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        if (existe) await _repository.Create(categoria);

        // Act
        var resultado = await _repository.GetById(Id);

        // Assert
        if (existe) resultado!.Nombre.Should().Be("Marvel");
        else resultado.Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetByName_ExisteONoExiste_RetornaCategoriaONull(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        if (existe) await _repository.Create(categoria);

        // Act
        var resultado = await _repository.GetByName("Marvel");

        // Assert
        if (existe) resultado!.Id.Should().Be(Id);
        else resultado.Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Update_ExisteONoExiste_RetornaCategoriaActualizadaONull(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        if (existe) await _repository.Create(categoria);
        _context.ChangeTracker.Clear();
        var actualizada = categoria with { Nombre = "Anime" };

        // Act
        var resultado = await _repository.Update(actualizada);

        // Assert
        if (existe)
        {
            resultado.Should().Be(actualizada);
            (await _repository.GetById(Id))!.Nombre.Should().Be("Anime");
        }
        else resultado.Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SoftDelete_ExisteONoExiste_RetornaBooleanoYConservaRegistro(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        if (existe) await _repository.Create(categoria);

        // Act
        var resultado = await _repository.SoftDelete(Id);

        // Assert
        resultado.Should().Be(existe);
        if (existe) (await _repository.GetById(Id))!.IsDeleted.Should().BeTrue();
        else (await _repository.GetAll()).Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task HardDelete_ExisteONoExiste_RetornaBooleanoYEliminaRegistro(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        if (existe) categoria = await _repository.Create(categoria);

        // Act
        var resultado = await _repository.HardDelete(categoria);

        // Assert
        resultado.Should().Be(existe);
        (await _repository.GetAll()).Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ExistsById_ExisteONoExiste_RetornaBooleano(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        if (existe) await _repository.Create(categoria);

        // Act
        var resultado = await _repository.ExistsById(Id);

        // Assert
        resultado.Should().Be(existe);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ExistsByName_ExisteONoExiste_RetornaBooleano(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        if (existe) await _repository.Create(categoria);

        // Act
        var resultado = await _repository.ExistsByName("Marvel");

        // Assert
        resultado.Should().Be(existe);
    }
}

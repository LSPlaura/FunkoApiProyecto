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
        await _context.Categorias.AddRangeAsync(nombres.Select((nombre, i) =>
            new Categoria(nombre, i == 3, fecha, fecha, $"cat{i:00000000}")));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var esperados = nombres.Where(n => n != "Beta").OrderBy(n => n).Skip((page - 1) * size).Take(size).ToArray();

        // Act
        var resultado = await _repository.GetPaged(page, size);

        // Assert
        resultado.TotalItems.Should().Be(4);
        resultado.Items.Select(c => c.Nombre).Should().Equal(esperados);
        _context.ChangeTracker.Entries().Should().BeEmpty();
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
        if (existe) (await _context.Categorias.IgnoreQueryFilters().AsNoTracking().SingleAsync()).IsDeleted.Should().BeTrue();
        (await _repository.GetById(Id)).Should().BeNull();
        (await _repository.GetAll()).Should().BeEmpty();
    }

    [Test]
    public async Task SoftDelete_YaEliminada_RetornaFalseYConservaFecha()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await _repository.Create(new Categoria("Marvel", true, fecha, fecha, Id));

        // Act
        var resultado = await _repository.SoftDelete(Id);

        // Assert
        resultado.Should().BeFalse();
        var persistida = await _context.Categorias.IgnoreQueryFilters().AsNoTracking().SingleAsync();
        persistida.IsDeleted.Should().BeTrue();
        persistida.UpdatedAt.Should().Be(fecha);
    }

    [Test]
    public async Task Consultas_CategoriaEliminada_LaOcultanPeroReservanNombre() {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", true, fecha, fecha, Id);
        await _repository.Create(categoria);
        _context.ChangeTracker.Clear();

        // Act
        var listado = await _repository.GetAll();
        var pagina = await _repository.GetPaged(1, 10);
        var porId = await _repository.GetById(Id);
        var porNombre = await _repository.GetByName(categoria.Nombre);
        var existe = await _repository.ExistsById(Id);
        var nombreReservado = await _repository.ExistsByName(categoria.Nombre);
        var actualizada = await _repository.Update(categoria with { Nombre = "Nuevo" });

        // Assert
        listado.Should().BeEmpty();
        pagina.Items.Should().BeEmpty();
        pagina.TotalItems.Should().Be(0);
        porId.Should().BeNull();
        porNombre.Should().BeNull();
        existe.Should().BeFalse();
        nombreReservado.Should().BeTrue();
        actualizada.Should().BeNull();
        (await _context.Categorias.IgnoreQueryFilters().AsNoTracking().SingleAsync()).Nombre.Should().Be("Marvel");
    }

    [Test]
    public async Task HardDelete_EliminadaSinTracking_EliminaRegistroFisico() {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = await _repository.Create(new Categoria("Marvel", true, fecha, fecha, Id));
        _context.ChangeTracker.Clear();

        // Act
        var resultado = await _repository.HardDelete(categoria);

        // Assert
        resultado.Should().BeTrue();
        (await _context.Categorias.IgnoreQueryFilters().CountAsync()).Should().Be(0);
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

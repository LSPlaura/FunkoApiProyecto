using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.Config;
using FunkoApi.Back.Data.Seed.Efc;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Moq;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace FunkoApi.Tests.Seeders;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class EfcSeederTests
{
    private PostgreSqlContainer _container = null!;
    private AppDbContext _context = null!;
    private Mock<IProveedorRepository> _proveedorRepositoryMock = null!;
    private EfcSeeder _seeder = null!;
    private bool _databaseCreated;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder().WithImage("postgres:17-alpine")
            .WithDatabase("seeder_test").WithUsername("test").WithPassword("test").Build();
        await _container.StartAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        _databaseCreated = false;
        _context = null!;
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString()).Options);
        await _context.Database.EnsureCreatedAsync();
        _databaseCreated = true;
        _proveedorRepositoryMock = new Mock<IProveedorRepository>();
        var storageOptions = Options.Create(new StorageSettings
        {
            UploadPath = "uploads", DefaultImage = "default.png",
            Folders = new Dictionary<string, string> { ["Funkos"] = "funkos" }
        });
        _seeder = new EfcSeeder(_context, new Mock<ILogger<EfcSeeder>>().Object, _proveedorRepositoryMock.Object, storageOptions);
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
            await _context.DisposeAsync();
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_container is not null) await _container.DisposeAsync();
    }

    [Test]
    public async Task SeedAsync_BaseVacia_InsertaCategoriasYFunkosConSusRelaciones()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedores = new[]
        {
            new Proveedor(ObjectId.GenerateNewId(), "Funko Corp", false, fecha, fecha),
            new Proveedor(ObjectId.GenerateNewId(), "Toy Master", false, fecha, fecha),
            new Proveedor(ObjectId.GenerateNewId(), "Geek Toys", false, fecha, fecha)
        };
        _proveedorRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(proveedores);

        // Act
        await _seeder.SeedAsync();

        // Assert
        var categorias = await _context.Categorias.AsNoTracking().ToListAsync();
        var funkos = await _context.Funkos.AsNoTracking().Include(f => f.Categoria).OrderBy(f => f.Id).ToListAsync();
        categorias.Select(c => c.Nombre).Should().BeEquivalentTo("Marvel", "Star Wars", "Anime");
        categorias.Should().OnlyContain(c => c.Id.Length == 11 && !c.IsDeleted);
        funkos.Select(f => f.Nombre).Should().Equal("Iron Man", "Darth Vader", "Goku Super Saiyan");
        funkos.Select(f => f.Categoria!.Nombre).Should().Equal("Marvel", "Star Wars", "Anime");
        funkos.Select(f => f.ProveedorId).Should().Equal(proveedores.Select(p => p.Id));
        _proveedorRepositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task SeedAsync_EjecutadoDosVeces_NoDuplicaDatos()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedores = new[]
        {
            new Proveedor(ObjectId.GenerateNewId(), "Funko Corp", false, fecha, fecha),
            new Proveedor(ObjectId.GenerateNewId(), "Toy Master", false, fecha, fecha),
            new Proveedor(ObjectId.GenerateNewId(), "Geek Toys", false, fecha, fecha)
        };
        _proveedorRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(proveedores);
        await _seeder.SeedAsync();
        var idsIniciales = await _context.Categorias.Select(c => c.Id).OrderBy(id => id).ToListAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        (await _context.Categorias.CountAsync()).Should().Be(3);
        (await _context.Funkos.CountAsync()).Should().Be(3);
        (await _context.Categorias.Select(c => c.Id).OrderBy(id => id).ToListAsync()).Should().Equal(idsIniciales);
        _proveedorRepositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [TestCase("Funko Corp", false)]
    [TestCase("Toy Master", false)]
    [TestCase("Geek Toys", false)]
    [TestCase("Funko Corp", true)]
    [TestCase("Toy Master", true)]
    [TestCase("Geek Toys", true)]
    public async Task SeedAsync_ProveedorAusenteOEliminado_NoInsertaFunkosNiPropagaError(string nombre, bool eliminado)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedores = new[]
        {
            new Proveedor(ObjectId.GenerateNewId(), "Funko Corp", false, fecha, fecha),
            new Proveedor(ObjectId.GenerateNewId(), "Toy Master", false, fecha, fecha),
            new Proveedor(ObjectId.GenerateNewId(), "Geek Toys", false, fecha, fecha)
        }.Where(p => eliminado || p.Nombre != nombre)
            .Select(p => p with { IsDeleted = eliminado && p.Nombre == nombre }).ToArray();
        _proveedorRepositoryMock.Setup(r => r.GetAll()).ReturnsAsync(proveedores);
        Func<Task> accion = () => _seeder.SeedAsync();

        // Act
        await accion.Should().NotThrowAsync();

        // Assert
        (await _context.Categorias.CountAsync()).Should().Be(3);
        (await _context.Funkos.CountAsync()).Should().Be(0);
        _proveedorRepositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task SeedAsync_ErrorEnRepositorio_NoPropagaError()
    {
        // Arrange
        _proveedorRepositoryMock.Setup(r => r.GetAll()).ThrowsAsync(new InvalidOperationException("Sin conexión"));
        Func<Task> accion = () => _seeder.SeedAsync();

        // Act
        await accion.Should().NotThrowAsync();

        // Assert
        (await _context.Funkos.CountAsync()).Should().Be(0);
        _proveedorRepositoryMock.Verify(r => r.GetAll(), Times.Once);
    }
}

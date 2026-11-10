using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Funko;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace FunkoApi.Tests.Repositories.Funko;

/// <summary>
/// Tests de integración de FunkoRepository contra PostgreSQL real
/// usando un contenedor efímero de Testcontainers.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class FunkoRepositoryTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("funkos_test").WithUsername("test").WithPassword("test").Build();
        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_container is not null) await _container.DisposeAsync();
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
        _repository = new FunkoRepository(_context, new Mock<ILogger<FunkoRepository>>().Object);
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

    private PostgreSqlContainer _container = null!;
    private AppDbContext _context = null!;
    private FunkoRepository _repository = null!;
    private bool _databaseCreated;

    [Test]
    public async Task Create_FunkoValido_SeGuardaEnPostgreSQL()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);

        // Act
        var created = await _repository.Create(funko);

        // Assert
        created.Should().Be(funko);
        (await _context.Funkos.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task Create_CategoriaExistenteSinTracking_NoDuplicaCategoria()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var categoriaSinTracking = await _context.Categorias.AsNoTracking()
            .FirstAsync(c => c.Id == categoria.Id);
        var funko = new Back.Models.Funko(0, "Iron Man", 14.99m, 10, "default.png",
            categoria.Id, categoriaSinTracking, ObjectId.Parse("000000000000000000000001"),
            false, fecha, fecha);

        // Act
        var resultado = await _repository.Create(funko);

        // Assert
        resultado.Id.Should().BeGreaterThan(0);
        resultado.CategoriaId.Should().Be(categoria.Id);
        resultado.Categoria.Should().NotBeNull();
        resultado.Categoria!.Nombre.Should().Be("Marvel");
        (await _context.Categorias.CountAsync()).Should().Be(1);
        (await _context.Funkos.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task GetAll_ConDatos_RetornaFunkosOrdenadosPorNombre()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedorId = ObjectId.Parse("000000000000000000000001");
        var funkoZeta = new Back.Models.Funko(1, "Zeta", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedorId, false, fecha, fecha);
        var funkoAlpha = new Back.Models.Funko(2, "Alpha", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedorId, false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(funkoZeta);
        await _repository.Create(funkoAlpha);

        // Act
        var result = (await _repository.GetAll()).ToList();

        // Assert
        result.Select(f => f.Nombre).Should().Equal("Alpha", "Zeta");
    }

    [Test]
    public async Task GetById_Existente_RetornaFunko()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(funko);

        // Act
        var result = await _repository.GetById(1);

        // Assert
        result.Should().NotBeNull();
        // La lectura materializa una entidad nueva desde PostgreSQL, incluido el constructor de EF.
        result.Should().NotBeSameAs(funko);
        result.Should().BeEquivalentTo(funko,
            o => o.Using<DateTime>(c => c.Subject.Should().BeCloseTo(c.Expectation,
                TimeSpan.FromMilliseconds(1))).WhenTypeIs<DateTime>());
    }

    [Test]
    public async Task GetById_Inexistente_RetornaNull()
    {
        // Arrange
        const long id = 999;

        // Act
        var resultado = await _repository.GetById(id);

        // Assert
        resultado.Should().BeNull();
    }

    [Test]
    public async Task GetByName_Existente_RetornaFunko()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(funko);

        // Act
        var result = await _repository.GetByName("Iron Man");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Test]
    public async Task GetByName_Inexistente_RetornaNull()
    {
        // Arrange
        const string nombre = "Missing";

        // Act
        var resultado = await _repository.GetByName(nombre);

        // Assert
        resultado.Should().BeNull();
    }

    [Test]
    public async Task Update_Existente_RetornaFunkoActualizado()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(funko);
        _context.ChangeTracker.Clear();
        var actualizado = funko with
        {
            Nombre = "Updated", Precio = 25m, Stock = 5, Imagen = "nueva.png",
            ProveedorId = ObjectId.Parse("000000000000000000000002")
        };

        // Act
        var result = await _repository.Update(actualizado);

        // Assert
        result.Should().NotBeNull();
        var persistido = await _repository.GetById(1);
        persistido.Should().NotBeNull();
        persistido!.Nombre.Should().Be(actualizado.Nombre);
        persistido.Precio.Should().Be(actualizado.Precio);
        persistido.Stock.Should().Be(actualizado.Stock);
        persistido.Imagen.Should().Be(actualizado.Imagen);
        persistido.ProveedorId.Should().Be(actualizado.ProveedorId);
        persistido.Categoria!.Id.Should().Be(categoria.Id);
        persistido.CreatedAt.Should().BeCloseTo(funko.CreatedAt, TimeSpan.FromMilliseconds(1));
        persistido.UpdatedAt.Should().BeOnOrAfter(persistido.CreatedAt);
    }

    [Test]
    public async Task SoftDelete_Existente_RetornaTrueYMarcaElFunko()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(funko);

        // Act
        var result = await _repository.SoftDelete(1);

        // Assert
        result.Should().BeTrue();
        (await _context.Funkos.AsNoTracking().SingleAsync()).IsDeleted.Should().BeTrue();
    }

    [Test]
    public async Task SoftDelete_Inexistente_RetornaFalse()
    {
        // Arrange
        const long id = 999;

        // Act
        var result = await _repository.SoftDelete(id);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HardDelete_Existente_RetornaTrueYEliminaElFunko()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();

        await _repository.Create(funko);

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
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();

        await _repository.Create(funko);

        // Act
        var result = await _repository.ExitsName("Iron Man");

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task ExitsName_Inexistente_RetornaFalse()
    {
        // Arrange
        const string nombre = "Missing";

        // Act
        var result = await _repository.ExitsName(nombre);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task GetAll_SinDatos_RetornaColeccionVacia()
    {
        // Arrange
        // La base de datos se crea vacía en SetUp.

        // Act
        var resultado = await _repository.GetAll();

        // Assert
        resultado.Should().BeEmpty();
    }

    [Test]
    public async Task Update_Inexistente_RetornaNull()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", "cat00000001", null,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);

        // Act
        var resultado = await _repository.Update(funko);

        // Assert
        resultado.Should().BeNull();
    }

    [Test]
    public async Task Update_EliminadoLogicamente_NoActualiza()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), true, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(funko);

        // Act
        var resultado = await _repository.Update(funko with { Nombre = "Nuevo" });

        // Assert
        resultado.Should().BeNull();
        (await _context.Funkos.AsNoTracking().SingleAsync()).Nombre.Should().Be(funko.Nombre);
    }

    [Test]
    public async Task GetByIdYGetByName_EliminadoLogicamente_NoRetornanFunko()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), true, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(funko);

        // Act
        var porId = await _repository.GetById(funko.Id);
        var porNombre = await _repository.GetByName(funko.Nombre);

        // Assert
        porId.Should().BeNull();
        porNombre.Should().BeNull();
    }

    [Test]
    public async Task SoftDelete_YaEliminado_RetornaFalse()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), true, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        await _repository.Create(funko);

        // Act
        var resultado = await _repository.SoftDelete(funko.Id);

        // Assert
        resultado.Should().BeFalse();
    }

    [Test]
    public async Task HardDelete_Inexistente_RetornaFalse()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", "cat00000001", null,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);

        // Act
        var resultado = await _repository.HardDelete(funko);

        // Assert
        resultado.Should().BeFalse();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ExistAnyFunkoCategory_ConYSinAsociados_RetornaBooleano(bool asociado)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        if (asociado) await _repository.Create(funko);

        // Act
        var resultado = await _repository.ExistAnyFunkoCategory(categoria.Id);

        // Assert
        resultado.Should().Be(asociado);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ExistAnyFunkoProveedor_ConYSinAsociados_RetornaBooleano(bool asociado)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
        var proveedorId = ObjectId.Parse("000000000000000000000001");
        var funko = new Back.Models.Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
            proveedorId, false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.SaveChangesAsync();
        if (asociado) await _repository.Create(funko);

        // Act
        var resultado = await _repository.ExistAnyFunkoProveedor(proveedorId.ToString());

        // Assert
        resultado.Should().Be(asociado);
    }
}

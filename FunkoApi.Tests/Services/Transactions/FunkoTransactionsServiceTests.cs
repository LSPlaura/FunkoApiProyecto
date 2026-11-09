using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Data;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Hub;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Repositories.Proveedor;
using FunkoApi.Back.Services.Email;
using FunkoApi.Back.Services.Funko;
using FunkoApi.Back.Services.Funko.Transactions;
using FunkoApi.Back.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Moq;
using NUnit.Framework;
using Testcontainers.PostgreSql;
using FunkoModel = FunkoApi.Back.Models.Funko;

namespace FunkoApi.Tests.Services.Transactions;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class FunkoTransactionsServiceTests
{
    private PostgreSqlContainer _container = null!;
    private AppDbContext _context = null!;
    private Mock<IStorageService> _storageMock = null!;
    private Mock<ICacheService> _cacheMock = null!;
    private FunkoTransactionsService _transactions = null!;
    private bool _databaseCreated;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder().WithImage("postgres:17-alpine")
            .WithDatabase("funko_transactions_test").WithUsername("test").WithPassword("test").Build();
        await _container.StartAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        _databaseCreated = false;
        _context = null!;
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_container.GetConnectionString()).Options);
        await _context.Database.EnsureCreatedAsync();
        _databaseCreated = true;
        _storageMock = new Mock<IStorageService>();
        _cacheMock = new Mock<ICacheService>();
        _storageMock.Setup(s => s.DeleteFileAsync(It.IsAny<string>())).ReturnsAsync(Result.Success<bool, DomainError>(true));
        _transactions = new FunkoTransactionsService(_context, "funko:", Options.Create(new CacheOptions()),
            _cacheMock.Object, _storageMock.Object, new Mock<ILogger<FunkoTransactionsService>>().Object);
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

    [TestCase("")]
    [TestCase("imagen.png")]
    public async Task Delete_Existente_ConfirmaBorradoYEliminaImagenSiExiste(string imagen)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "categoria01");
        var funko = new FunkoModel(1, "Iron Man", 15m, 1, imagen, categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.Categorias.AddAsync(categoria);
        await _context.Funkos.AddAsync(funko);
        await _context.SaveChangesAsync();

        // Act
        var resultado = await _transactions.Delete(funko);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
        (await _context.Funkos.AsNoTracking().AnyAsync()).Should().BeFalse();
        _storageMock.Verify(s => s.DeleteFileAsync(imagen), imagen == "" ? Times.Never() : Times.Once());
    }

    [Test]
    public async Task Delete_ErrorDeArchivo_RevierteBorrado()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "categoria01");
        var funko = new FunkoModel(1, "Iron Man", 15m, 1, "imagen.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.AddRangeAsync(categoria, funko);
        await _context.SaveChangesAsync();
        _storageMock.Setup(s => s.DeleteFileAsync(funko.Imagen)).ThrowsAsync(new IOException("No se puede borrar"));

        // Act
        var resultado = await _transactions.Delete(funko);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<BusinessRuleError>().Which.Message.Should().Contain("No se pudo eliminar el archivo");
        (await _context.Funkos.AsNoTracking().AnyAsync(f => f.Id == 1)).Should().BeTrue();
        _storageMock.Verify(s => s.DeleteFileAsync(funko.Imagen), Times.Once);
    }

    [Test]
    public async Task Delete_RegistroInexistente_RetornaErrorSinBorrarArchivo()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var funko = new FunkoModel(999, "Ausente", 15m, 1, "imagen.png", "categoria01", null,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);

        // Act
        var resultado = await _transactions.Delete(funko);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<BusinessRuleError>();
        _storageMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Update_ExitoOErrorDeDatos_ConfirmaORevierteYCompensaArchivo(bool falla)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "categoria01");
        var funko = new FunkoModel(1, "Iron Man", 15m, 1, "anterior.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.AddRangeAsync(categoria, funko);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var actualizado = funko with { Nombre = falla ? new string('A', 101) : "Actualizado", Imagen = "nueva.png" };

        // Act
        var resultado = await _transactions.Update(actualizado, "nueva.png", "anterior.png");

        // Assert
        resultado.IsSuccess.Should().Be(!falla);
        var persistido = await _context.Funkos.AsNoTracking().SingleAsync();
        persistido.Nombre.Should().Be(falla ? "Iron Man" : "Actualizado");
        persistido.Imagen.Should().Be(falla ? "anterior.png" : "nueva.png");
        _storageMock.Verify(s => s.DeleteFileAsync("anterior.png"), falla ? Times.Never() : Times.Once());
        _storageMock.Verify(s => s.DeleteFileAsync("nueva.png"), falla ? Times.Once() : Times.Never());
    }

    [TestCase(false, true)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public async Task HardDelete_Servicio_UsaTransaccionEInvalidaCacheSoloEnExito(bool enCache, bool exito)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "categoria01");
        var funko = new FunkoModel(1, "Iron Man", 15m, 1, "imagen.png", categoria.Id, categoria,
            ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);
        await _context.AddRangeAsync(categoria, funko);
        await _context.SaveChangesAsync();
        var repositoryMock = new Mock<IFunkoRepository>();
        repositoryMock.Setup(r => r.GetById(1)).ReturnsAsync(funko);
        _cacheMock.Setup(c => c.GetAsync<FunkoModel>("funko:1", default)).ReturnsAsync(enCache ? funko : null);
        if (!exito) _storageMock.Setup(s => s.DeleteFileAsync(funko.Imagen)).ThrowsAsync(new IOException("Bloqueado"));
        var emailQueue = new EmailQueueService();
        var service = new FunkoServices(repositoryMock.Object, new Mock<ICategoriaRepository>().Object,
            new Mock<IProveedorRepository>().Object, _cacheMock.Object, Options.Create(new CacheOptions()),
            _storageMock.Object, Options.Create(new StorageSettings()), _transactions,
            new Mock<IHubContext<FunkoHub>>().Object,
            emailQueue,
            new Mock<ILogger<FunkoServices>>().Object);

        // Act
        var resultado = await service.HardDelete(1);

        // Assert
        resultado.IsSuccess.Should().Be(exito);
        (await _context.Funkos.AsNoTracking().AnyAsync()).Should().Be(!exito);
        repositoryMock.Verify(r => r.GetById(1), enCache ? Times.Never() : Times.Once());
        _cacheMock.Verify(c => c.RemoveAsync("funko:1", default), exito ? Times.Once() : Times.Never());
        _storageMock.Verify(s => s.DeleteFileAsync(funko.Imagen), Times.Once);
        emailQueue.Count.Should().Be(exito ? 1 : 0);
        if (exito)
        {
            resultado.Value.Should().BeTrue();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var email = await emailQueue.DequeueAsync(cancellation.Token);
            email.Should().NotBeNull();
            email!.To.Should().Be("funkopopapi@gmail.com");
            email.Subject.Should().Be($"Funko eliminado definitivamente: {funko.Nombre}");
            email.Body.Should().Be($"Se ha realizado el borrado físico del Funko '{funko.Nombre}' con ID {funko.Id}.");
            email.IsHtml.Should().BeFalse();
            emailQueue.Count.Should().Be(0);
        }
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task Update_Servicio_MapeaCambiosYPropagaErrorTransaccional(bool enCache, bool falla)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "categoria01");
        var proveedorId = ObjectId.Parse("000000000000000000000001");
        var funko = new FunkoModel(1, "Iron Man", 15m, 1, "anterior.png", categoria.Id, categoria, proveedorId, false, fecha, fecha);
        await _context.AddRangeAsync(categoria, funko);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var repositoryMock = new Mock<IFunkoRepository>();
        var categoriaMock = new Mock<ICategoriaRepository>();
        var proveedorMock = new Mock<IProveedorRepository>();
        repositoryMock.Setup(r => r.GetById(1)).ReturnsAsync(funko);
        categoriaMock.Setup(r => r.ExistsById(categoria.Id)).ReturnsAsync(true);
        proveedorMock.Setup(r => r.ExistsById(proveedorId.ToString())).ReturnsAsync(true);
        _cacheMock.Setup(c => c.GetAsync<FunkoModel>("funko:1", default)).ReturnsAsync(enCache ? funko : null);
        var archivo = new Mock<IFormFile>().Object;
        _storageMock.Setup(s => s.IsEmpty(archivo)).Returns(false);
        _storageMock.Setup(s => s.SaveFileAsync(archivo, "funkos")).ReturnsAsync(Result.Success<string, DomainError>("nueva.png"));
        var storageOptions = Options.Create(new StorageSettings { Folders = new Dictionary<string, string> { ["Funkos"] = "funkos" } });
        var service = new FunkoServices(repositoryMock.Object, categoriaMock.Object, proveedorMock.Object,
            _cacheMock.Object, Options.Create(new CacheOptions()), _storageMock.Object, storageOptions, _transactions,
            new Mock<IHubContext<FunkoHub>>().Object,
            new EmailQueueService(),
            new Mock<ILogger<FunkoServices>>().Object);
        var dto = new UpdateFunkoDto { Nombre = falla ? new string('A', 101) : funko.Nombre, Precio = 20m,
            Stock = 5, Categoria = categoria.Id, Proveedor = proveedorId.ToString(), Imagen = archivo };

        // Act
        var resultado = await service.Update(dto, 1);

        // Assert
        resultado.IsSuccess.Should().Be(!falla);
        if (!falla)
        {
            resultado.Value.Precio.Should().Be(20m);
            resultado.Value.Stock.Should().Be(5);
            resultado.Value.Imagen.Should().Be("nueva.png");
        }
        else resultado.Error.Should().BeOfType<BusinessRuleError>();
        repositoryMock.Verify(r => r.GetById(1), enCache ? Times.Never() : Times.Once());
        repositoryMock.Verify(r => r.ExitsName(dto.Nombre), falla ? Times.Once() : Times.Never());
        _storageMock.Verify(s => s.SaveFileAsync(archivo, "funkos"), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("funko:1", default), falla ? Times.Never() : Times.Once());
    }
}

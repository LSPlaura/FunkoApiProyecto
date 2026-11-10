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

namespace FunkoApi.Tests.Services.Funko;

[TestFixture]
public class FunkoServicesTests
{
    private Mock<IFunkoRepository> _repositoryMock = null!;
    private Mock<ICategoriaRepository> _categoriaMock = null!;
    private Mock<IProveedorRepository> _proveedorMock = null!;
    private Mock<ICacheService> _cacheMock = null!;
    private Mock<IStorageService> _storageMock = null!;
    private Mock<IHubContext<FunkoHub>> _hubContextMock = null!;
    private Mock<IHubClients> _hubClientsMock = null!;
    private Mock<IClientProxy> _adminClientsMock = null!;
    private AppDbContext _context = null!;
    private FunkoServices _service = null!;
    private EmailQueueService _emailQueue = null!;
    private const string CategoriaId = "categoria01";
    private const string ProveedorId = "000000000000000000000001";

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IFunkoRepository>();
        _categoriaMock = new Mock<ICategoriaRepository>();
        _proveedorMock = new Mock<IProveedorRepository>();
        _cacheMock = new Mock<ICacheService>();
        _storageMock = new Mock<IStorageService>();
        _hubContextMock = new Mock<IHubContext<FunkoHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _adminClientsMock = new Mock<IClientProxy>();
        _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(c => c.Group("admins")).Returns(_adminClientsMock.Object);
        _adminClientsMock.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _storageMock.Setup(s => s.IsEmpty(It.IsAny<IFormFile?>())).Returns((IFormFile? f) => f is null || f.Length == 0);
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var cacheOptions = Options.Create(new CacheOptions());
        var storageOptions = Options.Create(new StorageSettings
        {
            DefaultImage = "default.png", Folders = new Dictionary<string, string> { ["Funkos"] = "funkos" }
        });
        var transactions = new FunkoTransactionsService(_context, "funko:", cacheOptions, _cacheMock.Object,
            _storageMock.Object, new Mock<ILogger<FunkoTransactionsService>>().Object);
        _emailQueue = new EmailQueueService();
        _service = new FunkoServices(_repositoryMock.Object, _categoriaMock.Object, _proveedorMock.Object,
            _cacheMock.Object, cacheOptions, _storageMock.Object, storageOptions, transactions, _hubContextMock.Object,
            _emailQueue,
            new Mock<ILogger<FunkoServices>>().Object);
        // Los caminos que ejecutan transacciones se prueban con PostgreSQL en Services/Transactions.
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [TestCase("duplicado")]
    [TestCase("categoria")]
    [TestCase("proveedor")]
    [TestCase("categoriaNula")]
    public async Task Create_ValidacionFalla_NoGuardaArchivoNiEntidad(string fallo)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, CategoriaId);
        var dto = new CreateFunkoDto { Nombre = "Iron Man", Precio = 15m, Stock = 1, Categoria = CategoriaId, Proveedor = ProveedorId };
        _repositoryMock.Setup(r => r.ExitsName(dto.Nombre)).ReturnsAsync(fallo == "duplicado");
        _categoriaMock.Setup(r => r.ExistsById(CategoriaId)).ReturnsAsync(fallo != "categoria");
        _proveedorMock.Setup(r => r.ExistsById(ProveedorId)).ReturnsAsync(fallo != "proveedor");
        _categoriaMock.Setup(r => r.GetById(CategoriaId)).ReturnsAsync(fallo == "categoriaNula" ? null : categoria);
        DomainError esperado = fallo switch
        {
            "duplicado" => FunkoError.FunkoAlreadyExists(dto.Nombre),
            "categoria" => CategoriaError.CategoriaBadRequest(CategoriaId),
            "proveedor" => ProveedorError.ProveedorBadRequest(ProveedorId),
            _ => CategoriaError.CategoriaNotFound(CategoriaId)
        };

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(esperado);
        _emailQueue.Count.Should().Be(0);
        _repositoryMock.Verify(r => r.ExitsName(dto.Nombre), Times.Once);
        _categoriaMock.Verify(r => r.ExistsById(CategoriaId), fallo == "duplicado" ? Times.Never() : Times.Once());
        _proveedorMock.Verify(r => r.ExistsById(ProveedorId), fallo is "duplicado" or "categoria" ? Times.Never() : Times.Once());
        _repositoryMock.Verify(r => r.Create(It.IsAny<Back.Models.Funko>()), Times.Never);
        _storageMock.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        _hubClientsMock.Verify(c => c.Group(It.IsAny<string>()), Times.Never);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task Create_ConOSinImagenYErrorDeRepositorio_CompensaSoloImagenNueva(bool conImagen, bool fallaRepositorio)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, CategoriaId);
        var archivoMock = new Mock<IFormFile>();
        archivoMock.Setup(f => f.Length).Returns(1);
        IFormFile? archivo = conImagen ? archivoMock.Object : null;
        var dto = new CreateFunkoDto { Nombre = "Iron Man", Precio = 15m, Stock = 1, Categoria = CategoriaId, Proveedor = ProveedorId, Imagen = archivo };
        var imagenEsperada = conImagen ? "nueva.png" : "/uploads/funkos/default.png";
        _categoriaMock.Setup(r => r.ExistsById(CategoriaId)).ReturnsAsync(true);
        _proveedorMock.Setup(r => r.ExistsById(ProveedorId)).ReturnsAsync(true);
        _categoriaMock.Setup(r => r.GetById(CategoriaId)).ReturnsAsync(categoria);
        _storageMock.Setup(s => s.GetRelativePath("default.png", "funkos")).Returns(imagenEsperada);
        _storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "funkos")).ReturnsAsync(Result.Success<string, DomainError>("nueva.png"));
        _storageMock.Setup(s => s.DeleteFileAsync("nueva.png")).ReturnsAsync(Result.Success<bool, DomainError>(true));
        if (fallaRepositorio) _repositoryMock.Setup(r => r.Create(It.IsAny<Back.Models.Funko>())).ThrowsAsync(new InvalidOperationException("Conflicto de datos"));
        else _repositoryMock.Setup(r => r.Create(It.IsAny<Back.Models.Funko>())).ReturnsAsync((Back.Models.Funko f) => f with { Id = 1 });

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.IsSuccess.Should().Be(!fallaRepositorio);
        if (fallaRepositorio) resultado.Error.Should().Be(new ConflictError("Conflicto de datos"));
        else
        {
            resultado.Value.Id.Should().Be(1);
            resultado.Value.Nombre.Should().Be(dto.Nombre);
            resultado.Value.Imagen.Should().Be(imagenEsperada);
            resultado.Value.Categoria.Id.Should().Be(CategoriaId);
        }
        _repositoryMock.Verify(r => r.Create(It.Is<Back.Models.Funko>(f => f.Id == 0 && f.Nombre == dto.Nombre
            && f.Precio == dto.Precio && f.Stock == dto.Stock && f.CategoriaId == CategoriaId
            && f.ProveedorId == ObjectId.Parse(ProveedorId) && f.Imagen == imagenEsperada && !f.IsDeleted)), Times.Once);
        _storageMock.Verify(s => s.IsEmpty(archivo), Times.Once);
        _storageMock.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "funkos"), conImagen ? Times.Once() : Times.Never());
        _storageMock.Verify(s => s.DeleteFileAsync("nueva.png"), conImagen && fallaRepositorio ? Times.Once() : Times.Never());
        _hubClientsMock.Verify(c => c.Group("admins"), fallaRepositorio ? Times.Never() : Times.Once());
        _adminClientsMock.Verify(c => c.SendCoreAsync("FunkoCreado", It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), fallaRepositorio ? Times.Never() : Times.Once());
        _emailQueue.Count.Should().Be(fallaRepositorio ? 0 : 1);
        if (!fallaRepositorio)
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var email = await _emailQueue.DequeueAsync(cancellation.Token);
            email.Should().NotBeNull();
            email!.To.Should().Be("funkopopapi@gmail.com");
            email.Subject.Should().Be($"Nuevo Funko: {dto.Nombre}");
            email.Body.Should().Be($"Se ha creado el Funko '{dto.Nombre}' con precio {dto.Precio} €.");
            email.IsHtml.Should().BeFalse();
            _emailQueue.Count.Should().Be(0);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Create_ErrorNotificando_ConservaExitoYNoBorraImagen(bool conImagen)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, CategoriaId);
        var archivoMock = new Mock<IFormFile>();
        archivoMock.Setup(f => f.Length).Returns(1);
        var dto = new CreateFunkoDto
        {
            Nombre = "Iron Man", Precio = 15m, Stock = 1,
            Categoria = CategoriaId, Proveedor = ProveedorId,
            Imagen = conImagen ? archivoMock.Object : null
        };
        _categoriaMock.Setup(r => r.ExistsById(CategoriaId)).ReturnsAsync(true);
        _categoriaMock.Setup(r => r.GetById(CategoriaId)).ReturnsAsync(categoria);
        _proveedorMock.Setup(r => r.ExistsById(ProveedorId)).ReturnsAsync(true);
        _storageMock.Setup(s => s.GetRelativePath("default.png", "funkos")).Returns("default.png");
        _storageMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "funkos"))
            .ReturnsAsync(Result.Success<string, DomainError>("nueva.png"));
        _repositoryMock.Setup(r => r.Create(It.IsAny<Back.Models.Funko>()))
            .ReturnsAsync((Back.Models.Funko f) => f with { Id = 1 });
        _adminClientsMock.Setup(c => c.SendCoreAsync("FunkoCreado", It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Error de SignalR"));

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(1);
        _emailQueue.Count.Should().Be(1);
        _repositoryMock.Verify(r => r.Create(It.IsAny<Back.Models.Funko>()), Times.Once);
        _hubClientsMock.Verify(c => c.Group("admins"), Times.Once);
        _adminClientsMock.Verify(c => c.SendCoreAsync("FunkoCreado", It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _storageMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        _storageMock.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "funkos"),
            conImagen ? Times.Once() : Times.Never());
    }

    [Test]
    public async Task Create_ErrorGuardandoImagen_NoCreaEntidad()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var archivo = new Mock<IFormFile>().Object;
        var dto = new CreateFunkoDto { Nombre = "Iron Man", Categoria = CategoriaId, Proveedor = ProveedorId, Imagen = archivo };
        _storageMock.Setup(s => s.IsEmpty(archivo)).Returns(false);
        _categoriaMock.Setup(r => r.ExistsById(CategoriaId)).ReturnsAsync(true);
        _proveedorMock.Setup(r => r.ExistsById(ProveedorId)).ReturnsAsync(true);
        _categoriaMock.Setup(r => r.GetById(CategoriaId)).ReturnsAsync(new Categoria("Marvel", false, fecha, fecha, CategoriaId));
        _storageMock.Setup(s => s.SaveFileAsync(archivo, "funkos")).ReturnsAsync(Result.Failure<string, DomainError>(StorageError.ErrorGuardando()));

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(StorageError.ErrorGuardando());
        _emailQueue.Count.Should().Be(0);
        _storageMock.Verify(s => s.SaveFileAsync(archivo, "funkos"), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.IsAny<Back.Models.Funko>()), Times.Never);
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public async Task GetByIdOGetByName_CacheEjecutaFactory_RetornaResultado(bool porId, bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, CategoriaId);
        var funko = new Back.Models.Funko(1, "Iron Man", 15m, 1, "imagen.png", CategoriaId, categoria, ObjectId.Parse(ProveedorId), false, fecha, fecha);
        var key = porId ? "funko:1" : "funko:Iron Man";
        _repositoryMock.Setup(r => r.GetById(1)).ReturnsAsync(existe ? funko : null);
        _repositoryMock.Setup(r => r.GetByName(funko.Nombre)).ReturnsAsync(existe ? funko : null);
        _cacheMock.Setup(c => c.GetOrSetAsync(key, It.IsAny<Func<Task<Back.Models.Funko?>>>(), TimeSpan.FromMinutes(30), default))
            .Returns((string k, Func<Task<Back.Models.Funko?>> factory, TimeSpan? expiration, CancellationToken ct) => factory());

        // Act
        var resultado = porId ? await _service.GetById(1) : await _service.GetByName(funko.Nombre);

        // Assert
        resultado.IsSuccess.Should().Be(existe);
        if (existe) resultado.Value.Nombre.Should().Be(funko.Nombre);
        else resultado.Error.Should().Be(porId ? FunkoError.FunkoNotFound(1) : FunkoError.FunkoNotFoundByName(funko.Nombre));
        _cacheMock.Verify(c => c.GetOrSetAsync(key, It.IsAny<Func<Task<Back.Models.Funko?>>>(), TimeSpan.FromMinutes(30), default), Times.Once);
        _repositoryMock.Verify(r => r.GetById(1), porId ? Times.Once() : Times.Never());
        _repositoryMock.Verify(r => r.GetByName(funko.Nombre), porId ? Times.Never() : Times.Once());
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetByIdOGetByName_EncontradoEnCache_NoConsultaRepositorio(bool porId)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, CategoriaId);
        var funko = new Back.Models.Funko(1, "Iron Man", 15m, 1, "imagen.png", CategoriaId,
            categoria, ObjectId.Parse(ProveedorId), false, fecha, fecha);
        var key = porId ? "funko:1" : "funko:Iron Man";
        _cacheMock.Setup(c => c.GetOrSetAsync(key, It.IsAny<Func<Task<Back.Models.Funko?>>>(),
            TimeSpan.FromMinutes(30), default)).ReturnsAsync(funko);

        // Act
        var resultado = porId ? await _service.GetById(1) : await _service.GetByName(funko.Nombre);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(funko.Id);
        resultado.Value.Categoria.Id.Should().Be(CategoriaId);
        _cacheMock.Verify(c => c.GetOrSetAsync(key, It.IsAny<Func<Task<Back.Models.Funko?>>>(),
            TimeSpan.FromMinutes(30), default), Times.Once);
        _repositoryMock.Verify(r => r.GetById(It.IsAny<long>()), Times.Never);
        _repositoryMock.Verify(r => r.GetByName(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Create_ArchivoVacio_UtilizaImagenPredeterminadaSinGuardarArchivo()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, CategoriaId);
        var archivoMock = new Mock<IFormFile>();
        archivoMock.Setup(f => f.Length).Returns(0);
        var dto = new CreateFunkoDto { Nombre = "Iron Man", Precio = 15m, Stock = 1,
            Categoria = CategoriaId, Proveedor = ProveedorId, Imagen = archivoMock.Object };
        _categoriaMock.Setup(r => r.ExistsById(CategoriaId)).ReturnsAsync(true);
        _categoriaMock.Setup(r => r.GetById(CategoriaId)).ReturnsAsync(categoria);
        _proveedorMock.Setup(r => r.ExistsById(ProveedorId)).ReturnsAsync(true);
        _storageMock.Setup(s => s.GetRelativePath("default.png", "funkos")).Returns("/uploads/funkos/default.png");
        _repositoryMock.Setup(r => r.Create(It.IsAny<Back.Models.Funko>()))
            .ReturnsAsync((Back.Models.Funko f) => f with { Id = 1 });

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Imagen.Should().Be("/uploads/funkos/default.png");
        _repositoryMock.Verify(r => r.ExitsName(dto.Nombre), Times.Once);
        _categoriaMock.Verify(r => r.ExistsById(CategoriaId), Times.Once);
        _categoriaMock.Verify(r => r.GetById(CategoriaId), Times.Once);
        _proveedorMock.Verify(r => r.ExistsById(ProveedorId), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.Is<Back.Models.Funko>(f => f.Imagen == resultado.Value.Imagen)), Times.Once);
        _storageMock.Verify(s => s.IsEmpty(archivoMock.Object), Times.Once);
        _storageMock.Verify(s => s.GetRelativePath("default.png", "funkos"), Times.Once);
        _storageMock.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        _storageMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        _adminClientsMock.Verify(c => c.SendCoreAsync("FunkoCreado", It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailQueue.Count.Should().Be(1);
    }

    [TestCase(true, true, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public async Task SoftDelete_DistintosEscenarios_InvalidaCacheSoloSiBorra(bool enCache, bool existe, bool borrado)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var funko = new Back.Models.Funko(1, "Iron Man", 15m, 1, "imagen.png", CategoriaId, null, ObjectId.Parse(ProveedorId), false, fecha, fecha);
        _cacheMock.Setup(c => c.GetAsync<Back.Models.Funko>("funko:1", default)).ReturnsAsync(enCache ? funko : null);
        _repositoryMock.Setup(r => r.GetById(1)).ReturnsAsync(existe ? funko : null);
        _repositoryMock.Setup(r => r.SoftDelete(1)).ReturnsAsync(borrado);
        var encontrado = enCache || existe;

        // Act
        var resultado = await _service.SoftDelete(1);

        // Assert
        resultado.IsSuccess.Should().Be(encontrado);
        if (encontrado) resultado.Value.Should().Be(borrado);
        else resultado.Error.Should().Be(FunkoError.FunkoNotFound(1));
        _repositoryMock.Verify(r => r.GetById(1), enCache ? Times.Never() : Times.Once());
        _repositoryMock.Verify(r => r.SoftDelete(1), encontrado ? Times.Once() : Times.Never());
        _cacheMock.Verify(c => c.RemoveAsync("funko:1", default), encontrado && borrado ? Times.Once() : Times.Never());
        _cacheMock.Verify(c => c.RemoveAsync("funko:Iron Man", default), encontrado && borrado ? Times.Once() : Times.Never());
        _emailQueue.Count.Should().Be(encontrado && borrado ? 1 : 0);
        if (encontrado && borrado)
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var email = await _emailQueue.DequeueAsync(cancellation.Token);
            email.Should().NotBeNull();
            email!.To.Should().Be("funkopopapi@gmail.com");
            email.Subject.Should().Be($"Funko desactivado: {funko.Nombre}");
            email.Body.Should().Be($"Se ha realizado el borrado lógico del Funko '{funko.Nombre}' con ID {funko.Id}.");
            email.IsHtml.Should().BeFalse();
            _emailQueue.Count.Should().Be(0);
        }
    }

    [Test]
    public async Task HardDelete_Inexistente_NoInvalidaCache()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetById(1)).ReturnsAsync((Back.Models.Funko?)null);

        // Act
        var resultado = await _service.HardDelete(1);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(FunkoError.FunkoNotFound(1));
        _emailQueue.Count.Should().Be(0);
        _repositoryMock.Verify(r => r.GetById(1), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Never);
    }

    [TestCase("inexistente")]
    [TestCase("proveedor")]
    [TestCase("categoria")]
    [TestCase("duplicado")]
    [TestCase("sinImagen")]
    [TestCase("errorImagen")]
    public async Task Update_ValidacionFalla_NoIniciaTransaccionNiInvalidaCache(string fallo)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var funko = new Back.Models.Funko(1, "Iron Man", 15m, 1, "anterior.png", CategoriaId, null, ObjectId.Parse(ProveedorId), false, fecha, fecha);
        var archivo = new Mock<IFormFile>().Object;
        var dto = new UpdateFunkoDto { Nombre = "Actualizado", Categoria = CategoriaId, Proveedor = ProveedorId, Imagen = archivo };
        _repositoryMock.Setup(r => r.GetById(1)).ReturnsAsync(fallo == "inexistente" ? null : funko);
        _proveedorMock.Setup(r => r.ExistsById(ProveedorId)).ReturnsAsync(fallo != "proveedor");
        _categoriaMock.Setup(r => r.ExistsById(CategoriaId)).ReturnsAsync(fallo != "categoria");
        _repositoryMock.Setup(r => r.ExitsName(dto.Nombre)).ReturnsAsync(fallo == "duplicado");
        _storageMock.Setup(s => s.IsEmpty(archivo)).Returns(fallo == "sinImagen");
        _storageMock.Setup(s => s.SaveFileAsync(archivo, "funkos")).ReturnsAsync(Result.Failure<string, DomainError>(StorageError.ErrorGuardando()));
        DomainError esperado = fallo switch
        {
            "inexistente" => FunkoError.FunkoNotFound(1),
            "proveedor" => ProveedorError.ProveedorBadRequest(ProveedorId),
            "categoria" => CategoriaError.CategoriaBadRequest(CategoriaId),
            "duplicado" => FunkoError.FunkoAlreadyExists(dto.Nombre),
            "sinImagen" => new BadRequestError("No se ha mandado la imagen"),
            _ => StorageError.ErrorGuardando()
        };

        // Act
        var resultado = await _service.Update(dto, 1);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(esperado);
        _repositoryMock.Verify(r => r.GetById(1), Times.Once);
        _storageMock.Verify(s => s.SaveFileAsync(archivo, "funkos"), fallo == "errorImagen" ? Times.Once() : Times.Never());
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Never);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task Update_MismoNombreEImagenAusente_NoCompruebaDuplicadoNiGuardaArchivo(bool enCache, bool archivoVacio)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var funko = new Back.Models.Funko(1, "Iron Man", 15m, 1, "anterior.png", CategoriaId,
            null, ObjectId.Parse(ProveedorId), false, fecha, fecha);
        var archivoMock = new Mock<IFormFile>();
        archivoMock.Setup(f => f.Length).Returns(0);
        var dto = new UpdateFunkoDto { Nombre = funko.Nombre, Categoria = CategoriaId, Proveedor = ProveedorId,
            Imagen = archivoVacio ? archivoMock.Object : null };
        _cacheMock.Setup(c => c.GetAsync<Back.Models.Funko>("funko:1", default)).ReturnsAsync(enCache ? funko : null);
        _repositoryMock.Setup(r => r.GetById(1)).ReturnsAsync(funko);
        _proveedorMock.Setup(r => r.ExistsById(ProveedorId)).ReturnsAsync(true);
        _categoriaMock.Setup(r => r.ExistsById(CategoriaId)).ReturnsAsync(true);

        // Act
        var resultado = await _service.Update(dto, 1);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(new BadRequestError("No se ha mandado la imagen"));
        _cacheMock.Verify(c => c.GetAsync<Back.Models.Funko>("funko:1", default), Times.Once);
        _repositoryMock.Verify(r => r.GetById(1), enCache ? Times.Never() : Times.Once());
        _proveedorMock.Verify(r => r.ExistsById(ProveedorId), Times.Once);
        _categoriaMock.Verify(r => r.ExistsById(CategoriaId), Times.Once);
        _repositoryMock.Verify(r => r.ExitsName(It.IsAny<string>()), Times.Never);
        _storageMock.Verify(s => s.IsEmpty(dto.Imagen), Times.Once);
        _storageMock.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        _storageMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Never);
        _emailQueue.Count.Should().Be(0);
    }

    [TestCase(1, 2, 4, 2)]
    [TestCase(2, 2, 4, 2)]
    [TestCase(0, 0, 4, 1)]
    [TestCase(1, 200, 4, 4)]
    [TestCase(10, 2, 4, 0)]
    [TestCase(1, 10, 0, 0)]
    public async Task GetPaged_ParametrosYDatos_NormalizaYPagina(int page, int size, int cantidad, int esperados)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, CategoriaId);
        var funkos = Enumerable.Range(1, cantidad).Select(i => new Back.Models.Funko(i, $"Funko {i}", 15m, 1,
            "imagen.png", CategoriaId, categoria, ObjectId.Parse(ProveedorId), false, fecha, fecha)).ToList();
        var pagina = Math.Max(page, 1);
        var tamano = Math.Clamp(size, 1, 100);
        var items = funkos.Skip((pagina - 1) * tamano).Take(tamano);
        _repositoryMock.Setup(r => r.GetPaged(pagina, tamano)).ReturnsAsync((items, cantidad));

        // Act
        var resultado = await _service.GetPaged(page, size);

        // Assert
        resultado.TotalItems.Should().Be(cantidad);
        resultado.TotalPages.Should().Be((int)Math.Ceiling((double)cantidad / Math.Clamp(size, 1, 100)));
        resultado.Items.Should().HaveCount(esperados);
        if (esperados > 0)
        {
            var primerId = (Math.Max(page, 1) - 1) * Math.Clamp(size, 1, 100) + 1;
            resultado.Items.Select(f => f.Id).Should().Equal(Enumerable.Range(primerId, esperados).Select(i => (long)i));
        }
        _repositoryMock.Verify(r => r.GetPaged(pagina, tamano), Times.Once);
        _repositoryMock.Verify(r => r.GetAll(), Times.Never);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ConYSinDatos_MapeaColeccionCompleta(bool conDatos)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, CategoriaId);
        var funko = new Back.Models.Funko(1, "Iron Man", 15m, 1, "imagen.png", CategoriaId, categoria,
            ObjectId.Parse(ProveedorId), false, fecha, fecha);
        var funkos = conDatos ? new[] { funko } : Array.Empty<Back.Models.Funko>();
        _repositoryMock.Setup(r => r.GetAll()).ReturnsAsync(funkos);

        // Act
        var resultado = (await _service.GetAll()).ToList();

        // Assert
        resultado.Should().HaveCount(funkos.Length);
        if (conDatos)
        {
            resultado[0].Id.Should().Be(funko.Id);
            resultado[0].Nombre.Should().Be(funko.Nombre);
            resultado[0].Categoria.Id.Should().Be(CategoriaId);
        }
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
    }
}

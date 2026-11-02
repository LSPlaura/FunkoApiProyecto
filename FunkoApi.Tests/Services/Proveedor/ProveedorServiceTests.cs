using FluentAssertions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Repositories.Proveedor;
using FunkoApi.Back.Services.Proveedor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MongoDB.Bson;
using NUnit.Framework;

namespace FunkoApi.Tests.Services;

[TestFixture]
public class ProveedorServiceTests
{
    private Mock<IProveedorRepository> _repositoryMock = null!;
    private Mock<IFunkoRepository> _funkoRepositoryMock = null!;
    private Mock<ICacheService> _cacheMock = null!;
    private ProveedorService _service = null!;
    private const string Id = "000000000000000000000001";
    private string Key => "proveedor:" + Id;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IProveedorRepository>();
        _funkoRepositoryMock = new Mock<IFunkoRepository>();
        _cacheMock = new Mock<ICacheService>();
        _service = new ProveedorService(_repositoryMock.Object, _cacheMock.Object,
            Options.Create(new CacheOptions()), _funkoRepositoryMock.Object,
            new Mock<ILogger<ProveedorService>>().Object);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ConYSinDatos_RetornaDtos(bool conDatos)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        var proveedores = conDatos ? new[] { proveedor } : Array.Empty<Proveedor>();
        _repositoryMock.Setup(r => r.GetAll()).ReturnsAsync(proveedores);

        // Act
        var resultado = await _service.GetAll();

        // Assert
        resultado.Should().BeEquivalentTo(proveedores.Select(p => p.ToDto()));
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task Create_DtoValido_DeberiaCrearProveedor()
    {
        // Arrange
        var dto = new CreateProveedorDto { Nombre = "Funko Corp" };
        var fecha = DateTime.UtcNow;
        var proveedor = new Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        _repositoryMock.Setup(r => r.Create(It.IsAny<Proveedor>())).ReturnsAsync(proveedor);

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be(proveedor.ToDto());
        _repositoryMock.Verify(r => r.Create(It.Is<Proveedor>(p => p.Nombre == dto.Nombre && !p.IsDeleted)), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetById_EnCache_RetornaResultadoSinConsultarRepositorio(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = existe ? new Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha) : null;
        _cacheMock.Setup(c => c.GetOrSetAsync(Key, It.IsAny<Func<Task<Proveedor?>>>(),
                TimeSpan.FromMinutes(30), default)).ReturnsAsync(proveedor);

        // Act
        var resultado = await _service.GetById(Id);

        // Assert
        resultado.IsSuccess.Should().Be(existe);
        if (existe) resultado.Value.Should().Be(proveedor!.ToDto());
        else resultado.Error.Should().Be(ProveedorError.ProveedoraNotFound(Id));
        _cacheMock.Verify(c => c.GetOrSetAsync(Key, It.IsAny<Func<Task<Proveedor?>>>(),
            TimeSpan.FromMinutes(30), default), Times.Once);
        _repositoryMock.Verify(r => r.GetById(Id), Times.Never);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetById_CacheEjecutaFactory_ConsultaRepositorio(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync(existe ? proveedor : null);
        _cacheMock.Setup(c => c.GetOrSetAsync(Key, It.IsAny<Func<Task<Proveedor?>>>(),
                TimeSpan.FromMinutes(30), default))
            .Returns((string key, Func<Task<Proveedor?>> factory, TimeSpan? expiration, CancellationToken ct) => factory());

        // Act
        var resultado = await _service.GetById(Id);

        // Assert
        resultado.IsSuccess.Should().Be(existe);
        if (existe) resultado.Value.Should().Be(proveedor.ToDto());
        else resultado.Error.Should().BeOfType<NotFoundError>();
        _repositoryMock.Verify(r => r.GetById(Id), Times.Once);
        _cacheMock.Verify(c => c.GetOrSetAsync(Key, It.IsAny<Func<Task<Proveedor?>>>(),
            TimeSpan.FromMinutes(30), default), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Update_ProveedorExistente_ActualizaEInvalidaCache(bool enCache)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        _cacheMock.Setup(c => c.GetAsync<Proveedor>(Key, default)).ReturnsAsync(enCache ? proveedor : null);
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync(proveedor);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Proveedor>())).ReturnsAsync((Proveedor p) => p);

        // Act
        var resultado = await _service.Update(new UpdateProveedorDto { Nombre = "Nuevo nombre" }, Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nombre.Should().Be("Nuevo nombre");
        resultado.Value.Id.Should().Be(Id);
        resultado.Value.CreatedAt.Should().Be(proveedor.CreatedAt);
        _cacheMock.Verify(c => c.GetAsync<Proveedor>(Key, default), Times.Once);
        _repositoryMock.Verify(r => r.GetById(Id), enCache ? Times.Never() : Times.Once());
        _repositoryMock.Verify(r => r.Update(It.Is<Proveedor>(p => p.Id == proveedor.Id && p.Nombre == "Nuevo nombre")), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(Key, default), Times.Once);
    }

    [Test]
    public async Task Update_ProveedorInexistente_NoActualiza()
    {
        // Arrange
        var dto = new UpdateProveedorDto { Nombre = "Nuevo" };

        // Act
        var resultado = await _service.Update(dto, Id);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ProveedorError.ProveedoraNotFound(Id));
        _cacheMock.Verify(c => c.GetAsync<Proveedor>(Key, default), Times.Once);
        _repositoryMock.Verify(r => r.GetById(Id), Times.Once);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Proveedor>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(Key, default), Times.Never);
    }

    [Test]
    public async Task Update_RepositorioRetornaNull_RetornaNotFoundEInvalidaCache()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync(proveedor);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Proveedor>())).ReturnsAsync((Proveedor?)null);

        // Act
        var resultado = await _service.Update(new UpdateProveedorDto { Nombre = "Nuevo" }, Id);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ProveedorError.ProveedoraNotFound(Id));
        _repositoryMock.Verify(r => r.GetById(Id), Times.Once);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Proveedor>()), Times.Once);
        _cacheMock.Verify(c => c.GetAsync<Proveedor>(Key, default), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(Key, default), Times.Once);
    }

    [TestCase(false, true, true, true)]
    [TestCase(false, false, false, true)]
    [TestCase(false, false, true, true)]
    [TestCase(false, false, true, false)]
    [TestCase(true, true, true, true)]
    [TestCase(true, false, false, true)]
    [TestCase(true, false, true, true)]
    [TestCase(true, false, true, false)]
    public async Task Delete_DistintosEscenarios_RetornaResultadoYRespetaDependencias(
        bool fisico, bool tieneFunkos, bool existe, bool eliminado)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new Proveedor(ObjectId.Parse(Id), "Funko Corp", false, fecha, fecha);
        _funkoRepositoryMock.Setup(r => r.ExistAnyFunkoProveedor(Id)).ReturnsAsync(tieneFunkos);
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync(existe ? proveedor : null);
        _repositoryMock.Setup(r => r.SoftDelete(Id)).ReturnsAsync(eliminado);
        _repositoryMock.Setup(r => r.HardDelete(proveedor)).ReturnsAsync(eliminado);
        var intentaBorrar = !tieneFunkos && existe;

        // Act
        var resultado = fisico ? await _service.HardDelete(Id) : await _service.SoftDelete(Id);

        // Assert
        resultado.IsSuccess.Should().Be(intentaBorrar);
        if (intentaBorrar) resultado.Value.Should().Be(eliminado);
        else resultado.Error.Should().Be(tieneFunkos ? (DomainError)ConflictError.Dependency("proveedor", Id)
            : ProveedorError.ProveedoraNotFound(Id));
        _funkoRepositoryMock.Verify(r => r.ExistAnyFunkoProveedor(Id), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(Key, default), tieneFunkos ? Times.Never() : Times.Once());
        _repositoryMock.Verify(r => r.GetById(Id), tieneFunkos ? Times.Never() : Times.Once());
        _repositoryMock.Verify(r => r.SoftDelete(Id), !fisico && intentaBorrar ? Times.Once() : Times.Never());
        _repositoryMock.Verify(r => r.HardDelete(proveedor), fisico && intentaBorrar ? Times.Once() : Times.Never());
    }
}

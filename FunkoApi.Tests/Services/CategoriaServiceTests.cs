using FluentAssertions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Services.Categoria;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Services;

[TestFixture]
public class CategoriaServiceTests
{
    private Mock<ICategoriaRepository> _repositoryMock = null!;
    private Mock<IFunkoRepository> _funkoRepositoryMock = null!;
    private Mock<ICacheService> _cacheMock = null!;
    private CategoriaService _service = null!;
    private const string Id = "cat00000001";
    private const string Key = "categoria:" + Id;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<ICategoriaRepository>();
        _funkoRepositoryMock = new Mock<IFunkoRepository>();
        _cacheMock = new Mock<ICacheService>();
        _service = new CategoriaService(_repositoryMock.Object, _funkoRepositoryMock.Object, _cacheMock.Object);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ConYSinCategorias_RetornaDtos(bool conDatos)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        var categorias = conDatos ? new[] { categoria } : Array.Empty<Categoria>();
        _repositoryMock.Setup(r => r.GetAll()).ReturnsAsync(categorias);

        // Act
        var resultado = await _service.GetAll();

        // Assert
        resultado.Should().BeEquivalentTo(categorias.Select(c => c.ToDto()));
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task GetById_EnCache_NoConsultaRepositorio()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var dto = new CategoriaDto(Id, "Marvel", false, fecha, fecha);
        _cacheMock.Setup(c => c.GetAsync<CategoriaDto>(Key, default)).ReturnsAsync(dto);

        // Act
        var resultado = await _service.GetById(Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be(dto);
        _cacheMock.Verify(c => c.GetAsync<CategoriaDto>(Key, default), Times.Once);
        _repositoryMock.Verify(r => r.GetById(It.IsAny<string>()), Times.Never);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<CategoriaDto>(), null, default), Times.Never);
    }

    [Test]
    public async Task GetById_FueraDeCache_GuardaYRetornaDto()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync(categoria);

        // Act
        var resultado = await _service.GetById(Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be(categoria.ToDto());
        _cacheMock.Verify(c => c.GetAsync<CategoriaDto>(Key, default), Times.Once);
        _repositoryMock.Verify(r => r.GetById(Id), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(Key, categoria.ToDto(), null, default), Times.Once);
    }

    [Test]
    public async Task GetById_Inexistente_RetornaNotFound()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync((Categoria?)null);

        // Act
        var resultado = await _service.GetById(Id);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CategoriaError.CategoriaNotFound(Id));
        _cacheMock.Verify(c => c.GetAsync<CategoriaDto>(Key, default), Times.Once);
        _repositoryMock.Verify(r => r.GetById(Id), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<CategoriaDto>(), null, default), Times.Never);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetByName_ExisteONoExiste_RetornaResultadoCorrespondiente(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        _repositoryMock.Setup(r => r.GetByName("Marvel")).ReturnsAsync(existe ? categoria : null);

        // Act
        var resultado = await _service.GetByName("Marvel");

        // Assert
        resultado.IsSuccess.Should().Be(existe);
        if (existe) resultado.Value.Should().Be(categoria.ToDto());
        else resultado.Error.Should().Be(CategoriaError.CategoriaNotFoundName("Marvel"));
        _repositoryMock.Verify(r => r.GetByName("Marvel"), Times.Once);
    }

    [Test]
    public async Task Create_NombreDisponible_CreaCategoria()
    {
        // Arrange
        var dto = new CreateCategoriaDto { Nombre = "Marvel" };
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        _repositoryMock.Setup(r => r.Create(It.IsAny<Categoria>())).ReturnsAsync(categoria);

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be(categoria.ToDto());
        _repositoryMock.Verify(r => r.ExistsByName(dto.Nombre), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.Is<Categoria>(c => c.Nombre == dto.Nombre && !c.IsDeleted)), Times.Once);
    }

    [Test]
    public async Task Create_NombreDuplicado_NoCreaCategoria()
    {
        // Arrange
        var dto = new CreateCategoriaDto { Nombre = "Marvel" };
        _repositoryMock.Setup(r => r.ExistsByName(dto.Nombre)).ReturnsAsync(true);

        // Act
        var resultado = await _service.Create(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CategoriaError.NombreDuplicado(dto.Nombre));
        _repositoryMock.Verify(r => r.ExistsByName(dto.Nombre), Times.Once);
        _repositoryMock.Verify(r => r.Create(It.IsAny<Categoria>()), Times.Never);
    }

    [TestCase("Marvel")]
    [TestCase("Anime")]
    public async Task Update_NombreDisponible_ActualizaEInvalidaCache(string nombre)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync(categoria);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Categoria>())).ReturnsAsync((Categoria c) => c);

        // Act
        var resultado = await _service.Update(new UpdateCategoriaDto { Nombre = nombre }, Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nombre.Should().Be(nombre);
        resultado.Value.CreatedAt.Should().Be(categoria.CreatedAt);
        resultado.Value.UpdatedAt.Should().BeOnOrAfter(categoria.UpdatedAt);
        _repositoryMock.Verify(r => r.GetById(Id), Times.Once);
        _repositoryMock.Verify(r => r.ExistsByName(nombre), nombre == categoria.Nombre ? Times.Never() : Times.Once());
        _repositoryMock.Verify(r => r.Update(It.Is<Categoria>(c => c.Id == Id && c.Nombre == nombre)), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(Key, default), Times.Once);
    }

    [Test]
    public async Task Update_CategoriaInexistente_NoActualiza()
    {
        // Arrange
        var dto = new UpdateCategoriaDto { Nombre = "Anime" };

        // Act
        var resultado = await _service.Update(dto, Id);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CategoriaError.CategoriaNotFound(Id));
        _repositoryMock.Verify(r => r.GetById(Id), Times.Once);
        _repositoryMock.Verify(r => r.ExistsByName(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Categoria>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(Key, default), Times.Never);
    }

    [Test]
    public async Task Update_NombreDuplicado_NoActualiza()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync(categoria);
        _repositoryMock.Setup(r => r.ExistsByName("Anime")).ReturnsAsync(true);

        // Act
        var resultado = await _service.Update(new UpdateCategoriaDto { Nombre = "Anime" }, Id);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CategoriaError.NombreDuplicado("Anime"));
        _repositoryMock.Verify(r => r.GetById(Id), Times.Once);
        _repositoryMock.Verify(r => r.ExistsByName("Anime"), Times.Once);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Categoria>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(Key, default), Times.Never);
    }

    [Test]
    public async Task Update_RepositorioRetornaNull_RetornaNotFound()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync(categoria);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Categoria>())).ReturnsAsync((Categoria?)null);

        // Act
        var resultado = await _service.Update(new UpdateCategoriaDto { Nombre = "Marvel" }, Id);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(CategoriaError.CategoriaNotFound(Id));
        _repositoryMock.Verify(r => r.GetById(Id), Times.Once);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Categoria>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(Key, default), Times.Never);
    }

    [TestCase(false, true, true, true)]
    [TestCase(false, false, true, true)]
    [TestCase(false, false, false, true)]
    [TestCase(false, false, true, false)]
    [TestCase(true, true, true, true)]
    [TestCase(true, false, true, true)]
    [TestCase(true, false, false, true)]
    [TestCase(true, false, true, false)]
    public async Task Delete_DistintosEscenarios_RetornaResultadoYRespetaDependencias(
        bool fisico, bool tieneFunkos, bool existe, bool eliminado)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, Id);
        _funkoRepositoryMock.Setup(r => r.ExistAnyFunkoCategory(Id)).ReturnsAsync(tieneFunkos);
        _repositoryMock.Setup(r => r.ExistsById(Id)).ReturnsAsync(existe);
        _repositoryMock.Setup(r => r.GetById(Id)).ReturnsAsync(categoria);
        _repositoryMock.Setup(r => r.SoftDelete(Id)).ReturnsAsync(eliminado);
        _repositoryMock.Setup(r => r.HardDelete(categoria)).ReturnsAsync(eliminado);
        var intentaBorrar = !tieneFunkos && existe;
        var exito = intentaBorrar && eliminado;

        // Act
        var resultado = fisico ? await _service.HardDelete(Id) : await _service.SoftDelete(Id);

        // Assert
        resultado.IsSuccess.Should().Be(exito);
        if (exito) resultado.Value.Should().BeTrue();
        else resultado.Error.Should().Be(tieneFunkos ? CategoriaError.CategoriaCanNotDelete(Id)
            : !existe ? (DomainError)CategoriaError.CategoriaNotFound(Id) : CategoriaError.CategoriaBadRequest(Id));
        _funkoRepositoryMock.Verify(r => r.ExistAnyFunkoCategory(Id), Times.Once);
        _repositoryMock.Verify(r => r.ExistsById(Id), tieneFunkos ? Times.Never() : Times.Once());
        _repositoryMock.Verify(r => r.GetById(Id), fisico && intentaBorrar ? Times.Once() : Times.Never());
        _repositoryMock.Verify(r => r.SoftDelete(Id), !fisico && intentaBorrar ? Times.Once() : Times.Never());
        _repositoryMock.Verify(r => r.HardDelete(categoria), fisico && intentaBorrar ? Times.Once() : Times.Never());
        _cacheMock.Verify(c => c.RemoveAsync(Key, default), exito ? Times.Once() : Times.Never());
    }
}

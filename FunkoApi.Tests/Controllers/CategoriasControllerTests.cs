using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Back.Controllers;
using FunkoApi.Back.Dto.Categoria;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Categoria;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Controllers;

[TestFixture]
public class CategoriasControllerTests
{
    private Mock<ICategoriaService> _serviceMock = null!;
    private CategoriasController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<ICategoriaService>();
        _controller = new CategoriasController(_serviceMock.Object);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ConYSinDatos_RetornaOkConLista(bool conDatos)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categorias = conDatos ? new[] { new CategoriaDto("categoria01", "Marvel", false, fecha, fecha) } : Array.Empty<CategoriaDto>();
        _serviceMock.Setup(s => s.GetAll()).ReturnsAsync(categorias);

        // Act
        var resultado = await _controller.GetAll();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(categorias);
        _serviceMock.Verify(s => s.GetAll(), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetById_ExisteONoExiste_RetornaOkONotFound(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        _serviceMock.Setup(s => s.GetById(categoria.Id)).ReturnsAsync(existe
            ? Result.Success<CategoriaDto, DomainError>(categoria)
            : Result.Failure<CategoriaDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.GetById(categoria.Id);

        // Assert
        var respuesta = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(existe ? 200 : 404);
        respuesta.Value.Should().BeEquivalentTo(existe ? (object)categoria : new { message = "No existe" });
        _serviceMock.Verify(s => s.GetById(categoria.Id), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetByName_ExisteONoExiste_RetornaOkONotFound(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        _serviceMock.Setup(s => s.GetByName(categoria.Nombre)).ReturnsAsync(existe
            ? Result.Success<CategoriaDto, DomainError>(categoria)
            : Result.Failure<CategoriaDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.GetByName(categoria.Nombre);

        // Assert
        var respuesta = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(existe ? 200 : 404);
        respuesta.Value.Should().BeEquivalentTo(existe ? (object)categoria : new { message = "No existe" });
        _serviceMock.Verify(s => s.GetByName(categoria.Nombre), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Post_ExitoOConflicto_RetornaCreatedOConflict(bool exito)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var dto = new CreateCategoriaDto { Nombre = "Marvel" };
        var categoria = new CategoriaDto("categoria01", dto.Nombre, false, fecha, fecha);
        _serviceMock.Setup(s => s.Create(dto)).ReturnsAsync(exito
            ? Result.Success<CategoriaDto, DomainError>(categoria)
            : Result.Failure<CategoriaDto, DomainError>(new ConflictError("Duplicada")));

        // Act
        var resultado = await _controller.Post(dto);

        // Assert
        if (exito)
        {
            var respuesta = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            respuesta.ActionName.Should().Be(nameof(CategoriasController.GetById));
            respuesta.RouteValues!["id"].Should().Be(categoria.Id);
            respuesta.Value.Should().Be(categoria);
        }
        else resultado.Result.Should().BeOfType<ConflictObjectResult>();
        _serviceMock.Verify(s => s.Create(dto), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Update_ExitoOError_RetornaOkONotFound(bool exito)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var dto = new UpdateCategoriaDto { Nombre = "Anime" };
        var categoria = new CategoriaDto("categoria01", dto.Nombre, false, fecha, fecha);
        _serviceMock.Setup(s => s.Update(dto, categoria.Id)).ReturnsAsync(exito
            ? Result.Success<CategoriaDto, DomainError>(categoria)
            : Result.Failure<CategoriaDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.Update(dto, categoria.Id);

        // Assert
        var respuesta = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(exito ? 200 : 404);
        respuesta.Value.Should().BeEquivalentTo(exito ? (object)categoria : new { message = "No existe" });
        _serviceMock.Verify(s => s.Update(dto, categoria.Id), Times.Once);
    }

    [Test]
    public async Task Update_NombreDuplicado_Retorna409ConMensaje()
    {
        // Arrange
        var dto = new UpdateCategoriaDto { Nombre = "Marvel" };
        var error = CategoriaError.NombreDuplicado(dto.Nombre);
        _serviceMock.Setup(s => s.Update(dto, "categoria01"))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(error));

        // Act
        var resultado = await _controller.Update(dto, "categoria01");

        // Assert
        var respuesta = resultado.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        respuesta.Value.Should().BeEquivalentTo(new { message = error.Message });
        _serviceMock.Verify(s => s.Update(dto, "categoria01"), Times.Once);
    }

    [Test]
    public async Task Delete_CategoriaInexistente_Retorna404ConMensaje()
    {
        // Arrange
        var error = CategoriaError.CategoriaNotFound("categoria01");
        _serviceMock.Setup(s => s.SoftDelete("categoria01"))
            .ReturnsAsync(Result.Failure<bool, DomainError>(error));

        // Act
        var resultado = await _controller.Delete("categoria01");

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>().Which.Value
            .Should().BeEquivalentTo(new { message = error.Message });
        _serviceMock.Verify(s => s.SoftDelete("categoria01"), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Delete_ExitoOError_RetornaOkOBadRequest(bool exito)
    {
        // Arrange
        const string id = "categoria01";
        _serviceMock.Setup(s => s.SoftDelete(id)).ReturnsAsync(exito
            ? Result.Success<bool, DomainError>(true)
            : Result.Failure<bool, DomainError>(new BadRequestError("Tiene funkos")));

        // Act
        var resultado = await _controller.Delete(id);

        // Assert
        var respuesta = resultado.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(exito ? 200 : 400);
        respuesta.Value.Should().BeEquivalentTo(exito ? (object)true : new { message = "Tiene funkos" });
        _serviceMock.Verify(s => s.SoftDelete(id), Times.Once);
    }
}

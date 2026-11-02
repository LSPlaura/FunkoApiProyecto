using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Back.Controllers;
using FunkoApi.Back.Dto;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Funko;
using FunkoApi.Back.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Controllers;

[TestFixture]
public class FunkosControllerTests
{
    private Mock<IFunkoService> _serviceMock = null!;
    private Mock<IStorageService> _storageMock = null!;
    private FunkosController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IFunkoService>();
        _storageMock = new Mock<IStorageService>();
        _controller = new FunkosController(_serviceMock.Object, _storageMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ConYSinDatos_RetornaOkConLista(bool conDatos)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, false, fecha, fecha);
        var funkos = conDatos ? new[] { funko } : Array.Empty<FunkoDto>();
        _serviceMock.Setup(s => s.GetAll()).ReturnsAsync(funkos);

        // Act
        var resultado = await _controller.GetAll();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(funkos);
        _serviceMock.Verify(s => s.GetAll(), Times.Once);
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public async Task GetByIdOGetByNombre_ExisteONoExiste_RetornaRespuestaCorrespondiente(bool porId, bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, false, fecha, fecha);
        var respuesta = existe ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new NotFoundError("No existe"));
        _serviceMock.Setup(s => s.GetById(1)).ReturnsAsync(respuesta);
        _serviceMock.Setup(s => s.GetByName(funko.Nombre)).ReturnsAsync(respuesta);

        // Act
        var resultado = porId ? await _controller.GetById(1) : await _controller.GetByNombre(funko.Nombre);

        // Assert
        var http = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        http.StatusCode.Should().Be(existe ? 200 : 404);
        http.Value.Should().BeEquivalentTo(existe ? (object)funko : new { message = "No existe" });
        _serviceMock.Verify(s => s.GetById(1), porId ? Times.Once() : Times.Never());
        _serviceMock.Verify(s => s.GetByName(funko.Nombre), porId ? Times.Never() : Times.Once());
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public async Task Create_FormularioConOSinImagen_RetornaCreatedOConflict(bool conImagen, bool exito)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, false, fecha, fecha);
        IFormFile? archivo = conImagen ? new Mock<IFormFile>().Object : null;
        const string proveedorId = "000000000000000000000001";
        _serviceMock.Setup(s => s.Create(It.IsAny<CreateFunkoDto>())).ReturnsAsync(exito
            ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new ConflictError("Duplicado")));

        // Act
        var resultado = await _controller.Create(funko.Nombre, funko.Precio, funko.Stock, categoria.Id, proveedorId, archivo);

        // Assert
        if (exito)
        {
            var http = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            http.Value.Should().Be(funko);
            http.ActionName.Should().Be(nameof(FunkosController.GetById));
            http.RouteValues!["id"].Should().Be(funko.Id);
        }
        else resultado.Result.Should().BeOfType<ConflictObjectResult>();
        _serviceMock.Verify(s => s.Create(It.Is<CreateFunkoDto>(d => d.Nombre == funko.Nombre
            && d.Precio == funko.Precio && d.Stock == funko.Stock && d.Categoria == categoria.Id
            && d.Proveedor == proveedorId && d.Imagen == archivo)), Times.Once);
        _storageMock.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Update_Formulario_RetornaOkONotFound(bool exito)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, false, fecha, fecha);
        var archivo = new Mock<IFormFile>().Object;
        const string proveedorId = "000000000000000000000001";
        _serviceMock.Setup(s => s.Update(It.IsAny<UpdateFunkoDto>(), 1)).ReturnsAsync(exito
            ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.Update(funko.Nombre, funko.Precio, funko.Stock, categoria.Id, proveedorId, archivo, 1);

        // Assert
        resultado.Result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(exito ? 200 : 404);
        _serviceMock.Verify(s => s.Update(It.Is<UpdateFunkoDto>(d => d.Nombre == funko.Nombre
            && d.Precio == funko.Precio && d.Stock == funko.Stock && d.Categoria == categoria.Id
            && d.Proveedor == proveedorId && d.Imagen == archivo), 1), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Delete_ExitoOError_RetornaOkONotFound(bool exito)
    {
        // Arrange
        _serviceMock.Setup(s => s.SoftDelete(1)).ReturnsAsync(exito ? Result.Success<bool, DomainError>(true)
            : Result.Failure<bool, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.Delete(1);

        // Assert
        resultado.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(exito ? 200 : 404);
        _serviceMock.Verify(s => s.SoftDelete(1), Times.Once);
    }

    [TestCase(false, "anterior.png", true, true, 404)]
    [TestCase(true, "anterior.png", false, true, 400)]
    [TestCase(true, "anterior.png", true, false, 409)]
    [TestCase(true, "anterior.png", true, true, 200)]
    [TestCase(true, "", true, true, 200)]
    public async Task UpdateImagen_DistintosResultados_RespetaLlamadas(bool existe, string anterior, bool guarda, bool actualiza, int codigo)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, anterior, categoria, false, fecha, fecha);
        var actualizado = funko with { Imagen = "nueva.png" };
        var archivo = new Mock<IFormFile>().Object;
        _serviceMock.Setup(s => s.GetById(1)).ReturnsAsync(existe ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new NotFoundError("No existe")));
        _storageMock.Setup(s => s.DeleteFileAsync(anterior)).ReturnsAsync(Result.Success<bool, DomainError>(true));
        _storageMock.Setup(s => s.SaveFileAsync(archivo, "funkos")).ReturnsAsync(guarda
            ? Result.Success<string, DomainError>("nueva.png") : Result.Failure<string, DomainError>(new BadRequestError("Imagen incorrecta")));
        _serviceMock.Setup(s => s.Update(It.IsAny<UpdateFunkoDto>(), 1)).ReturnsAsync(actualiza
            ? Result.Success<FunkoDto, DomainError>(actualizado) : Result.Failure<FunkoDto, DomainError>(new ConflictError("Conflicto")));

        // Act
        var resultado = await _controller.UpdateImagen(1, archivo);

        // Assert
        resultado.Result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(codigo);
        _serviceMock.Verify(s => s.GetById(1), Times.Once);
        _storageMock.Verify(s => s.DeleteFileAsync(anterior), existe && anterior != "" ? Times.Once() : Times.Never());
        _storageMock.Verify(s => s.SaveFileAsync(archivo, "funkos"), existe ? Times.Once() : Times.Never());
        _serviceMock.Verify(s => s.Update(It.Is<UpdateFunkoDto>(d => d.Nombre == funko.Nombre
            && d.Imagen == archivo && d.Categoria == categoria.Id), 1), existe && guarda ? Times.Once() : Times.Never());
    }

    [TestCase(0, 0, 1, 1, 0)]
    [TestCase(1, 200, 1, 100, 3)]
    [TestCase(2, 10, 2, 10, 3)]
    public async Task GetPaged_Parametros_NormalizaYDevuelveMetadatosYCabecera(int page, int size, int paginaEsperada, int sizeEsperado, int totalPages)
    {
        // Arrange
        _controller.Request.Scheme = "https";
        _controller.Request.Host = new HostString("localhost");
        _controller.Request.Path = "/api/funkos/paged";
        _serviceMock.Setup(s => s.GetPaged(paginaEsperada, sizeEsperado))
            .ReturnsAsync((Array.Empty<FunkoDto>().AsEnumerable(), totalPages, 0));

        // Act
        var resultado = await _controller.GetPaged(page, size);

        // Assert
        var body = resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value
            .Should().BeOfType<PagedResponse<FunkoDto>>().Subject;
        body.Data.Should().BeEmpty();
        body.Pagination.Should().BeEquivalentTo(new PageMetadata(paginaEsperada, sizeEsperado, totalPages, 0));
        if (totalPages == 0) _controller.Response.Headers.ContainsKey("Link").Should().BeFalse();
        else _controller.Response.Headers["Link"].ToString().Should().Contain("https://localhost/api/funkos/paged?");
        _serviceMock.Verify(s => s.GetPaged(paginaEsperada, sizeEsperado), Times.Once);
    }
}

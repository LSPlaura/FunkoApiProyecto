using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Back.Controllers;
using FunkoApi.Back.Dto.Categoria;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Funko;
using FunkoApi.Back.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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
        _controller = new FunkosController(_serviceMock.Object, _storageMock.Object);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ConYSinDatos_RetornaOkConLista(bool conDatos)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var proveedor = new ProveedorDto("000000000000000000000001", "Funko Corp", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, proveedor, false, fecha, fecha);
        var funkos = conDatos ? new[] { funko } : Array.Empty<FunkoDto>();
        _serviceMock.Setup(s => s.GetAll()).ReturnsAsync(funkos);

        // Act
        var resultado = await _controller.GetAll();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(funkos);
        _serviceMock.Verify(s => s.GetAll(), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetById_ExisteONoExiste_RetornaOkONotFound(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var proveedor = new ProveedorDto("000000000000000000000001", "Funko Corp", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, proveedor, false, fecha, fecha);
        _serviceMock.Setup(s => s.GetById(1)).ReturnsAsync(existe
            ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.GetById(1);

        // Assert
        var respuesta = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(existe ? 200 : 404);
        respuesta.Value.Should().BeEquivalentTo(existe ? (object)funko : new { message = "No existe" });
        _serviceMock.Verify(s => s.GetById(1), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetByNombre_ExisteONoExiste_RetornaOkONotFound(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var proveedor = new ProveedorDto("000000000000000000000001", "Funko Corp", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, proveedor, false, fecha, fecha);
        _serviceMock.Setup(s => s.GetByName(funko.Nombre)).ReturnsAsync(existe
            ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.GetByNombre(funko.Nombre);

        // Assert
        var respuesta = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(existe ? 200 : 404);
        respuesta.Value.Should().BeEquivalentTo(existe ? (object)funko : new { message = "No existe" });
        _serviceMock.Verify(s => s.GetByName(funko.Nombre), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Create_ExitoOConflicto_RetornaCreatedOConflict(bool exito)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var proveedor = new ProveedorDto("000000000000000000000001", "Funko Corp", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, proveedor, false, fecha, fecha);
        var dto = new CreateFunkoDto { Nombre = funko.Nombre, Precio = 15m, Stock = 10, Categoria = categoria.Id, Proveedor = proveedor.Id };
        _serviceMock.Setup(s => s.Create(dto)).ReturnsAsync(exito
            ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new ConflictError("Duplicado")));

        // Act
        var resultado = await _controller.Create(dto);

        // Assert
        if (exito)
        {
            var respuesta = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            respuesta.Value.Should().Be(funko);
            respuesta.ActionName.Should().Be(nameof(FunkosController.GetById));
            respuesta.RouteValues!["id"].Should().Be(funko.Id);
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
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var proveedor = new ProveedorDto("000000000000000000000001", "Funko Corp", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, proveedor, false, fecha, fecha);
        var dto = new UpdateFunkoDto { Nombre = funko.Nombre, Precio = 15m, Stock = 10, Categoria = categoria.Id, Proveedor = proveedor.Id };
        _serviceMock.Setup(s => s.Update(dto, 1)).ReturnsAsync(exito
            ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.Update(dto, 1);

        // Assert
        var respuesta = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(exito ? 200 : 404);
        respuesta.Value.Should().BeEquivalentTo(exito ? (object)funko : new { message = "No existe" });
        _serviceMock.Verify(s => s.Update(dto, 1), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Delete_ExitoOError_RetornaOkONotFound(bool exito)
    {
        // Arrange
        _serviceMock.Setup(s => s.SoftDelete(1)).ReturnsAsync(exito
            ? Result.Success<bool, DomainError>(true)
            : Result.Failure<bool, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.Delete(1);

        // Assert
        var respuesta = resultado.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(exito ? 200 : 404);
        respuesta.Value.Should().BeEquivalentTo(exito ? (object)true : new { message = "No existe" });
        _serviceMock.Verify(s => s.SoftDelete(1), Times.Once);
    }

    [TestCase(false, true, 400)]
    [TestCase(true, false, 409)]
    [TestCase(true, true, 201)]
    public async Task CreateConImagen_DistintosResultados_RetornaRespuestaYRespetaLlamadas(bool guarda, bool crea, int codigo)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var proveedor = new ProveedorDto("000000000000000000000001", "Funko Corp", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, "imagen.png", categoria, proveedor, false, fecha, fecha);
        var archivo = new Mock<IFormFile>().Object;
        _storageMock.Setup(s => s.SaveFileAsync(archivo, "funkos")).ReturnsAsync(guarda
            ? Result.Success<string, DomainError>("imagen.png")
            : Result.Failure<string, DomainError>(new BadRequestError("Imagen incorrecta")));
        _serviceMock.Setup(s => s.Create(It.IsAny<CreateFunkoDto>())).ReturnsAsync(crea
            ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new ConflictError("Duplicado")));

        // Act
        var resultado = await _controller.CreateConImagen(funko.Nombre, funko.Precio, funko.Stock,
            categoria.Id, ObjectId.Parse(proveedor.Id), archivo);

        // Assert
        resultado.Result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(codigo);
        if (guarda && crea) resultado.Result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().Be(funko);
        _storageMock.Verify(s => s.SaveFileAsync(archivo, "funkos"), Times.Once);
        _serviceMock.Verify(s => s.Create(It.Is<CreateFunkoDto>(d => d.Nombre == funko.Nombre
            && d.Precio == funko.Precio && d.Stock == funko.Stock && d.Categoria == categoria.Id
            && d.Proveedor == proveedor.Id && d.Imagen == "imagen.png")), guarda ? Times.Once() : Times.Never());
    }

    [TestCase(false, "anterior.png", true, true, 404)]
    [TestCase(true, "anterior.png", false, true, 400)]
    [TestCase(true, "anterior.png", true, false, 409)]
    [TestCase(true, "anterior.png", true, true, 200)]
    [TestCase(true, "", true, true, 200)]
    public async Task UpdateImagen_DistintosResultados_ConservaDatosYRespetaLlamadas(
        bool existe, string imagenAnterior, bool guarda, bool actualiza, int codigo)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var categoria = new CategoriaDto("categoria01", "Marvel", false, fecha, fecha);
        var proveedor = new ProveedorDto("000000000000000000000001", "Funko Corp", false, fecha, fecha);
        var funko = new FunkoDto(1, "Iron Man", 15m, 10, imagenAnterior, categoria, proveedor, false, fecha, fecha);
        var actualizado = funko with { Imagen = "nueva.png" };
        var archivo = new Mock<IFormFile>().Object;
        _serviceMock.Setup(s => s.GetById(1)).ReturnsAsync(existe
            ? Result.Success<FunkoDto, DomainError>(funko)
            : Result.Failure<FunkoDto, DomainError>(new NotFoundError("No existe")));
        _storageMock.Setup(s => s.DeleteFileAsync(imagenAnterior)).ReturnsAsync(Result.Success<bool, DomainError>(true));
        _storageMock.Setup(s => s.SaveFileAsync(archivo, "funkos")).ReturnsAsync(guarda
            ? Result.Success<string, DomainError>("nueva.png")
            : Result.Failure<string, DomainError>(new BadRequestError("Imagen incorrecta")));
        _serviceMock.Setup(s => s.Update(It.IsAny<UpdateFunkoDto>(), 1)).ReturnsAsync(actualiza
            ? Result.Success<FunkoDto, DomainError>(actualizado)
            : Result.Failure<FunkoDto, DomainError>(new ConflictError("Conflicto")));

        // Act
        var resultado = await _controller.UpdateImagen(1, archivo);

        // Assert
        resultado.Result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(codigo);
        if (existe && guarda && actualiza) resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(actualizado);
        _serviceMock.Verify(s => s.GetById(1), Times.Once);
        _storageMock.Verify(s => s.DeleteFileAsync(imagenAnterior), existe && imagenAnterior != "" ? Times.Once() : Times.Never());
        _storageMock.Verify(s => s.SaveFileAsync(archivo, "funkos"), existe ? Times.Once() : Times.Never());
        _serviceMock.Verify(s => s.Update(It.Is<UpdateFunkoDto>(d => d.Nombre == funko.Nombre
            && d.Precio == funko.Precio && d.Stock == funko.Stock && d.Categoria == categoria.Id
            && d.Proveedor == proveedor.Id && d.Imagen == "nueva.png"), 1), existe && guarda ? Times.Once() : Times.Never());
    }
}

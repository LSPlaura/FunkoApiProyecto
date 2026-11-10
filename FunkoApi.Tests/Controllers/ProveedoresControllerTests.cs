using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Back.Controllers;
using FunkoApi.Back.Dto.Proveedor;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Proveedor;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Controllers;

[TestFixture]
public class ProveedoresControllerTests
{
    private Mock<IProveedorService> _serviceMock = null!;
    private ProveedoresController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IProveedorService>();
        _controller = new ProveedoresController(_serviceMock.Object);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAll_ConYSinDatos_RetornaOkConLista(bool conDatos)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedores = conDatos ? new[] { new ProveedorDto("000000000000000000000001", "Funko Corp", false, fecha, fecha) } : Array.Empty<ProveedorDto>();
        _serviceMock.Setup(s => s.GetAll()).ReturnsAsync(proveedores);

        // Act
        var resultado = await _controller.GetAll();

        // Assert
        resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(proveedores);
        _serviceMock.Verify(s => s.GetAll(), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetById_ExisteONoExiste_RetornaOkONotFound(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedor = new ProveedorDto("000000000000000000000001", "Funko Corp", false, fecha, fecha);
        _serviceMock.Setup(s => s.GetById(proveedor.Id)).ReturnsAsync(existe
            ? Result.Success<ProveedorDto, DomainError>(proveedor)
            : Result.Failure<ProveedorDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.GetById(proveedor.Id);

        // Assert
        var respuesta = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(existe ? 200 : 404);
        respuesta.Value.Should().BeEquivalentTo(existe ? (object)proveedor : new { message = "No existe" });
        _serviceMock.Verify(s => s.GetById(proveedor.Id), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Create_ExitoOError_RetornaCreatedOBadRequest(bool exito)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var dto = new CreateProveedorDto { Nombre = "Funko Corp" };
        var proveedor = new ProveedorDto("000000000000000000000001", dto.Nombre, false, fecha, fecha);
        _serviceMock.Setup(s => s.Create(dto)).ReturnsAsync(exito
            ? Result.Success<ProveedorDto, DomainError>(proveedor)
            : Result.Failure<ProveedorDto, DomainError>(new BadRequestError("Datos incorrectos")));

        // Act
        var resultado = await _controller.Create(dto);

        // Assert
        if (exito)
        {
            var respuesta = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            respuesta.ActionName.Should().Be(nameof(ProveedoresController.GetById));
            respuesta.RouteValues!["id"].Should().Be(proveedor.Id);
            respuesta.Value.Should().Be(proveedor);
        }
        else resultado.Result.Should().BeOfType<BadRequestObjectResult>();
        _serviceMock.Verify(s => s.Create(dto), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Update_ExitoOError_RetornaOkONotFound(bool exito)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var dto = new UpdateProveedorDto { Nombre = "Actualizado" };
        var proveedor = new ProveedorDto("000000000000000000000001", dto.Nombre, false, fecha, fecha);
        _serviceMock.Setup(s => s.Update(dto, proveedor.Id)).ReturnsAsync(exito
            ? Result.Success<ProveedorDto, DomainError>(proveedor)
            : Result.Failure<ProveedorDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.Update(dto, proveedor.Id);

        // Assert
        var respuesta = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(exito ? 200 : 404);
        respuesta.Value.Should().BeEquivalentTo(exito ? (object)proveedor : new { message = "No existe" });
        _serviceMock.Verify(s => s.Update(dto, proveedor.Id), Times.Once);
    }

    [Test]
    public async Task Delete_ProveedorInexistente_Retorna404ConMensaje()
    {
        // Arrange
        const string id = "000000000000000000000001";
        var error = ProveedorError.ProveedoraNotFound(id);
        _serviceMock.Setup(s => s.SoftDelete(id)).ReturnsAsync(Result.Failure<bool, DomainError>(error));

        // Act
        var resultado = await _controller.Delete(id);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>().Which.Value
            .Should().BeEquivalentTo(new { message = error.Message });
        _serviceMock.Verify(s => s.SoftDelete(id), Times.Once);
    }

    [Test]
    public async Task Delete_ServicioNoModificaProveedor_Retorna200False()
    {
        // Arrange
        const string id = "000000000000000000000001";
        _serviceMock.Setup(s => s.SoftDelete(id)).ReturnsAsync(Result.Success<bool, DomainError>(false));

        // Act
        var resultado = await _controller.Delete(id);

        // Assert
        var respuesta = resultado.Should().BeOfType<OkObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(200);
        respuesta.Value.Should().Be(false);
        _serviceMock.Verify(s => s.SoftDelete(id), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Delete_ExitoOConflicto_RetornaOkOConflict(bool exito)
    {
        // Arrange
        const string id = "000000000000000000000001";
        _serviceMock.Setup(s => s.SoftDelete(id)).ReturnsAsync(exito
            ? Result.Success<bool, DomainError>(true)
            : Result.Failure<bool, DomainError>(new ConflictError("Tiene funkos")));

        // Act
        var resultado = await _controller.Delete(id);

        // Assert
        var respuesta = resultado.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(exito ? 200 : 409);
        respuesta.Value.Should().BeEquivalentTo(exito ? (object)true : new { message = "Tiene funkos" });
        _serviceMock.Verify(s => s.SoftDelete(id), Times.Once);
    }
}

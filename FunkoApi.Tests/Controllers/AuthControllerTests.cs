using System.Security.Claims;
using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.Back.Controllers;
using FunkoApi.Back.Dto.Auth;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Auth.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private Mock<IAuthService> _serviceMock = null!;
    private AuthController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IAuthService>();
        _controller = new AuthController(_serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Login_ResultadoDelServicio_RetornaRespuestaCorrespondiente(bool exito)
    {
        // Arrange
        var dto = new LoginDto("usuario@example.test", "Prueba123!");
        var respuesta = new AuthResponseDto("token-sintetico", dto.Email, "USER", DateTime.UtcNow.AddMinutes(5));
        _serviceMock.Setup(s => s.LoginAsync(dto)).ReturnsAsync(exito ? Result.Success<AuthResponseDto, DomainError>(respuesta)
            : Result.Failure<AuthResponseDto, DomainError>(UnauthorizedError.InvalidCredentials()));

        // Act
        var resultado = await _controller.Login(dto);

        // Assert
        if (exito) resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(respuesta);
        else
        {
            // El contrato de autenticación debe devolver 401, no convertir UnauthorizedError en 500.
            var http = resultado.Result.Should().BeAssignableTo<ObjectResult>().Subject;
            http.StatusCode.Should().Be(401);
            http.Value.Should().BeEquivalentTo(new { message = UnauthorizedError.InvalidCredentials().Message });
        }
        _serviceMock.Verify(s => s.LoginAsync(dto), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Signup_ResultadoDelServicio_RetornaOkOConflict(bool exito)
    {
        // Arrange
        var dto = new SignupDto("usuario@example.test", "Prueba123!", "Lucia", "Pruebas");
        var respuesta = new AuthResponseDto("token-sintetico", dto.Email, "USER", DateTime.UtcNow.AddMinutes(5));
        _serviceMock.Setup(s => s.SignupAsync(dto)).ReturnsAsync(exito ? Result.Success<AuthResponseDto, DomainError>(respuesta)
            : Result.Failure<AuthResponseDto, DomainError>(new ConflictError("Duplicado")));

        // Act
        var resultado = await _controller.Signup(dto);

        // Assert
        resultado.Result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(exito ? 200 : 409);
        _serviceMock.Verify(s => s.SignupAsync(dto), Times.Once);
    }

    [Test]
    public async Task Signup_IdentityRechazaRegistro_Retorna422ConMensaje()
    {
        // Arrange
        var dto = new SignupDto("usuario@example.test", "sinsimbolos", "Lucia", "Pruebas");
        var error = new BusinessRuleError("Error al crear el usuario: contraseña no válida");
        _serviceMock.Setup(s => s.SignupAsync(dto))
            .ReturnsAsync(Result.Failure<AuthResponseDto, DomainError>(error));

        // Act
        var resultado = await _controller.Signup(dto);

        // Assert
        var respuesta = resultado.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(422);
        respuesta.Value.Should().BeEquivalentTo(new { message = error.Message });
        _serviceMock.Verify(s => s.SignupAsync(dto), Times.Once);
        _serviceMock.Verify(s => s.LoginAsync(It.IsAny<LoginDto>()), Times.Never);
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, false)]
    public async Task GetMe_ConOSinClaim_DelegaIdentificador(bool conClaim, bool existe)
    {
        // Arrange
        long id = conClaim ? 1 : 0;
        if (conClaim) _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1") }));
        var user = new UserDto(id, "usuario@example.test", "Lucia", "Pruebas", "USER");
        _serviceMock.Setup(s => s.GetMeAsync(id)).ReturnsAsync(existe ? Result.Success<UserDto, DomainError>(user)
            : Result.Failure<UserDto, DomainError>(new NotFoundError("No existe")));

        // Act
        var resultado = await _controller.GetMe();

        // Assert
        resultado.Result.Should().BeAssignableTo<ObjectResult>().Which.StatusCode.Should().Be(existe ? 200 : 404);
        if (existe) resultado.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(user);
        _serviceMock.Verify(s => s.GetMeAsync(id), Times.Once);
    }
}

using FluentAssertions;
using FunkoApi.Back.Controllers;
using Microsoft.AspNetCore.Authorization;
using NUnit.Framework;

namespace FunkoApi.Tests.Controllers;

// Como Authorization Attribute Tests de ProductosControllerTests del profesor.
// Comprueba los atributos; no sustituye una petición HTTP con JWT y autorización real.
[TestFixture]
public class AuthorizationAttributesTests
{
    [TestCase(typeof(CategoriasController), nameof(CategoriasController.GetAll), "ADMIN,USER")]
    [TestCase(typeof(CategoriasController), nameof(CategoriasController.GetPaged), "ADMIN,USER")]
    [TestCase(typeof(CategoriasController), nameof(CategoriasController.GetById), "ADMIN,USER")]
    [TestCase(typeof(CategoriasController), nameof(CategoriasController.GetByName), "ADMIN,USER")]
    [TestCase(typeof(CategoriasController), nameof(CategoriasController.Post), "ADMIN")]
    [TestCase(typeof(CategoriasController), nameof(CategoriasController.Update), "ADMIN")]
    [TestCase(typeof(CategoriasController), nameof(CategoriasController.Delete), "ADMIN")]
    [TestCase(typeof(FunkosController), nameof(FunkosController.GetAll), "ADMIN,USER")]
    [TestCase(typeof(FunkosController), nameof(FunkosController.GetPaged), "ADMIN,USER")]
    [TestCase(typeof(FunkosController), nameof(FunkosController.GetById), "ADMIN")]
    [TestCase(typeof(FunkosController), nameof(FunkosController.GetByNombre), "ADMIN")]
    [TestCase(typeof(FunkosController), nameof(FunkosController.Create), "ADMIN")]
    [TestCase(typeof(FunkosController), nameof(FunkosController.Update), "ADMIN")]
    [TestCase(typeof(FunkosController), nameof(FunkosController.UpdateImagen), "ADMIN")]
    [TestCase(typeof(FunkosController), nameof(FunkosController.Delete), "ADMIN")]
    [TestCase(typeof(ProveedoresController), nameof(ProveedoresController.GetAll), "ADMIN")]
    [TestCase(typeof(ProveedoresController), nameof(ProveedoresController.GetPaged), "ADMIN")]
    [TestCase(typeof(ProveedoresController), nameof(ProveedoresController.GetById), "ADMIN")]
    [TestCase(typeof(ProveedoresController), nameof(ProveedoresController.Create), "ADMIN")]
    [TestCase(typeof(ProveedoresController), nameof(ProveedoresController.Update), "ADMIN")]
    [TestCase(typeof(ProveedoresController), nameof(ProveedoresController.Delete), "ADMIN")]
    public void Endpoint_Protegido_ConservaRolesPermitidos(Type controlador, string metodo, string roles)
    {
        // Arrange
        var methodInfo = controlador.GetMethod(metodo)!;

        // Act
        var permisos = methodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().ToList();

        // Assert
        permisos.Should().ContainSingle().Which.Roles.Should().Be(roles);
        methodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().BeEmpty();
    }

    [TestCase(nameof(AuthController.Login))]
    [TestCase(nameof(AuthController.Signup))]
    public void Auth_OperacionPublica_NoExigeAutorizacion(string metodo)
    {
        // Arrange
        var controlador = typeof(AuthController);
        var methodInfo = controlador.GetMethod(metodo)!;

        // Act
        var permisos = methodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true);

        // Assert
        permisos.Should().BeEmpty();
        controlador.GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().BeEmpty();
    }

    [Test]
    public void GetMe_UsuarioAutenticado_NoExigeRolConcreto()
    {
        // Arrange
        var methodInfo = typeof(AuthController).GetMethod(nameof(AuthController.GetMe))!;

        // Act
        var permisos = methodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().ToList();

        // Assert
        permisos.Should().ContainSingle().Which.Roles.Should().BeNullOrEmpty();
        methodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().BeEmpty();
    }
}

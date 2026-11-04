using FluentAssertions;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Extensions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace FunkoApi.Tests.Extensions;

[TestFixture]
public class DomainErrorExtensionsTests
{
    [TestCase("notfound", 404)]
    [TestCase("badrequest", 400)]
    [TestCase("validation", 400)]
    [TestCase("conflict", 409)]
    [TestCase("businessrule", 422)]
    [TestCase("internal", 500)]
    public void ToHttpResult_NoGenerico_MapeaCodigoYCuerpo(string tipo, int codigo)
    {
        // Arrange
        DomainError error = tipo switch
        {
            "notfound" => new NotFoundError("mensaje"),
            "badrequest" => new BadRequestError("mensaje"),
            "validation" => new BadRequestError.ValidationError("mensaje", new Dictionary<string, string[]> { ["Nombre"] = ["obligatorio"] }),
            "conflict" => new ConflictError("mensaje"),
            "businessrule" => new BusinessRuleError("mensaje"),
            _ => new InternalError("privado")
        };

        // Act
        var resultado = error.ToHttpResult();

        // Assert
        var respuesta = resultado.Should().BeAssignableTo<ObjectResult>().Subject;
        respuesta.StatusCode.Should().Be(codigo);
        if (tipo == "validation") respuesta.Value.Should().BeEquivalentTo(new
        {
            message = "mensaje", errors = new Dictionary<string, string[]> { ["Nombre"] = ["obligatorio"] }
        });
        else respuesta.Value.Should().BeEquivalentTo(new { message = codigo == 500 ? "Error interno del servidor" : "mensaje" });
    }

    [TestFixture]
    public class CasosValidos
    {
        [TestCase(typeof(NotFoundError), 404)]
        [TestCase(typeof(BadRequestError), 400)]
        [TestCase(typeof(ConflictError), 409)]
        [TestCase(typeof(BusinessRuleError), 422)]
        public void ToHttpResult_Generico_MapeaCodigoHttp(Type errorType, int statusCode)
        {
            // Arrange
            var error = (DomainError)Activator.CreateInstance(errorType, "error")!;

            // Act
            var result = error.ToHttpResult<string>();

            // Assert
            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(statusCode);
        }

        [Test]
        public void ToHttpResult_ValidationError_ConservaErroresPorCampo()
        {
            // Arrange
            var error = BadRequestError.ValidationError.WithFieldErrors(
                new Dictionary<string, string[]> { ["Nombre"] = ["obligatorio"] });

            // Act
            var result = error.ToHttpResult<string>();

            // Assert
            var response = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            response.Value.Should().BeEquivalentTo(new
            {
                message = "Errores de validación",
                errors = new Dictionary<string, string[]> { ["Nombre"] = ["obligatorio"] }
            });
        }

        [Test]
        public void ToHttpResult_SobrecargaNoGenerica_MapeaNotFound()
        {
            // Arrange
            DomainError error = new NotFoundError("no existe");

            // Act
            var result = error.ToHttpResult();

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Test]
        public void ToHttpResult_ExtensionNoPermitida_Devuelve422ConMensaje()
        {
            // Arrange
            var error = StorageError.ExtensionNoPermitida();

            // Act
            var result = error.ToHttpResult<string>();

            // Assert
            var response = result.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
            response.StatusCode.Should().Be(422);
            response.Value.Should().BeEquivalentTo(new { message = "La extensión del archivo no está permitida." });
        }

        [Test]
        public void ToHttpResult_TipoContenidoNoPermitido_NoGenerico_Devuelve422ConMensaje()
        {
            // Arrange
            var error = StorageError.TipoContenidoNoPermitido();

            // Act
            var result = error.ToHttpResult();

            // Assert
            var response = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
            response.StatusCode.Should().Be(422);
            response.Value.Should().BeEquivalentTo(new { message = "El tipo de contenido del archivo no está permitido." });
        }
    }

    [TestFixture]
    public class ErroresDeAutenticacion
    {
        [Test]
        public void ToHttpResult_Generico_Unauthorized_Devuelve401()
        {
            // Arrange
            DomainError error = UnauthorizedError.InvalidCredentials();

            // Act
            var resultado = error.ToHttpResult<string>();

            // Assert
            var respuesta = resultado.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            respuesta.StatusCode.Should().Be(401);
            respuesta.Value.Should().BeEquivalentTo(new { message = "Credenciales inválidas" });
        }

        [Test]
        public void ToHttpResult_NoGenerico_Unauthorized_Devuelve401()
        {
            // Arrange
            DomainError error = UnauthorizedError.TokenExpired();

            // Act
            var resultado = error.ToHttpResult();

            // Assert
            var respuesta = resultado.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            respuesta.StatusCode.Should().Be(401);
            respuesta.Value.Should().BeEquivalentTo(new { message = "Token expirado o inválido" });
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [Test]
        public void ToHttpResult_ErrorDesconocido_Devuelve500()
        {
            // Arrange
            DomainError error = new InternalError("fallo");

            // Act
            var result = error.ToHttpResult<int>();

            // Assert
            result.Result.Should().BeOfType<ObjectResult>()
                .Which.StatusCode.Should().Be(500);
        }
    }
}

using FluentAssertions;
using FunkoApi.Back.Errors;
using NUnit.Framework;

namespace FunkoApi.Tests.Errors;

[TestFixture]
public class DomainErrorsTests
{
    [TestFixture]
    public class CasosValidos
    {
        [Test]
        public void NotFoundError_Factories_ConstruyenMensajesEstandar()
        {
            // Act
            var byId = NotFoundError.FromId(4, "Funko");
            var byName = NotFoundError.FromName("Iron Man", "Funko");

            // Assert
            byId.Message.Should().Be("Funko con ID 4 no encontrado");
            byName.Message.Should().Be("Funko con nombre Iron Man no encontrado");
            byId.ToString().Should().Contain("Funko con ID 4 no encontrado");
        }

        [Test]
        public void ErroresEspecializados_Factories_ConstruyenMensajes()
        {
            // Act
            var validation = BadRequestError.ValidationError.WithFieldErrors(
                new Dictionary<string, string[]> { ["Nombre"] = ["Es obligatorio"] });
            var unauthorized = UnauthorizedError.InvalidCredentials();
            var expired = UnauthorizedError.TokenExpired();
            var forbidden = ForbiddenError.NotOwner("Funko", "4");
            var conflict = ConflictError.Duplicate("Funko", "Iron Man");

            // Assert
            validation.ValidationErrors.Should().ContainKey("Nombre");
            unauthorized.Message.Should().Be("Credenciales inválidas");
            expired.Message.Should().Be("Token expirado o inválido");
            forbidden.Message.Should().Contain("Funko (ID: 4)");
            conflict.Message.Should().Contain("Iron Man");
        }

        [Test]
        public void FunkoError_YCategoriaError_DevuelvenTiposCorrectos()
        {
            // Act
            var funko = FunkoError.FunkoAlreadyExists("Iron Man");
            var categoria = CategoriaError.CategoriaNotFound(1);

            // Assert
            funko.Should().BeOfType<ConflictError>();
            funko.Message.Should().Contain("Iron Man");
            categoria.Should().BeOfType<NotFoundError>();
            categoria.Message.Should().Contain("Categoria");
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [Test]
        public void DomainError_ToString_UsaElTipoConcreto()
        {
            // Arrange
            DomainError error = new InternalError("fallo");

            // Act
            var result = error.ToString();

            // Assert
            result.Should().Contain("InternalError").And.Contain("fallo");
        }
    }
}
using FluentAssertions;
using FunkoApi.Back.Errors;
using NUnit.Framework;

namespace FunkoApi.Tests.Errors;

[TestFixture]
public class CategoriaErrorTests
{
    [Test]
    public void CategoriaConflictError_Identificador_ConservaTipoYMensaje()
    {
        // Arrange
        const string id = "categoria01";

        // Act
        var resultado = CategoriaError.CategoriaConflictError(id);

        // Assert
        resultado.Should().Be(new ConflictError($"Se han tenido problemas a la hora de operar con la Categoria con el ID {id}"));
    }
}

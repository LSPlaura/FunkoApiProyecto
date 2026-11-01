using FluentAssertions;
using FunkoApi.Back.Errors;
using NUnit.Framework;

namespace FunkoApi.Tests.Errors;

[TestFixture]
public class StorageErrorTests
{
    [Test]
    public void ErrorEliminando_ErrorDeDisco_ConstruyeInternalError()
    {
        // Arrange
        const string mensaje = "No se ha podido eliminar el archivo del servidor.";

        // Act
        var resultado = StorageError.ErrorEliminando();

        // Assert
        resultado.Should().Be(new InternalError(mensaje));
    }
}

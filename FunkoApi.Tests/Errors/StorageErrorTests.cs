using FluentAssertions;
using FunkoApi.Back.Errors;
using NUnit.Framework;

namespace FunkoApi.Tests.Errors;

[TestFixture]
public class StorageErrorTests
{
    [Test]
    public void ArchivoVacio_SinContenido_ConstruyeBusinessRuleError()
    {
        // Arrange
        const string mensaje = "El archivo está vacío o no se ha proporcionado.";

        // Act
        var resultado = StorageError.ArchivoVacio();

        // Assert
        resultado.Should().Be(new BusinessRuleError(mensaje));
    }

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

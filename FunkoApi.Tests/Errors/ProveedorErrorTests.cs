using FluentAssertions;
using FunkoApi.Back.Errors;
using NUnit.Framework;

namespace FunkoApi.Tests.Errors;

[TestFixture]
public class ProveedorErrorTests
{
    [Test]
    public void Factories_DatosDelProveedor_ConservanTipoYMensaje()
    {
        // Arrange
        const string id = "000000000000000000000001";

        // Act
        var porId = ProveedorError.ProveedoraNotFound(id);
        var solicitud = ProveedorError.ProveedorBadRequest(id);

        // Assert
        porId.Should().Be(new NotFoundError($"Proveedor con ID {id} no encontrado"));
        solicitud.Should().Be(new BadRequestError($"Proveedor con el ID {id} no encontrada"));
    }
}

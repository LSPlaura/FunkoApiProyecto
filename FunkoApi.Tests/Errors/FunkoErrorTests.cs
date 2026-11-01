using FluentAssertions;
using FunkoApi.Back.Errors;
using NUnit.Framework;

namespace FunkoApi.Tests.Errors;

[TestFixture]
public class FunkoErrorTests
{
    [Test]
    public void Factories_DatosDelFunko_ConservanTipoYMensaje()
    {
        // Arrange
        const string nombre = "Iron Man";

        // Act
        var porId = FunkoError.FunkoNotFound(1);
        var porNombre = FunkoError.FunkoNotFoundByName(nombre);
        var duplicado = FunkoError.FunkoAlreadyExists(nombre);
        var stock = FunkoError.InsufficientStockWithName(nombre);
        var precio = FunkoError.InvalidPrice(-1);
        var proveedor = FunkoError.FunkoBadRequestProveedor(nombre);

        // Assert
        porId.Should().Be(new NotFoundError("Funko con ID 1 no encontrado"));
        porNombre.Should().Be(new NotFoundError("No se encontró el Funko con el nombre 'Iron Man'"));
        duplicado.Should().Be(new ConflictError("Ya existe un Funko registrado con el nombre 'Iron Man'"));
        stock.Should().Be(new BusinessRuleError("Stock insuficiente para el Funko 'Iron Man'"));
        precio.Should().Be(new BusinessRuleError("El precio -1 no es válido"));
        proveedor.Should().Be(new BadRequestError("Proveedor no encontrado para el Funko 'Iron Man'"));
    }
}

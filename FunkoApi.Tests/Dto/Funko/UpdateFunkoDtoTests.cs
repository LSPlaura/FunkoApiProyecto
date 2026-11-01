using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using FunkoApi.Back.Dto.Funko;
using NUnit.Framework;

namespace FunkoApi.Tests.Dtos.Funko;

[TestFixture]
public class UpdateFunkoDtoTests
{
    [TestCase("Nombre")]
    [TestCase("Precio")]
    [TestCase("Stock")]
    [TestCase("Categoria")]
    [TestCase("Proveedor")]
    [TestCase("Imagen")]
    public void Validar_CampoInvalido_IdentificaPropiedad(string campo)
    {
        // Arrange
        var dto = new UpdateFunkoDto
        {
            Nombre = "Iron Man", Precio = 15m, Stock = 1,
            Categoria = "categoria01", Proveedor = "000000000000000000000001"
        };
        dto = campo switch
        {
            "Nombre" => dto with { Nombre = new string('A', 101) },
            "Precio" => dto with { Precio = 0 },
            "Stock" => dto with { Stock = 0 },
            "Categoria" => dto with { Categoria = "" },
            "Proveedor" => dto with { Proveedor = "" },
            _ => dto with { Imagen = new string('A', 1001) }
        };
        var errores = new List<ValidationResult>();

        // Act
        var resultado = Validator.TryValidateObject(dto, new ValidationContext(dto), errores, true);

        // Assert
        resultado.Should().BeFalse();
        errores.Should().ContainSingle().Which.MemberNames.Should().Contain(campo);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("imagen.png")]
    public void Validar_DatosValidosConImagenOpcional_PasaValidacion(string? imagen)
    {
        // Arrange
        var dto = new UpdateFunkoDto
        {
            Nombre = "Iron Man", Precio = 15m, Stock = 1, Imagen = imagen,
            Categoria = "categoria01", Proveedor = "000000000000000000000001"
        };
        var errores = new List<ValidationResult>();

        // Act
        var resultado = Validator.TryValidateObject(dto, new ValidationContext(dto), errores, true);

        // Assert
        resultado.Should().BeTrue();
        errores.Should().BeEmpty();
    }
}

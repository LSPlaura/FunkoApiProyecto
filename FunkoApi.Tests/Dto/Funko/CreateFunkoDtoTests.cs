using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using FunkoApi.Back.Dto.Funko;
using NUnit.Framework;

namespace FunkoApi.Tests.Dtos.Funko;

[TestFixture]
public class CreateFunkoDtoTests
{
    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(100, true)]
    [TestCase(101, false)]
    public void Validar_NombreEnDistintosLimites_RetornaResultadoEsperado(int longitud, bool valido)
    {
        // Arrange
        var dto = new CreateFunkoDto
        {
            Nombre = new string('A', longitud), Precio = 15m, Stock = 1,
            Categoria = "categoria01", Proveedor = "000000000000000000000001", Imagen = null
        };
        var errores = new List<ValidationResult>();

        // Act
        var resultado = Validator.TryValidateObject(dto, new ValidationContext(dto), errores, true);

        // Assert
        resultado.Should().Be(valido);
        if (valido) errores.Should().BeEmpty();
        else errores.Should().Contain(e => e.MemberNames.Contains(nameof(dto.Nombre)));
    }

    [TestCase("Nombre")]
    [TestCase("Precio")]
    [TestCase("Stock")]
    [TestCase("Categoria")]
    [TestCase("Proveedor")]
    public void Validar_CampoInvalido_IdentificaPropiedad(string campo)
    {
        // Arrange
        var dto = new CreateFunkoDto
        {
            Nombre = "Iron Man", Precio = 15m, Stock = 1,
            Categoria = "categoria01", Proveedor = "000000000000000000000001"
        };
        dto = campo switch
        {
            "Nombre" => dto with { Nombre = null! },
            "Precio" => dto with { Precio = 0 },
            "Stock" => dto with { Stock = 0 },
            "Categoria" => dto with { Categoria = "" },
            _ => dto with { Proveedor = "" }
        };
        var errores = new List<ValidationResult>();

        // Act
        var resultado = Validator.TryValidateObject(dto, new ValidationContext(dto), errores, true);

        // Assert
        resultado.Should().BeFalse();
        errores.Should().ContainSingle().Which.MemberNames.Should().Contain(campo);
    }

    [TestCase(0.01, 1, true)]
    [TestCase(999999.99, int.MaxValue, true)]
    [TestCase(-1, 1, false)]
    [TestCase(1000000, 1, false)]
    [TestCase(15, -1, false)]
    public void Validar_PrecioYStockEnLimites_RespetaRangos(double precio, int stock, bool valido)
    {
        // Arrange
        var dto = new CreateFunkoDto
        {
            Nombre = "Iron Man", Precio = (decimal)precio, Stock = stock,
            Categoria = "categoria01", Proveedor = "000000000000000000000001"
        };
        var errores = new List<ValidationResult>();

        // Act
        var resultado = Validator.TryValidateObject(dto, new ValidationContext(dto), errores, true);

        // Assert
        resultado.Should().Be(valido);
        errores.Should().HaveCount(valido ? 0 : 1);
    }
}

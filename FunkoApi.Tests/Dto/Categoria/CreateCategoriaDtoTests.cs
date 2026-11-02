using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using FunkoApi.Back.Dto.Categoria;
using NUnit.Framework;

namespace FunkoApi.Tests.Dtos.Categoria;

[TestFixture]
public class CreateCategoriaDtoTests
{
    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(100, true)]
    [TestCase(101, false)]
    public void Validar_NombreEnLimites_RespetaRequiredYMaxLength(int longitud, bool valido)
    {
        // Arrange
        var dto = new CreateCategoriaDto { Nombre = new string('A', longitud) };
        var errores = new List<ValidationResult>();

        // Act
        var resultado = Validator.TryValidateObject(dto, new ValidationContext(dto), errores, true);

        // Assert
        resultado.Should().Be(valido);
        if (valido) errores.Should().BeEmpty();
        else errores.Should().ContainSingle().Which.MemberNames.Should().Contain(nameof(dto.Nombre));
    }
}

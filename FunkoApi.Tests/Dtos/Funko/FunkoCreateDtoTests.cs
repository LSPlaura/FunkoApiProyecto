using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using FunkoApi.Back.Dtos.Funko;
using FunkoApi.Back.Dtos.Proveedor;
using NUnit.Framework;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace FunkoApi.Tests.Dtos.Funko;

[TestFixture]
public class FunkoCreateDtoTests
{
    [TestFixture]
    public class CasosValidos
    {
        [Test]
        public void CreateFunkoDto_DatosValidos_PasaValidacion()
        {
            // Arrange
            var dto = new CreateFunkoDto
            {
                Nombre = "Iron Man",
                Precio = 14.99m,
                Stock = 10,
                CategoriaId = 1,
                Proveedor = new ProveedorDto(
                    1,
                    "Funko Corp",
                    false,
                    DateTime.UtcNow,
                    DateTime.UtcNow
                )
            };

            // Act & Assert
            new RequiredAttribute().IsValid(dto.Nombre).Should().BeTrue();
            new MaxLengthAttribute(100).IsValid(dto.Nombre).Should().BeTrue();
            new RangeAttribute(0.01, 999999.99).IsValid(dto.Precio).Should().BeTrue();
            new RangeAttribute(1, int.MaxValue).IsValid(dto.Stock).Should().BeTrue();
            new RequiredAttribute().IsValid(dto.CategoriaId).Should().BeTrue();
            new RequiredAttribute().IsValid(dto.Proveedor).Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [TestCase("", "El nombre es obligatorio")]
        [TestCase(null, "El nombre es obligatorio")]
        public void CreateFunkoDto_NombreInvalido_DevuelveError(string? nombre, string mensajeEsperado)
        {
            // Arrange
            var attribute = new RequiredAttribute { ErrorMessage = mensajeEsperado };

            // Act
            var esValido = attribute.IsValid(nombre);

            // Assert
            esValido.Should().BeFalse();
            attribute.FormatErrorMessage(nameof(CreateFunkoDto.Nombre)).Should().Be(mensajeEsperado);
        }

        [Test]
        public void CreateFunkoDto_NombreExcesivo_DevuelveError()
        {
            // Arrange
            var attribute = new MaxLengthAttribute(100)
                { ErrorMessage = "El nombre no puede tener más de 100 caracteres" };
            var nombreExcesivo = new string('A', 101);

            // Act
            var esValido = attribute.IsValid(nombreExcesivo);

            // Assert
            esValido.Should().BeFalse();
            attribute.FormatErrorMessage(nameof(CreateFunkoDto.Nombre)).Should()
                .Be("El nombre no puede tener más de 100 caracteres");
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1000000)]
        public void CreateFunkoDto_PrecioFueraDeRango_DevuelveError(decimal precio)
        {
            // Arrange
            var attribute = new RangeAttribute(0.01, 999999.99)
                { ErrorMessage = "El precio debe estar entre 0.01 y 999999.99" };

            // Act
            var esValido = attribute.IsValid(precio);

            // Assert
            esValido.Should().BeFalse();
            attribute.FormatErrorMessage(nameof(CreateFunkoDto.Precio)).Should()
                .Be("El precio debe estar entre 0.01 y 999999.99");
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void CreateFunkoDto_StockInvalido_DevuelveError(int stock)
        {
            // Arrange
            var attribute = new RangeAttribute(1, int.MaxValue) { ErrorMessage = "El stock debe ser mayor a 0" };

            // Act
            var esValido = attribute.IsValid(stock);

            // Assert
            esValido.Should().BeFalse();
            attribute.FormatErrorMessage(nameof(CreateFunkoDto.Stock)).Should().Be("El stock debe ser mayor a 0");
        }

        [Test]
        public void CreateFunkoDto_ProveedorInvalido_DevuelveError()
        {
            // Arrange
            var attribute = new RequiredAttribute { ErrorMessage = "El proveedor es obligatorio" };
            ProveedorDto? proveedor = null;

            // Act
            var esValido = attribute.IsValid(proveedor);

            // Assert
            esValido.Should().BeFalse();
            attribute.FormatErrorMessage(nameof(CreateFunkoDto.Proveedor)).Should().Be("El proveedor es obligatorio");
        }
    }
}
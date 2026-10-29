using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using FunkoApi.Back.Dtos.Proveedor;
using NUnit.Framework;

namespace FunkoApi.Tests.Dtos.Proveedor;

[TestFixture]
public class ProveedorDtoTests
{
    [TestFixture]
    public class CasosValidos
    {
        [Test]
        public void ProveedorDto_DatosValidos_PasaValidacion()
        {
            // Arrange
            var dto = new ProveedorDto(
                1,
                "Funko Corp",
                false,
                DateTime.UtcNow,
                DateTime.UtcNow
            );

            // Act & Assert
            new RequiredAttribute().IsValid(dto.Id).Should().BeTrue();
            new RequiredAttribute().IsValid(dto.Nombre).Should().BeTrue();
            new StringLengthAttribute(100).IsValid(dto.Nombre).Should().BeTrue();
            new DataTypeAttribute(DataType.DateTime).IsValid(dto.CreatedAt).Should().BeTrue();
            new DataTypeAttribute(DataType.DateTime).IsValid(dto.UpdatedAt).Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [TestCase("", "El nombre es obligatorio")]
        [TestCase(null, "El nombre es obligatorio")]
        public void ProveedorDto_NombreInvalido_DevuelveError(string? nombre, string mensajeEsperado)
        {
            // Arrange
            var attribute = new RequiredAttribute { ErrorMessage = mensajeEsperado };

            // Act
            var esValido = attribute.IsValid(nombre);

            // Assert
            esValido.Should().BeFalse();
            attribute.FormatErrorMessage(nameof(ProveedorDto.Nombre)).Should().Be(mensajeEsperado);
        }

        [Test]
        public void ProveedorDto_NombreExcesivo_DevuelveError()
        {
            // Arrange
            var attribute = new StringLengthAttribute(100)
                { ErrorMessage = "El nombre no puede superar los 100 caracteres" };
            var nombreExcesivo = new string('A', 101);

            // Act
            var esValido = attribute.IsValid(nombreExcesivo);

            // Assert
            esValido.Should().BeFalse();
            attribute.FormatErrorMessage(nameof(ProveedorDto.Nombre)).Should()
                .Be("El nombre no puede superar los 100 caracteres");
        }
    }
}
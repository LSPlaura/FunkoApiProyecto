using FluentAssertions;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Models;
using NUnit.Framework;

namespace FunkoApi.Tests.Mappers;

[TestFixture]
public class CategoriaMapperTests
{
    [TestFixture]
    public class CasosValidos
    {
        [Test]
        public void ToDto_EntidadValida_MapeaPropiedades()
        {
            // Arrange
            var fecha = DateTime.UtcNow;
            var model = new Categoria("Marvel", false, fecha, fecha, "cat00000001");

            // Act
            var result = model.ToDto();

            // Assert
            result.Should().BeEquivalentTo(model);
        }

        [Test]
        public void ToModel_DtoValido_MapeaPropiedades()
        {
            // Arrange
            var fecha = DateTime.UtcNow;
            var dto = new CategoriaDto("cat00000001", "Marvel", false, fecha, fecha);
            var esperado = new Categoria("Marvel", false, fecha, fecha, dto.Id);

            // Act
            var result = dto.ToModel();

            // Assert
            result.Should().Be(esperado);
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [Test]
        public void ToDto_EntidadNula_LanzaExcepcion()
        {
            // Act
            var action = () => ((Categoria)null!).ToDto();

            // Assert
            action.Should().Throw<NullReferenceException>();
        }
    }
}

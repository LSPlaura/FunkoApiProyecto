using FluentAssertions;
using FunkoApi.Back.Mappers;
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
            var model = TestData.Categoria();

            // Act
            var result = model.ToDto();

            // Assert
            result.Should().BeEquivalentTo(model);
        }

        [Test]
        public void ToModel_DtoValido_MapeaPropiedades()
        {
            // Arrange
            var dto = TestData.Categoria().ToDto();

            // Act
            var result = dto.ToModel();

            // Assert
            result.Should().Be(TestData.Categoria());
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
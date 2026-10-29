using FluentAssertions;
using FunkoApi.Back.Mappers;
using NUnit.Framework;

namespace FunkoApi.Tests.Mappers;

[TestFixture]
public class EntityMappersTests
{
    [TestFixture]
    public class CasosValidos
    {
        [Test]
        public void Categoria_ToDtoYToModel_ConservaTodosLosValores()
        {
            // Arrange
            var model = TestData.Categoria();

            // Act
            var dto = model.ToDto();
            var result = dto.ToModel();

            // Assert
            result.Should().Be(model);
            dto.Id.Should().Be(model.Id);
            dto.Nombre.Should().Be(model.Nombre);
        }

        [Test]
        public void Proveedor_ToDtoYToModel_ConservaTodosLosValores()
        {
            // Arrange
            var model = TestData.Proveedor();

            // Act
            var dto = model.ToDto();
            var result = dto.ToModel();

            // Assert
            result.Should().Be(model);
            dto.Should().BeEquivalentTo(model);
        }

        [Test]
        public void Funko_ToDtoYToModel_MapeaRelacionesAnidadas()
        {
            // Arrange
            var model = TestData.Funko();

            // Act
            var dto = model.ToDto();
            var result = dto.ToModel();

            // Assert
            dto.Should().BeEquivalentTo(model, options => options.ExcludingMissingMembers());
            result.Should().Be(model);
            result.Categoria.Should().Be(model.Categoria);
            result.Proveedor.Should().Be(model.Proveedor);
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [Test]
        public void Funko_ToDto_ConCategoriaNula_LanzaNullReferenceException()
        {
            // Arrange
            var model = TestData.Funko() with { Categoria = null! };

            // Act
            var action = () => model.ToDto();

            // Assert
            action.Should().Throw<NullReferenceException>();
        }
    }
}
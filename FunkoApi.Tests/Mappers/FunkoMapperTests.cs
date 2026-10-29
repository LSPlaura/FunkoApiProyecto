using FluentAssertions;
using FunkoApi.Back.Mappers;
using NUnit.Framework;

namespace FunkoApi.Tests.Mappers;

[TestFixture]
public class FunkoMapperTests
{
    [TestFixture]
    public class CasosValidos
    {
        [Test]
        public void ToDto_EntidadValida_MapeaRelaciones()
        {
            // Arrange
            var model = TestData.Funko();

            // Act
            var result = model.ToDto();

            // Assert
            result.Categoria.Nombre.Should().Be("Marvel");
            result.Proveedor.Nombre.Should().Be("Funko Corp");
        }

        [Test]
        public void ToModel_DtoValido_DevuelveEntidadEquivalente()
        {
            // Arrange
            var dto = TestData.Funko().ToDto();

            // Act
            var result = dto.ToModel();

            // Assert
            result.Should().Be(TestData.Funko());
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [Test]
        public void ToModel_DtoConProveedorNulo_LanzaExcepcion()
        {
            // Arrange
            var dto = TestData.Funko().ToDto() with { Proveedor = null! };

            // Act
            var action = () => dto.ToModel();

            // Assert
            action.Should().Throw<NullReferenceException>();
        }
    }
}
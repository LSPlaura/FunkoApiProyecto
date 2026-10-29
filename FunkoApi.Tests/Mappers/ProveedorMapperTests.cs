using FluentAssertions;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Mappers;
using NUnit.Framework;

namespace FunkoApi.Tests.Mappers;

[TestFixture]
public class ProveedorMapperTests
{
    [TestFixture]
    public class CasosValidos
    {
        [Test]
        public void ToDtoYToModel_ProveedorValido_ConservanDatos()
        {
            // Arrange
            var model = TestData.Proveedor();

            // Act
            var result = model.ToDto().ToModel();

            // Assert
            result.Should().Be(model);
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [Test]
        public void ToModel_DtoNulo_LanzaExcepcion()
        {
            // Act
            var action = () => ((ProveedorDto)null!).ToModel();

            // Assert
            action.Should().Throw<NullReferenceException>();
        }
    }
}
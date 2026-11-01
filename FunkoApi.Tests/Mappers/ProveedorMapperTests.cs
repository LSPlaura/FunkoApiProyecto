using FluentAssertions;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Models;
using MongoDB.Bson;
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
            var fecha = DateTime.UtcNow;
            var model = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);

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

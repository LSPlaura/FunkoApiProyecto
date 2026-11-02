using FluentAssertions;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Models;
using MongoDB.Bson;
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
            var fecha = DateTime.UtcNow;
            var model = new Categoria("Marvel", false, fecha, fecha, "cat00000001");

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
            var fecha = DateTime.UtcNow;
            var model = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);

            // Act
            var dto = model.ToDto();
            var result = dto.ToModel();

            // Assert
            result.Should().Be(model);
            dto.Id.Should().Be(model.Id.ToString());
            dto.Nombre.Should().Be(model.Nombre);
        }

        [Test]
        public void Funko_ToDto_MapeaRelacionesAnidadas()
        {
            // Arrange
            var fecha = DateTime.UtcNow;
            var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
            var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
            var model = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
                proveedor.Id, false, fecha, fecha);

            // Act
            var dto = model.ToDto();

            // Assert
            dto.Id.Should().Be(model.Id);
            dto.Categoria.Should().Be(model.Categoria!.ToDto());
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [Test]
        public void Funko_ToDto_ConCategoriaNula_RetornaCategoriaVacia()
        {
            // Arrange
            var fecha = DateTime.UtcNow;
            var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
            var model = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", "cat00000001", null,
                proveedor.Id, false, fecha, fecha);

            // Act
            var resultado = model.ToDto();

            // Assert
            resultado.Categoria.Id.Should().BeEmpty();
            resultado.Categoria.Nombre.Should().BeEmpty();
        }
    }
}

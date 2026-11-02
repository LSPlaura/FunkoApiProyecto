using FluentAssertions;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Models;
using MongoDB.Bson;
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
            var fecha = DateTime.UtcNow;
            var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
            var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
            var model = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
                proveedor.Id, false, fecha, fecha);

            // Act
            var result = model.ToDto();

            // Assert
            result.Categoria.Nombre.Should().Be("Marvel");
            result.Categoria.Id.Should().Be(categoria.Id);
        }

        [Test]
        public void ToDto_EntidadValida_ConservaCamposSimples()
        {
            // Arrange
            var fecha = DateTime.UtcNow;
            var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
            var proveedor = new Proveedor(ObjectId.Parse("000000000000000000000001"), "Funko Corp", false, fecha, fecha);
            var model = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, categoria,
                proveedor.Id, false, fecha, fecha);

            // Act
            var result = model.ToDto();

            // Assert
            result.Id.Should().Be(model.Id);
            result.Nombre.Should().Be(model.Nombre);
            result.Precio.Should().Be(model.Precio);
            result.Stock.Should().Be(model.Stock);
            result.Imagen.Should().Be(model.Imagen);
            result.IsDeleted.Should().Be(model.IsDeleted);
            result.CreatedAt.Should().Be(model.CreatedAt);
            result.UpdatedAt.Should().Be(model.UpdatedAt);
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [Test]
        public void ToDto_ModeloConCategoriaNula_RetornaCategoriaVacia()
        {
            // Arrange
            var fecha = DateTime.UtcNow;
            var categoria = new Categoria("Marvel", false, fecha, fecha, "cat00000001");
            var model = new Funko(1, "Iron Man", 14.99m, 10, "iron-man.png", categoria.Id, null,
                ObjectId.Parse("000000000000000000000001"), false, fecha, fecha);

            // Act
            var resultado = model.ToDto();

            // Assert
            resultado.Categoria.Id.Should().BeEmpty();
            resultado.Categoria.Nombre.Should().BeEmpty();
        }
    }
}

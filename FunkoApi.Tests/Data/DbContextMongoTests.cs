using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.Models;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Data;

[TestFixture]
public class DbContextMongoTests
{
    [Test]
    public void Proveedores_BaseDeDatos_ObtieneColeccionCorrecta()
    {
        // Arrange
        var databaseMock = new Mock<IMongoDatabase>();
        var coleccionMock = new Mock<IMongoCollection<Proveedor>>();
        databaseMock.Setup(d => d.GetCollection<Proveedor>("proveedores", null)).Returns(coleccionMock.Object);
        var contexto = new DbContextMongo(databaseMock.Object);

        // Act
        var resultado = contexto.Proveedores;

        // Assert
        resultado.Should().BeSameAs(coleccionMock.Object);
        databaseMock.Verify(d => d.GetCollection<Proveedor>("proveedores", null), Times.Once);
    }
}

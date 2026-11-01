using System.Text.Json;
using FluentAssertions;
using FunkoApi.Back.Conventers;
using MongoDB.Bson;
using NUnit.Framework;

namespace FunkoApi.Tests.Conventers;

[TestFixture]
public class ObjectIdJsonConverterTests
{
    [TestCase("\"000000000000000000000001\"", "000000000000000000000001")]
    [TestCase("\"\"", "000000000000000000000000")]
    [TestCase("null", "000000000000000000000000")]
    public void Read_JsonValido_RetornaObjectId(string json, string esperado)
    {
        // Arrange
        var opciones = new JsonSerializerOptions();
        opciones.Converters.Add(new ObjectIdJsonConverter());

        // Act
        var resultado = JsonSerializer.Deserialize<ObjectId>(json, opciones);

        // Assert
        resultado.Should().Be(ObjectId.Parse(esperado));
    }

    [Test]
    public void Write_ObjectId_SerializaComoCadena()
    {
        // Arrange
        var id = ObjectId.Parse("000000000000000000000001");
        var opciones = new JsonSerializerOptions();
        opciones.Converters.Add(new ObjectIdJsonConverter());

        // Act
        var resultado = JsonSerializer.Serialize(id, opciones);

        // Assert
        resultado.Should().Be("\"000000000000000000000001\"");
    }
}

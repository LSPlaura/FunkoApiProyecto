using System.Text.Json;
using FluentAssertions;
using FunkoApi.Back.Conventers;
using MongoDB.Bson;
using NUnit.Framework;

namespace FunkoApi.Tests.Conventers;

[TestFixture]
public class ObjectIdJsonConverterTests
{
    private JsonSerializerOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new ObjectIdJsonConverter());
    }

    [TestCase("507f1f77bcf86cd799439011")]
    [TestCase("000000000000000000000001")]
    public void Read_ObjectIdValido_DevuelveIdentificador(string valor)
    {
        // Arrange
        var json = $"\"{valor}\"";

        // Act
        var resultado = JsonSerializer.Deserialize<ObjectId>(json, _options);

        // Assert
        resultado.Should().Be(ObjectId.Parse(valor));
    }

    [TestCase("\"\"")]
    [TestCase("null")]
    public void Read_SinValor_DevuelveObjectIdVacio(string json)
    {
        // Act
        var resultado = JsonSerializer.Deserialize<ObjectId>(json, _options);

        // Assert
        resultado.Should().Be(ObjectId.Empty);
    }

    [Test]
    public void Write_ObjectId_EscribeCadenaHexadecimal()
    {
        // Arrange
        var id = ObjectId.Parse("507f1f77bcf86cd799439011");

        // Act
        var resultado = JsonSerializer.Serialize(id, _options);

        // Assert
        resultado.Should().Be("\"507f1f77bcf86cd799439011\"");
    }
}

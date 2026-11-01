using FluentAssertions;
using FunkoApi.Back.Factory;
using NUnit.Framework;

namespace FunkoApi.Tests.Factory;

[TestFixture]
public class ConnectionStringGeneratorTests
{
    [Test]
    public void BuildPostgres_ComponentesValidos_ConstruyeCadena()
    {
        // Arrange
        const string host = "localhost";

        // Act
        var resultado = ConnectionStringGenerator.BuildPostgres(host, 5432, "pruebas", "test", "test");

        // Assert
        resultado.Should().Be("Host=localhost;Port=5432;Database=pruebas;Username=test;Password=test");
    }

    [Test]
    public void BuildMongoDb_AuthSourcePredeterminado_ConstruyeCadena()
    {
        // Arrange
        const string host = "localhost";

        // Act
        var resultado = ConnectionStringGenerator.BuildMongoDb(host, 27017, "pruebas", "test", "test");

        // Assert
        resultado.Should().Be("mongodb://test:test@localhost:27017/pruebas?authSource=admin&retryWrites=true&retryReads=true");
    }

    [TestCase(null, "localhost:6379")]
    [TestCase("", "localhost:6379")]
    [TestCase(" ", "localhost:6379")]
    [TestCase("test", "localhost:6379,password=test")]
    public void BuildRedis_ConYSinPassword_ConstruyeCadena(string? password, string esperado)
    {
        // Arrange
        const string host = "localhost";

        // Act
        var resultado = ConnectionStringGenerator.BuildRedis(host, 6379, password);

        // Assert
        resultado.Should().Be(esperado);
    }
}

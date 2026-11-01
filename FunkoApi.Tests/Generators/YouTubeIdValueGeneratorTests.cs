using FluentAssertions;
using FunkoApi.Back.Generators;
using NUnit.Framework;

namespace FunkoApi.Tests.Generators;

[TestFixture]
public class YouTubeIdValueGeneratorTests
{
    [Test]
    public void Next_Generador_CreaIdentificadorPermanenteDeOnceCaracteres()
    {
        // Arrange
        var generador = new YouTubeIdValueGenerator();

        // Act
        var resultado = generador.Next(null!);

        // Assert
        resultado.Should().MatchRegex("^[A-Za-z0-9_-]{11}$");
        generador.GeneratesTemporaryValues.Should().BeFalse();
    }
}

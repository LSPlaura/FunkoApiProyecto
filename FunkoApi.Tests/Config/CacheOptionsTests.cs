using FluentAssertions;
using FunkoApi.Back.Config;
using NUnit.Framework;

namespace FunkoApi.Tests.Config;

[TestFixture]
public class CacheOptionsTests
{
    [TestCase(30, 10)]
    [TestCase(5, 2)]
    public void Expiration_MinutosConfigurados_ConvierteATimeSpan(int absoluta, int deslizante)
    {
        // Arrange
        var opciones = new CacheOptions { DefaultExpirationMinutes = absoluta, SlidingExpirationMinutes = deslizante };

        // Act
        var resultado = (opciones.DefaultExpiration, opciones.SlidingExpiration);

        // Assert
        resultado.Should().Be((TimeSpan.FromMinutes(absoluta), TimeSpan.FromMinutes(deslizante)));
    }
}

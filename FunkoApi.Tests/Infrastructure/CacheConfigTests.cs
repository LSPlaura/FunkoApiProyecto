using FluentAssertions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Infrastructure;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using StackExchange.Redis;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class CacheConfigTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void AddCache_ConfiguracionAusenteOPersonalizada_RegistraRedisYOpciones(bool personalizada)
    {
        // Arrange
        var valores = personalizada ? new Dictionary<string, string?>
        {
            ["Redis:Host"] = "redis-test", ["Redis:Port"] = "6380", ["Redis:Password"] = "test",
            ["Redis:RedisInstanceName"] = "tests:", ["Redis:DefaultExpirationMinutes"] = "5"
        } : new Dictionary<string, string?>();
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var servicios = new ServiceCollection();

        // Act
        var resultado = servicios.AddCache(configuracion);
        using var provider = servicios.BuildServiceProvider();
        var opciones = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        var cacheOptions = provider.GetRequiredService<IOptions<CacheOptions>>().Value;

        // Assert
        resultado.Should().BeSameAs(servicios);
        opciones.Configuration.Should().Be(personalizada ? "redis-test:6380,password=test" : "localhost:6379");
        opciones.InstanceName.Should().Be(personalizada ? "tests:" : "funkoapi:");
        cacheOptions.DefaultExpirationMinutes.Should().Be(personalizada ? 5 : 30);
        servicios.Should().Contain(d => d.ServiceType == typeof(ICacheService) && d.ImplementationType == typeof(RedisCacheService));
        servicios.Should().Contain(d => d.ServiceType == typeof(IConnectionMultiplexer) && d.Lifetime == ServiceLifetime.Singleton);
    }

    [Test]
    public void AddCache_SinRedisDisponible_LaFabricaDelMultiplexerFallaAlConectar()
    {
        // Arrange
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Redis:Host"] = "127.0.0.1", ["Redis:Port"] = "1"
        }).Build();
        var servicios = new ServiceCollection();
        servicios.AddCache(configuracion);
        using var provider = servicios.BuildServiceProvider();

        // Act
        var crearMultiplexer = () => provider.GetRequiredService<IConnectionMultiplexer>();

        // Assert
        crearMultiplexer.Should().Throw<RedisConnectionException>();
    }
}

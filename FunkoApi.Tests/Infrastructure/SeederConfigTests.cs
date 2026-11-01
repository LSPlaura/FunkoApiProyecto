using FluentAssertions;
using FunkoApi.Back.Data.Seed.Efc;
using FunkoApi.Back.Data.Seed.Mongo;
using FunkoApi.Back.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class SeederConfigTests
{
    [Test]
    public void AddSeeders_Siempre_RegistraAmbosSeedersScoped()
    {
        // Arrange
        var servicios = new ServiceCollection();
        var configuracion = new ConfigurationBuilder().Build();

        // Act
        var resultado = servicios.AddSeeders(configuracion);

        // Assert
        resultado.Should().BeSameAs(servicios);
        servicios.Should().Contain(d => d.ServiceType == typeof(EfcSeeder) && d.Lifetime == ServiceLifetime.Scoped);
        servicios.Should().Contain(d => d.ServiceType == typeof(MongoSeeder) && d.Lifetime == ServiceLifetime.Scoped);
    }
}

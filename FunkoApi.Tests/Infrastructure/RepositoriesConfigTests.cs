using FluentAssertions;
using FunkoApi.Back.Infrastructure;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class RepositoriesConfigTests
{
    [Test]
    public void AddRepositories_Siempre_RegistraRepositoriosScoped()
    {
        // Arrange
        var servicios = new ServiceCollection();
        var configuracion = new ConfigurationBuilder().Build();

        // Act
        var resultado = servicios.AddRepositories(configuracion);

        // Assert
        resultado.Should().BeSameAs(servicios);
        var categoria = servicios.Single(d => d.ServiceType == typeof(ICategoriaRepository));
        categoria.Lifetime.Should().Be(ServiceLifetime.Scoped);
        categoria.ImplementationType.Should().NotBeNull();
        categoria.ImplementationType!.Name.Should().Be("CategoriaRepository");
        typeof(ICategoriaRepository).IsAssignableFrom(categoria.ImplementationType).Should().BeTrue();
        servicios.Should().Contain(d => d.ServiceType == typeof(IFunkoRepository)
            && d.ImplementationType == typeof(FunkoRepository) && d.Lifetime == ServiceLifetime.Scoped);
        servicios.Should().Contain(d => d.ServiceType == typeof(IProveedorRepository) && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Test]
    public void AddRepositories_Proveedor_DeberiaRegistrarImplementacionConcreta()
    {
        // Arrange
        var servicios = new ServiceCollection();
        var configuracion = new ConfigurationBuilder().Build();

        // Act
        servicios.AddRepositories(configuracion);

        // Assert
        servicios.Single(d => d.ServiceType == typeof(IProveedorRepository)).ImplementationType
            .Should().Be(typeof(ProveedorMongoRepository));
    }
}

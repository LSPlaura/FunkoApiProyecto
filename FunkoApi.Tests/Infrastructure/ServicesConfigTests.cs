using FluentAssertions;
using FunkoApi.Back.Infrastructure;
using FunkoApi.Back.Services.Categoria;
using FunkoApi.Back.Services.Funko;
using FunkoApi.Back.Services.Get.Categoria;
using FunkoApi.Back.Services.Auth;
using FunkoApi.Back.Services.Auth.Common;
using FunkoApi.Back.Services.Proveedor;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class ServicesConfigTests
{
    [Test]
    public void AddServices_Siempre_RegistraServiciosScoped()
    {
        // Arrange
        var servicios = new ServiceCollection();

        // Act
        var resultado = servicios.AddServices();

        // Assert
        resultado.Should().BeSameAs(servicios);
        servicios.Should().Contain(d => d.ServiceType == typeof(ICategoriaService)
            && d.ImplementationType == typeof(CategoriaService) && d.Lifetime == ServiceLifetime.Scoped);
        servicios.Should().Contain(d => d.ServiceType == typeof(IFunkoService)
            && d.ImplementationType == typeof(FunkoServices) && d.Lifetime == ServiceLifetime.Scoped);
        servicios.Should().Contain(d => d.ServiceType == typeof(IAuthService)
            && d.ImplementationType == typeof(AuthService) && d.Lifetime == ServiceLifetime.Scoped);
        servicios.Should().Contain(d => d.ServiceType == typeof(IProveedorService)
            && d.ImplementationType == typeof(ProveedorService) && d.Lifetime == ServiceLifetime.Scoped);
    }
}

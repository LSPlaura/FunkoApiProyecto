using FluentAssertions;
using FunkoApi.Back.Infrastructure;
using FunkoApi.Back.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class StorageConfigTests
{
    [Test]
    public void AddStorage_Siempre_RegistraServicioScoped()
    {
        // Arrange
        var servicios = new ServiceCollection();

        // Act
        var resultado = servicios.AddStorage();

        // Assert
        resultado.Should().BeSameAs(servicios);
        servicios.Should().Contain(d => d.ServiceType == typeof(IStorageService)
            && d.ImplementationType == typeof(FileSystemStorageService) && d.Lifetime == ServiceLifetime.Scoped);
    }
}

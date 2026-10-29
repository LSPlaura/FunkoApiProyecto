using FluentAssertions;
using FunkoApi.Back.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace FunkoApi.Tests.Config;

[TestFixture]
public class ApiConfigTests
{
    [TestFixture]
    public class CasosValidos
    {
        [Test]
        public void ApiConfig_ValoresPorDefecto_SonLosEsperados()
        {
            // Arrange
            var config = new ApiConfig();

            // Act
            var result = (config.Nombre, config.Mensaje);

            // Assert
            result.Should().Be(("Funko API", "Modo producción"));
        }

        [Test]
        public void ApiConfig_BindeadaDesdeSeccion_CargaValores()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Api:Nombre"] = "Funko Test",
                    ["Api:Mensaje"] = "Pruebas"
                })
                .Build();
            var services = new ServiceCollection();
            services.Configure<ApiConfig>(configuration.GetSection("Api"));

            // Act
            using var provider = services.BuildServiceProvider();
            var result = provider.GetRequiredService<IOptions<ApiConfig>>().Value;

            // Assert
            result.Nombre.Should().Be("Funko Test");
            result.Mensaje.Should().Be("Pruebas");
        }
    }

    [TestFixture]
    public class CasosInvalidos
    {
        [Test]
        public void ApiConfig_SeccionAusente_MantieneValoresPorDefecto()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var services = new ServiceCollection();
            services.AddOptions<ApiConfig>().Bind(configuration.GetSection("Api"));

            // Act
            using var provider = services.BuildServiceProvider();
            var result = provider.GetRequiredService<IOptions<ApiConfig>>().Value;

            // Assert
            result.Nombre.Should().Be("Funko API");
            result.Mensaje.Should().Be("Modo producción");
        }
    }
}
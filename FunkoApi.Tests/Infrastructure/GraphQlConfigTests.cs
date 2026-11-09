using FluentAssertions;
using FunkoApi.Back.Infrastructure;
using FunkoApi.Back.Repositories.Funko;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class GraphQlConfigTests
{
    [TestCase("Development", true)]
    [TestCase("Production", false)]
    [TestCase("Staging", false)]
    public async Task AddGraphQlConfig_ErrorEnResolver_MuestraDetallesSoloEnDevelopment(string entorno, bool muestraDetalles)
    {
        // Arrange
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(entorno);
        var repositoryMock = new Mock<IFunkoRepository>();
        var error = new InvalidOperationException("Detalle privado de prueba GraphQL");
        repositoryMock.Setup(r => r.GetById(1)).ThrowsAsync(error);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddSingleton(repositoryMock.Object);

        // Act
        services.AddGraphQlConfig(environment.Object);
        await using var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        await using var resultado = await executor.ExecuteAsync("{ funko(id: 1) { nombre } }");
        var json = resultado.ToJson();

        // Assert
        json.Should().Contain("errors");
        json.Contains(error.Message).Should().Be(muestraDetalles);
        repositoryMock.Verify(r => r.GetById(1), Times.Once);
        repositoryMock.Verify(r => r.GetAll(), Times.Never);
    }

    [Test]
    public async Task AddGraphQlConfig_Production_RechazaIntrospeccion()
    {
        // Arrange
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddGraphQlConfig(environment.Object);
        await using var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();

        // Act
        await using var resultado = await executor.ExecuteAsync("{ __schema { queryType { name } } }");

        // Assert
        resultado.ToJson().Should().Contain("HC0046");
    }

    [Test]
    public async Task MapGraphQlEndpoints_RegistraRutaHttpGraphQl()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.Services.AddGraphQlConfig(builder.Environment);
        await using var app = builder.Build();

        // Act
        app.MapGraphQlEndpoints();
        var rutas = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText).ToList();

        // Assert
        rutas.Should().Contain("/graphql/{**slug}");
        rutas.Should().Contain("/graphql/ws");
    }
}

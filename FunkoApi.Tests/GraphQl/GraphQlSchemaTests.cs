using System.Text.Json;
using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.GraphQl.Queries;
using FunkoApi.Back.Infrastructure;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Funko;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ObjectId = MongoDB.Bson.ObjectId;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.GraphQl;

[TestFixture]
public class GraphQlSchemaTests
{
    private ServiceProvider _provider = null!;
    private IRequestExecutor _executor = null!;
    private Mock<IFunkoRepository> _repositoryMock = null!;

    [SetUp]
    public async Task SetUp()
    {
        var database = $"GraphQlSchema_{Guid.NewGuid()}";
        _repositoryMock = new Mock<IFunkoRepository>();
        var fecha = DateTime.UtcNow;
        var categoria = new Categoria("Marvel", false, fecha, fecha, "marvel00001");
        var funkos = new[]
        {
            new Funko(1, "Iron Man", 15m, 3, "iron.png", categoria.Id, categoria,
                ObjectId.Parse("000000000000000000000001"), false, fecha, fecha),
            new Funko(2, "Thor", 30m, 0, "thor.png", categoria.Id, categoria,
                ObjectId.Parse("000000000000000000000002"), true, fecha, fecha)
        };
        _repositoryMock.Setup(r => r.GetAll()).ReturnsAsync(funkos);
        _repositoryMock.Setup(r => r.GetById(It.IsAny<long>()))
            .ReturnsAsync((long id) => funkos.FirstOrDefault(f => f.Id == id));
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddSingleton(_repositoryMock.Object);
        services.AddDbContextFactory<AppDbContext>(o => o.UseInMemoryDatabase(database));
        services.AddGraphQlConfig(environment.Object);
        _provider = services.BuildServiceProvider();
        await using (var context = await _provider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync())
        {
            context.Categorias.Add(categoria);
            await context.SaveChangesAsync();
        }
        _executor = await _provider.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_provider is not null) await _provider.DisposeAsync();
    }

    [Test]
    public async Task Query_Funkos_DevuelveSoloCamposSolicitados()
    {
        // Arrange
        const string query = "{ funkos { nodes { nombre precio } } }";

        // Act
        using var json = await EjecutarAsync(query);

        // Assert
        SinErrores(json);
        var nodes = json.RootElement.GetProperty("data").GetProperty("funkos").GetProperty("nodes");
        nodes.EnumerateArray().Select(n => n.GetProperty("nombre").GetString()).Should().Equal("Iron Man", "Thor");
        nodes[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("nombre", "precio");
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task Query_FiltroOrdenYPaginacion_SeAplicanAntesDeResponder()
    {
        // Arrange
        const string query = "{ funkos(where: { precio: { gt: 10 } }, order: { precio: DESC }, first: 1) { nodes { nombre } pageInfo { hasNextPage } } }";

        // Act
        using var json = await EjecutarAsync(query);

        // Assert
        SinErrores(json);
        var data = json.RootElement.GetProperty("data").GetProperty("funkos");
        data.GetProperty("nodes").GetArrayLength().Should().Be(1);
        data.GetProperty("nodes")[0].GetProperty("nombre").GetString().Should().Be("Thor");
        data.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean().Should().BeTrue();
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task Query_FiltroSinCoincidencias_DevuelveListaVacia()
    {
        // Arrange
        const string query = "{ funkos(where: { precio: { gt: 100 } }) { nodes { nombre } } }";

        // Act
        using var json = await EjecutarAsync(query);

        // Assert
        SinErrores(json);
        json.RootElement.GetProperty("data").GetProperty("funkos").GetProperty("nodes").GetArrayLength().Should().Be(0);
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [TestCase(1)]
    [TestCase(999)]
    public async Task Query_PorId_DevuelveFunkoONull(int id)
    {
        // Arrange
        var query = $"{{ funko(id: {id}) {{ nombre }} }}";

        // Act
        using var json = await EjecutarAsync(query);

        // Assert
        SinErrores(json);
        var data = json.RootElement.GetProperty("data").GetProperty("funko");
        if (id == 1) data.GetProperty("nombre").GetString().Should().Be("Iron Man");
        else data.ValueKind.Should().Be(JsonValueKind.Null);
        _repositoryMock.Verify(r => r.GetById(id), Times.Once);
    }

    [Test]
    public async Task Query_CategoriaYDisponibilidad_EjecutaResolversRegistrados()
    {
        // Arrange
        const string query = "{ funkos { nodes { nombre estaDisponible categoria { nombre } } } }";

        // Act
        using var json = await EjecutarAsync(query);

        // Assert
        SinErrores(json);
        var nodes = json.RootElement.GetProperty("data").GetProperty("funkos").GetProperty("nodes");
        nodes[0].GetProperty("categoria").GetProperty("nombre").GetString().Should().Be("Marvel");
        nodes[0].GetProperty("estaDisponible").GetBoolean().Should().BeTrue();
        nodes[1].GetProperty("estaDisponible").GetBoolean().Should().BeFalse();
        _repositoryMock.Verify(r => r.GetAll(), Times.Once);
    }

    [Test]
    public async Task Query_CampoInexistente_RechazaAntesDeConsultarRepositorio()
    {
        // Arrange
        const string query = "{ campoInexistente }";

        // Act
        using var json = await EjecutarAsync(query);

        // Assert
        json.RootElement.GetProperty("errors").GetArrayLength().Should().BeGreaterThan(0);
        _repositoryMock.Verify(r => r.GetAll(), Times.Never);
        _repositoryMock.Verify(r => r.GetById(It.IsAny<long>()), Times.Never);
    }

    [Test]
    public void Schema_TiposConfigurados_ConservaNombresDescripcionesYTipos()
    {
        // Arrange
        var schema = _executor.Schema;

        // Act
        var funko = schema.Types.GetType<ObjectType>("Funko");
        var categoria = schema.Types.GetType<ObjectType>("Categoria");
        var proveedor = schema.Types.GetType<ObjectType>("Proveedor");

        // Assert
        funko.Description.Should().Be("Funko de colección");
        categoria.Description.Should().Be("Categoría de funkos");
        proveedor.Description.Should().Be("Proveedor de funkos");
        funko.Fields["nombre"].Type.ToString().Should().Be("String!");
        funko.Fields["precio"].Type.ToString().Should().Be("Decimal!");
        funko.Fields["stock"].Type.ToString().Should().Be("Int!");
        categoria.Fields["id"].Type.ToString().Should().Be("ID!");
        proveedor.Fields["nombre"].Type.ToString().Should().Be("String!");
        _repositoryMock.Verify(r => r.GetAll(), Times.Never);
    }

    private async Task<JsonDocument> EjecutarAsync(string query)
    {
        await using var result = await _executor.ExecuteAsync(query);
        return JsonDocument.Parse(result.ToJson());
    }

    private static void SinErrores(JsonDocument json) =>
        json.RootElement.TryGetProperty("errors", out _).Should().BeFalse(json.RootElement.ToString());
}

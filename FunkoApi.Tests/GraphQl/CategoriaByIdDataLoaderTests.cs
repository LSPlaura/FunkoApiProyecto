using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.GraphQl.DataLoaders;
using FunkoApi.Back.GraphQl.Resolvers;
using FunkoApi.Back.Models;
using GreenDonut;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.GraphQl;

[TestFixture]
public class CategoriaByIdDataLoaderTests
{
    private DbContextOptions<AppDbContext> _options = null!;
    private Mock<IDbContextFactory<AppDbContext>> _factoryMock = null!;
    private Mock<ILogger<CategoriaByIdDataLoader>> _loggerMock = null!;

    [SetUp]
    public async Task SetUp()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"GraphQlCategorias_{Guid.NewGuid()}").Options;
        await using var context = new AppDbContext(_options);
        var fecha = DateTime.UtcNow;
        context.Categorias.AddRange(
            new Categoria("Marvel", false, fecha, fecha, "marvel00001"),
            new Categoria("Anime", false, fecha, fecha, "anime000001"));
        await context.SaveChangesAsync();
        _factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        _factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(_options));
        _loggerMock = new Mock<ILogger<CategoriaByIdDataLoader>>();
    }

    [TestCase("varias")]
    [TestCase("vacio")]
    [TestCase("inexistente")]
    [TestCase("repetidas")]
    public async Task LoadBatchAsync_ClavesSolicitadas_DevuelveSoloCategoriasEncontradas(string caso)
    {
        // Arrange
        var keys = caso switch
        {
            "varias" => new[] { "marvel00001", "anime000001", "no-existe" },
            "repetidas" => new[] { "marvel00001", "marvel00001" },
            "inexistente" => new[] { "no-existe" },
            _ => Array.Empty<string>()
        };
        var loader = CrearLoader();
        using var cancellation = new CancellationTokenSource();

        // Act
        var resultado = await loader.RunBatchAsync(keys, cancellation.Token);

        // Assert
        resultado.Keys.Should().BeEquivalentTo(keys.Distinct().Where(k => k != "no-existe"));
        if (resultado.ContainsKey("marvel00001")) resultado["marvel00001"].Nombre.Should().Be("Marvel");
        _factoryMock.Verify(f => f.CreateDbContextAsync(cancellation.Token), Times.Once);
        _loggerMock.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public async Task LoadBatchAsync_FactoryFalla_PropagaError()
    {
        // Arrange
        var error = new InvalidOperationException("No se pudo crear contexto");
        _factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ThrowsAsync(error);
        var loader = CrearLoader();

        // Act
        Func<Task> act = async () => await loader.RunBatchAsync(new[] { "marvel00001" }, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(error);
        _factoryMock.Verify(f => f.CreateDbContextAsync(CancellationToken.None), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Resolver_GetCategoria_DevuelveCategoriaOValorPorDefecto(bool existe)
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var funko = new Funko(1, "Iron Man", 15m, 0, "imagen.png", existe ? "marvel00001" : "no-existe",
            null, ObjectId.Empty, false, fecha, fecha);
        var resolver = new FunkoResolver();
        var loader = CrearLoader();
        var antes = DateTime.UtcNow;

        // Act
        var resultado = await resolver.GetCategoria(funko, loader);

        // Assert
        resultado.Nombre.Should().Be(existe ? "Marvel" : "");
        resultado.Id.Should().Be(existe ? "marvel00001" : "");
        if (!existe)
        {
            resultado.IsDeleted.Should().BeFalse();
            resultado.CreatedAt.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        }
        _factoryMock.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoadAsync_ConCacheYClaveRepetida_ReutilizaCategoriaSinOtraConsulta()
    {
        // Arrange
        var loader = new TestCategoriaDataLoader(_factoryMock.Object, _loggerMock.Object,
            new DataLoaderOptions { Cache = new PromiseCache(100) });

        // Act
        var primero = await loader.LoadAsync("marvel00001");
        var segundo = await loader.LoadAsync("marvel00001");

        // Assert
        primero.Should().NotBeNull();
        segundo.Should().BeSameAs(primero);
        _factoryMock.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(false, 0, true)]
    [TestCase(false, 5, true)]
    [TestCase(true, 0, false)]
    [TestCase(true, 5, false)]
    public void Resolver_EstaDisponible_UtilizaBorradoLogicoComoDefineElCodigo(bool eliminado, int stock, bool esperado)
    {
        // Arrange
        var funko = new Funko(1, "Iron Man", 15m, stock, "imagen.png", "marvel00001", null,
            ObjectId.Empty, eliminado, DateTime.UtcNow, DateTime.UtcNow);
        var resolver = new FunkoResolver();

        // Act
        var resultado = resolver.GetEstaDisponible(funko);

        // Assert
        resultado.Should().Be(esperado);
    }

    private TestCategoriaDataLoader CrearLoader() => new(_factoryMock.Object, _loggerMock.Object);

    private class TestCategoriaDataLoader(
        IDbContextFactory<AppDbContext> factory,
        ILogger<CategoriaByIdDataLoader> logger,
        DataLoaderOptions? options = null)
        : CategoriaByIdDataLoader(AutoBatchScheduler.Default, options ?? new DataLoaderOptions(), factory, logger)
    {
        public Task<IReadOnlyDictionary<string, Categoria>> RunBatchAsync(
            IReadOnlyList<string> keys, CancellationToken token) => base.LoadBatchAsync(keys, token);
    }
}

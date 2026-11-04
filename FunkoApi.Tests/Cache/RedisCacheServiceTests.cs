using System.Text.Json;
using FluentAssertions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MongoDB.Bson;
using NUnit.Framework;
using StackExchange.Redis;

namespace FunkoApi.Tests.Cache;

[TestFixture]
public class RedisCacheServiceTests
{
    private Mock<IDistributedCache> _cacheMock = null!;
    private Mock<IConnectionMultiplexer> _redisMock = null!;
    private Mock<IDatabase> _databaseMock = null!;
    private RedisCacheService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _redisMock.Setup(r => r.GetDatabase(-1, null)).Returns(_databaseMock.Object);
        _service = new RedisCacheService(_cacheMock.Object, _redisMock.Object, Options.Create(new CacheOptions()),
            new Mock<ILogger<RedisCacheService>>().Object);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetAsync_ConYSinDatos_RetornaValorONull(bool existe)
    {
        // Arrange
        var bytes = existe ? JsonSerializer.SerializeToUtf8Bytes("valor") : null;
        _cacheMock.Setup(c => c.GetAsync("clave", default)).ReturnsAsync(bytes);

        // Act
        var resultado = await _service.GetAsync<string>("clave");

        // Assert
        resultado.Should().Be(existe ? "valor" : null);
        _cacheMock.Verify(c => c.GetAsync("clave", default), Times.Once);
    }

    [Test]
    public async Task GetAsync_JsonInvalido_PropagaJsonException()
    {
        // Arrange
        _cacheMock.Setup(c => c.GetAsync("clave", default)).ReturnsAsync(new byte[] { 0 });
        Func<Task> accion = () => _service.GetAsync<string>("clave");

        // Act
        var resultado = await accion.Should().ThrowAsync<JsonException>();

        // Assert
        resultado.Which.Should().NotBeNull();
        _cacheMock.Verify(c => c.GetAsync("clave", default), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SetAsync_ConYSinExpiracion_SerializaYConfiguraTiempos(bool personalizada)
    {
        // Arrange
        TimeSpan? expiracion = personalizada ? TimeSpan.FromMinutes(5) : null;
        var bytes = JsonSerializer.SerializeToUtf8Bytes("valor");

        // Act
        await _service.SetAsync("clave", "valor", expiracion);

        // Assert
        _cacheMock.Verify(c => c.SetAsync("clave", It.Is<byte[]>(b => b.SequenceEqual(bytes)),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == (expiracion ?? TimeSpan.FromMinutes(30))
                && o.SlidingExpiration == TimeSpan.FromMinutes(10)), default), Times.Once);
    }

    [TestCase("507f1f77bcf86cd799439011")]
    [TestCase("000000000000000000000001")]
    public async Task SetAsyncYGetAsync_Proveedor_ConservaObjectId(string id)
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var proveedor = new Proveedor(ObjectId.Parse(id), "Proveedor E2E", false, fecha, fecha);
        var clave = "proveedor:" + id;
        byte[]? datosCache = null;
        _cacheMock.Setup(c => c.SetAsync(clave, It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), default))
            .Returns((string key, byte[] value, DistributedCacheEntryOptions opciones, CancellationToken ct) =>
            {
                datosCache = value;
                return Task.CompletedTask;
            });
        _cacheMock.Setup(c => c.GetAsync(clave, default))
            .Returns(() => Task.FromResult(datosCache));

        // Act
        await _service.SetAsync(clave, proveedor);
        var resultado = await _service.GetAsync<Proveedor>(clave);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(proveedor.Id);
        resultado.Should().Be(proveedor);
        _cacheMock.Verify(c => c.SetAsync(clave, It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
        _cacheMock.Verify(c => c.GetAsync(clave, default), Times.Once);
        _redisMock.Verify(r => r.GetDatabase(-1, null), Times.Never);
    }

    [Test]
    public async Task RemoveAsync_ClaveValida_DelegaEnCache()
    {
        // Arrange
        const string clave = "clave";

        // Act
        await _service.RemoveAsync(clave);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(clave, default), Times.Once);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ExistsAsync_ConYSinDatos_RetornaBooleano(bool existe)
    {
        // Arrange
        _cacheMock.Setup(c => c.GetAsync("clave", default)).ReturnsAsync(existe ? new byte[] { 1 } : null);

        // Act
        var resultado = await _service.ExistsAsync("clave");

        // Assert
        resultado.Should().Be(existe);
        _cacheMock.Verify(c => c.GetAsync("clave", default), Times.Once);
    }

    [Test]
    public async Task GetOrSetAsync_EnCache_NoEjecutaFactoryNiSolicitaLock()
    {
        // Arrange
        _cacheMock.Setup(c => c.GetAsync("clave", default)).ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes("guardado"));
        var factoryMock = new Mock<Func<Task<string?>>>();

        // Act
        var resultado = await _service.GetOrSetAsync("clave", factoryMock.Object);

        // Assert
        resultado.Should().Be("guardado");
        factoryMock.Verify(f => f(), Times.Never);
        _cacheMock.Verify(c => c.GetAsync("clave", default), Times.Once);
        _redisMock.Verify(r => r.GetDatabase(-1, null), Times.Never);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetOrSetAsync_LockGanado_GuardaSoloValoresNoNulosYLiberaLock(bool conValor)
    {
        // Arrange
        _databaseMock.Setup(d => d.StringSetAsync("lock:clave", "1", TimeSpan.FromSeconds(10), When.NotExists)).ReturnsAsync(true);
        var factoryMock = new Mock<Func<Task<string?>>>();
        factoryMock.Setup(f => f()).ReturnsAsync(conValor ? "nuevo" : null);

        // Act
        var resultado = await _service.GetOrSetAsync("clave", factoryMock.Object);

        // Assert
        resultado.Should().Be(conValor ? "nuevo" : null);
        factoryMock.Verify(f => f(), Times.Once);
        _cacheMock.Verify(c => c.GetAsync("clave", default), Times.Once);
        _databaseMock.Verify(d => d.StringSetAsync("lock:clave", "1", TimeSpan.FromSeconds(10), When.NotExists), Times.Once);
        _cacheMock.Verify(c => c.SetAsync("clave", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), conValor ? Times.Once() : Times.Never());
        _databaseMock.Verify(d => d.KeyDeleteAsync(new RedisKey("lock:clave")), Times.Once);
    }

    [Test]
    public async Task GetOrSetAsync_LockOcupado_ReconsultaSinEjecutarFactory()
    {
        // Arrange
        _cacheMock.SetupSequence(c => c.GetAsync("clave", default))
            .ReturnsAsync((byte[]?)null).ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes("otro proceso"));
        _databaseMock.Setup(d => d.StringSetAsync("lock:clave", "1", TimeSpan.FromSeconds(10), When.NotExists)).ReturnsAsync(false);
        var factoryMock = new Mock<Func<Task<string?>>>();

        // Act
        var resultado = await _service.GetOrSetAsync("clave", factoryMock.Object);

        // Assert
        resultado.Should().Be("otro proceso");
        factoryMock.Verify(f => f(), Times.Never);
        _cacheMock.Verify(c => c.GetAsync("clave", default), Times.Exactly(2));
        _databaseMock.Verify(d => d.StringSetAsync("lock:clave", "1", TimeSpan.FromSeconds(10), When.NotExists), Times.Once);
        _databaseMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>()), Times.Never);
    }

    [Test]
    public async Task GetOrSetAsync_FactoryFalla_PropagaErrorYLiberaLocks()
    {
        // Arrange
        _databaseMock.Setup(d => d.StringSetAsync("lock:clave", "1", TimeSpan.FromSeconds(10), When.NotExists)).ReturnsAsync(true);
        var factoryMock = new Mock<Func<Task<string?>>>();
        factoryMock.SetupSequence(f => f()).ThrowsAsync(new InvalidOperationException("Error factory")).ReturnsAsync("recuperado");
        Func<Task> accion = () => _service.GetOrSetAsync("clave", factoryMock.Object);

        // Act
        await accion.Should().ThrowAsync<InvalidOperationException>().WithMessage("Error factory");
        var resultado = await _service.GetOrSetAsync("clave", factoryMock.Object);

        // Assert
        resultado.Should().Be("recuperado");
        factoryMock.Verify(f => f(), Times.Exactly(2));
        _databaseMock.Verify(d => d.KeyDeleteAsync(new RedisKey("lock:clave")), Times.Exactly(2));
        _cacheMock.Verify(c => c.GetAsync("clave", default), Times.Exactly(2));
        _cacheMock.Verify(c => c.SetAsync("clave", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
    }
}

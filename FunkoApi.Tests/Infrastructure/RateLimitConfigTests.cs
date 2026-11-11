using System.Net;
using System.Text.Json;
using FluentAssertions;
using FunkoApi.Back.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

/// <summary>
/// Comprueba el middleware nativo con las reglas reales, sin arrancar la API ni sus bases de datos.
/// Cada test construye sus propios servicios para no compartir contadores.
/// </summary>
[TestFixture]
public class RateLimitConfigTests
{
    private ServiceProvider _provider = null!;
    private RequestDelegate _pipeline = null!;
    private int _nextCalls;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRateLimitingConfig();
        _provider = services.BuildServiceProvider();
        _nextCalls = 0;

        var app = new ApplicationBuilder(_provider);
        app.UseRateLimiter();
        app.Run(context =>
        {
            _nextCalls++;
            return Task.CompletedTask;
        });
        _pipeline = app.Build();
    }

    [TearDown]
    public async Task TearDown() => await _provider.DisposeAsync();

    // Contexto independiente por petición, igual que en los tests del middleware del profesor.
    private async Task<DefaultHttpContext> ExecuteAsync(
        string method, string path, string ip = "127.0.0.1",
        string? forwarded = null, string? realIp = null)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = _provider;
        context.Request.Method = method;
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        context.Response.Body = new MemoryStream();
        if (forwarded is not null) context.Request.Headers["X-Forwarded-For"] = forwarded;
        if (realIp is not null) context.Request.Headers["X-Real-IP"] = realIp;
        await _pipeline(context);
        return context;
    }

    [TestCase("GET", "/api/funkos", "127.0.0.1", 1000)]
    [TestCase("GET", "/api/funkos", "10.0.0.1", 1000)]
    [TestCase("POST", "/api/funkos", "127.0.0.1", 200)]
    [TestCase("POST", "/api/funkos", "10.0.0.1", 200)]
    public async Task Peticiones_AlAgotarElCupo_Devuelve429SinEjecutarElSiguiente(
        string method, string path, string ip, int limite)
    {
        // Arrange: consumir exactamente los permisos de esta ventana.
        for (var i = 0; i < limite; i++)
        {
            var permitida = await ExecuteAsync(method, path, ip);
            permitida.Response.StatusCode.Should().Be(200);
        }

        // Act
        var rechazada = await ExecuteAsync(method, path, ip);

        // Assert
        rechazada.Response.StatusCode.Should().Be(429);
        _nextCalls.Should().Be(limite);
    }

    [Test]
    public async Task Rechazo_DevuelveJsonYRetryAfterPositivo()
    {
        // Arrange
        for (var i = 0; i < 200; i++)
            await ExecuteAsync("POST", "/api/funkos");

        // Act
        var context = await ExecuteAsync("POST", "/api/funkos");

        // Assert
        context.Response.StatusCode.Should().Be(429);
        context.Response.ContentType.Should().Contain("application/json");
        int.TryParse(context.Response.Headers.RetryAfter.ToString(), out var retryAfter)
            .Should().BeTrue();
        retryAfter.Should().BeGreaterThan(0);
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        json.RootElement.GetProperty("errorType").GetString().Should().Be("RateLimitError");
        json.RootElement.GetProperty("message").GetString().Should().Contain("Demasiadas solicitudes");
        json.RootElement.GetProperty("retryAfter").GetInt32().Should().Be(retryAfter);
        _nextCalls.Should().Be(200);
    }

    [Test]
    public async Task ClientesYAmbitos_TienenContadoresIndependientes()
    {
        // Arrange
        for (var i = 0; i < 200; i++)
            await ExecuteAsync("POST", "/api/funkos", "10.0.0.1");

        // Act
        var bloqueada = await ExecuteAsync("POST", "/api/funkos", "10.0.0.1");
        var otraIp = await ExecuteAsync("POST", "/api/funkos", "10.0.0.2");
        var lectura = await ExecuteAsync("GET", "/api/funkos", "10.0.0.1");

        // Assert
        bloqueada.Response.StatusCode.Should().Be(429);
        otraIp.Response.StatusCode.Should().Be(200);
        lectura.Response.StatusCode.Should().Be(200);
        _nextCalls.Should().Be(202);
    }

    [TestCase("10.0.0.1, 10.0.0.9", "10.0.0.2", "10.0.0.1")]
    [TestCase(null, "10.0.0.2", "10.0.0.2")]
    public async Task CabecerasProxy_RespetanLaPrioridadDelEjemplo(
        string? forwarded, string? realIp, string ipEsperada)
    {
        // Arrange
        for (var i = 0; i < 200; i++)
            await ExecuteAsync("POST", "/api/funkos", forwarded: forwarded, realIp: realIp);

        // Act: la IP resuelta comparte contador con una conexión directa de esa IP.
        var bloqueada = await ExecuteAsync("POST", "/api/funkos", ipEsperada);

        // Assert
        bloqueada.Response.StatusCode.Should().Be(429);
        _nextCalls.Should().Be(200);
    }
}

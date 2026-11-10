using System.Text.Json;
using FunkoApi.Back.Dto.Documentation;
using FluentAssertions;
using FunkoApi.Back.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Middleware;

[TestFixture]
public class GlobalExceptionHandlerTests
{
    [TestCase("argumento", 400, "Dato incorrecto")]
    [TestCase("autorizacion", 401, "No autorizado")]
    [TestCase("timeout", 408, "Tiempo de espera agotado")]
    [TestCase("database", 409, "Error al procesar la operación en la base de datos.")]
    [TestCase("interno", 500, "Ha ocurrido un error interno")]
    public async Task InvokeAsync_Excepcion_RetornaCodigoYCuerpoEsperados(string tipo, int codigo, string mensaje)
    {
        // Arrange
        Exception excepcion = tipo switch
        {
            "argumento" => new ArgumentException("Dato incorrecto"),
            "autorizacion" => new UnauthorizedAccessException("Detalle privado"),
            "timeout" => new TimeoutException("Detalle privado"),
            "database" => new DbUpdateException("Detalle privado"),
            _ => new InvalidOperationException("Detalle privado")
        };
        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(excepcion);
        var handler = new GlobalExceptionHandler(nextMock.Object, new Mock<ILogger<GlobalExceptionHandler>>().Object);
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/api/funkos";
        contexto.Request.Method = "POST";
        using var stream = new MemoryStream();
        contexto.Response.Body = stream;

        // Act
        await handler.InvokeAsync(contexto);

        // Assert
        contexto.Response.StatusCode.Should().Be(codigo);
        contexto.Response.ContentType.Should().Be("application/json");
        stream.Position = 0;
        using var json = await JsonDocument.ParseAsync(stream);
        json.RootElement.GetProperty("message").GetString().Should().Be(mensaje);
        json.RootElement.GetProperty("status").GetInt32().Should().Be(codigo);
        json.RootElement.GetProperty("errorId").GetString().Should().HaveLength(8);
        json.RootElement.GetProperty("timestamp").GetDateTime().Kind.Should().Be(DateTimeKind.Utc);
        json.RootElement.GetProperty("method").GetString().Should().Be("POST");
        var path = json.RootElement.GetProperty("path");
        path.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("value", "hasValue");
        path.GetProperty("value").GetString().Should().Be("/api/funkos");
        path.GetProperty("hasValue").GetBoolean().Should().BeTrue();
        var documentada = JsonSerializer.Deserialize<ExceptionErrorResponse>(json.RootElement.GetRawText(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        documentada.Should().NotBeNull();
        documentada!.Status.Should().Be(codigo);
        documentada.Message.Should().Be(mensaje);
        documentada.Path.Should().Be(new RequestPathResponse("/api/funkos", true));
        json.RootElement.ToString().Should().NotContain("Detalle privado");
        nextMock.Verify(n => n(contexto), Times.Once);
    }

    [Test]
    public async Task UseGlobalExceptionHandler_SinExcepcion_EjecutaSiguienteMiddleware()
    {
        // Arrange
        var servicios = new ServiceCollection().AddLogging();
        using var provider = servicios.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        var contexto = new DefaultHttpContext();

        // Act
        app.UseGlobalExceptionHandler();
        app.Run(nextMock.Object);
        await app.Build()(contexto);

        // Assert
        contexto.Response.StatusCode.Should().Be(200);
        nextMock.Verify(n => n(contexto), Times.Once);
    }
}

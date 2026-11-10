using System.Text.Json;
using FluentAssertions;
using FunkoApi.Back.Dto.Documentation;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Extensions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace FunkoApi.Tests.Dto.Documentation;

[TestFixture]
public class ErrorResponsesTests
{
    [Test]
    public void MessageErrorResponse_ErrorDeDominio_CoincideConElCuerpoDocumentado()
    {
        // Arrange
        var esperado = new MessageErrorResponse("Funko no encontrado");
        var respuesta = (ObjectResult)new NotFoundError(esperado.Message).ToHttpResult();
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var json = JsonSerializer.Serialize(respuesta.Value, opciones);
        var resultado = JsonSerializer.Deserialize<MessageErrorResponse>(json, opciones);

        // Assert
        resultado.Should().Be(esperado);
        using var documento = JsonDocument.Parse(JsonSerializer.Serialize(esperado, opciones));
        documento.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("message");
        documento.RootElement.GetProperty("message").GetString().Should().Be(esperado.Message);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DomainValidationErrorResponse_ConYSinErrores_CoincideConElCuerpoDocumentado(bool conErrores)
    {
        // Arrange
        Dictionary<string, string[]>? errores = conErrores
            ? new Dictionary<string, string[]> { ["Nombre"] = ["Es obligatorio", "Debe ser único"] }
            : null;
        var esperado = new DomainValidationErrorResponse("Errores de validación", errores);
        var error = new BadRequestError.ValidationError(esperado.Message, errores);
        var respuesta = (ObjectResult)error.ToHttpResult();
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var json = JsonSerializer.Serialize(respuesta.Value, opciones);
        var resultado = JsonSerializer.Deserialize<DomainValidationErrorResponse>(json, opciones);

        // Assert
        resultado.Should().BeEquivalentTo(esperado);
        using var documento = JsonDocument.Parse(JsonSerializer.Serialize(esperado, opciones));
        documento.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("message", "errors");
        if (conErrores)
            documento.RootElement.GetProperty("errors").GetProperty("Nombre").EnumerateArray()
                .Select(e => e.GetString()).Should().Equal(errores!["Nombre"]);
        else documento.RootElement.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public void ExceptionErrorResponse_Serializacion_ConservaLosCamposDelEsquemaDocumental()
    {
        // Arrange
        var esperado = new ExceptionErrorResponse("abcd1234", 500, "Ha ocurrido un error interno",
            "2026-11-10T10:00:00Z", new RequestPathResponse("/api/funkos", true), "POST");
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Act
        var json = JsonSerializer.Serialize(esperado, opciones);
        var resultado = JsonSerializer.Deserialize<ExceptionErrorResponse>(json, opciones);

        // Assert
        resultado.Should().Be(esperado);
        using var documento = JsonDocument.Parse(json);
        documento.RootElement.EnumerateObject().Select(p => p.Name).Should()
            .Equal("errorId", "status", "message", "timestamp", "path", "method");
        documento.RootElement.GetProperty("status").GetInt32().Should().Be(500);
        documento.RootElement.GetProperty("path").GetProperty("value").GetString().Should().Be("/api/funkos");
        documento.RootElement.GetProperty("path").GetProperty("hasValue").GetBoolean().Should().BeTrue();
    }
}

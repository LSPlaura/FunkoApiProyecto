using FluentAssertions;
using FunkoApi.Back.Controllers;
using FunkoApi.Back.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using NUnit.Framework;
using Swashbuckle.AspNetCore.Swagger;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class SwaggerDocumentationTests {
    private WebApplication _app = null!;
    private OpenApiDocument _document = null!;

    [SetUp]
    public void SetUp() {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            EnvironmentName = Environments.Development
        });
        builder.Services.AddControllers(options => {
            options.RespectBrowserAcceptHeader = true;
            options.ReturnHttpNotAcceptable = true;
        }).AddApplicationPart(typeof(AuthController).Assembly).AddXmlDataContractSerializerFormatters();
        builder.Services.AddSwaggerConfiguration();
        _app = builder.Build();
        _app.MapControllers();
        _document = _app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
    }

    [TearDown]
    public async Task TearDown() {
        if (_app is not null) await _app.DisposeAsync();
    }

    [Test]
    public void Documento_ControladoresCompletos_Contiene22OperacionesDocumentadas() {
        // Arrange
        var operations = _document.Paths.Values.SelectMany(p => p.Operations!.Values).ToList();

        // Act
        var ids = operations.Select(o => o.OperationId).ToList();

        // Assert
        operations.Should().HaveCount(22);
        ids.Should().OnlyHaveUniqueItems();
        operations.Should().OnlyContain(o => !string.IsNullOrWhiteSpace(o.Summary));
        operations.Should().OnlyContain(o => o.Responses!.ContainsKey("500"));
        operations.Should().OnlyContain(o => o.Responses!.ContainsKey("408"));
        operations.Should().OnlyContain(o => o.Responses!.ContainsKey("406"));
    }

    [TestCase("/api/Auth/login", "Auth_Login")]
    [TestCase("/api/Auth/signup", "Auth_Signup")]
    public void Autenticacion_Publica_NoExigeJwt(string path, string operationId) {
        // Arrange
        var operation = _document.Paths[path].Operations!.Values.Single();

        // Act
        var security = operation.Security;

        // Assert
        operation.OperationId.Should().Be(operationId);
        security.Should().NotBeNull().And.BeEmpty();
        operation.Responses.Should().NotContainKey("403");
        operation.RequestBody!.Content.Should().ContainKey("application/json");
    }

    [Test]
    public void OperacionesProtegidas_ConRoles_Documentan401Y403() {
        // Arrange
        var operations = _document.Paths.Values.SelectMany(p => p.Operations!.Values)
            .Where(o => !o.OperationId!.StartsWith("Auth_")).ToList();

        // Act
        var descriptions = operations.Select(o => o.Description).ToList();

        // Assert
        operations.Should().HaveCount(19);
        operations.Should().OnlyContain(o => o.Responses!.ContainsKey("401") && o.Responses.ContainsKey("403"));
        descriptions.Should().OnlyContain(d => d!.Contains("ADMIN"));
        _document.Components!.SecuritySchemes.Should().ContainKey("Bearer");
        _document.Security.Should().NotBeEmpty();
    }

    [TestCase("/api/Funkos", "Funkos_Create", "201")]
    [TestCase("/api/Funkos/{id}", "Funkos_Update", "200")]
    [TestCase("/api/Funkos/{id}/imagen", "Funkos_UpdateImagen", "200")]
    public void Funkos_EntradasDeFormulario_DocumentaArchivoYSalida(string path, string id, string status) {
        // Arrange
        var operation = _document.Paths[path].Operations!.Values.Single(o => o.OperationId == id);

        // Act
        var body = operation.RequestBody!.Content["multipart/form-data"].Schema!;

        // Assert
        body.Properties.Should().ContainKey("archivo");
        body.Properties!["archivo"].Format.Should().Be("binary");
        operation.Responses![status].Content!["application/json"].Schema.Should().NotBeNull();
        operation.Responses.Should().ContainKey("422");
    }

    [TestCase("/api/Categorias")]
    [TestCase("/api/Funkos")]
    [TestCase("/api/Proveedores")]
    public void Creacion_DeRecursos_Documenta201YLocation(string path) {
        // Arrange
        var operation = _document.Paths[path].Operations!.Values.Single(o => o.OperationId!.EndsWith("_Create"));

        // Act
        var response = operation.Responses!["201"];

        // Assert
        response.Headers.Should().ContainKey("Location");
        response.Content!["application/json"].Schema.Should().NotBeNull();
        operation.Responses.Should().NotContainKey("200");
    }

    [TestCase("/api/Categorias/{id}")]
    [TestCase("/api/Funkos/{id}")]
    [TestCase("/api/Proveedores/{id}")]
    public void Delete_ContratoActual_Documenta200BooleanoYNo204(string path) {
        // Arrange
        var operation = _document.Paths[path].Operations!.Values.Single(o => o.OperationId!.EndsWith("_Delete"));

        // Act
        var schema = operation.Responses!["200"].Content!["application/json"].Schema!;

        // Assert
        schema.Type.Should().Be(JsonSchemaType.Boolean);
        operation.Responses.Should().NotContainKey("204");
    }

    [Test]
    public void Errores_DeValidacionYDominio_NoSeDocumentanComoElMismoFormato() {
        // Arrange
        var operation = _document.Paths["/api/Auth/signup"].Operations!.Values.Single();

        // Act
        var badRequest = operation.Responses!["400"];

        // Assert
        badRequest.Content.Should().ContainKey("application/problem+json");
        badRequest.Content!["application/json"].Schema!.AnyOf.Should().HaveCount(2);
        _document.Components!.Schemas.Should().ContainKey("MessageErrorResponse");
        _document.Components.Schemas.Should().ContainKey("ExceptionErrorResponse");
        operation.Responses.Should().ContainKey("422");
    }

    [Test]
    public void Paginacion_ParametrosPorDefecto_DocumentaLinkYMetadatos() {
        // Arrange
        var operation = _document.Paths["/api/Funkos/paged"].Operations!.Values.Single();

        // Act
        var names = operation.Parameters!.Select(p => p.Name).ToList();

        // Assert
        names.Should().BeEquivalentTo("page", "pageSize");
        operation.Responses!["200"].Headers.Should().ContainKey("Link");
        operation.Responses["200"].Content!["application/json"].Schema.Should().NotBeNull();
        operation.Parameters.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Description));
    }
}

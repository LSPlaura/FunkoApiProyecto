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
        var body = operation.RequestBody!.Content!["multipart/form-data"].Schema!;

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
        var schemas = badRequest.Content["application/json"].Schema!.AnyOf!
            .Cast<OpenApiSchemaReference>().Select(s => s.Reference.Id).ToList();
        schemas.Should().Contain("ValidationProblemDetails");
        schemas.Should().Contain("ExceptionErrorResponse");
        schemas.Should().NotContain("MessageErrorResponse");
        _document.Components!.Schemas.Should().ContainKey("MessageErrorResponse");
        _document.Components.Schemas.Should().ContainKey("ExceptionErrorResponse");
        operation.Responses.Should().ContainKey("422");
    }

    [Test]
    public void Login_Error401_ConservaErrorDeDominioYDocumentaExcepcion() {
        // Arrange
        var operation = _document.Paths["/api/Auth/login"].Operations!.Values.Single();

        // Act
        var schemas = operation.Responses!["401"].Content!["application/json"].Schema!.AnyOf!
            .Cast<OpenApiSchemaReference>().Select(s => s.Reference.Id).ToList();

        // Assert
        schemas.Should().BeEquivalentTo("MessageErrorResponse", "ExceptionErrorResponse");
    }

    [Test]
    public void Signup_Error401_DocumentaSoloExcepcionSinExigirJwt() {
        // Arrange
        var operation = _document.Paths["/api/Auth/signup"].Operations!.Values.Single();

        // Act
        var schema = operation.Responses!["401"].Content!["application/json"].Schema!;

        // Assert
        schema.Should().BeOfType<OpenApiSchemaReference>();
        ((OpenApiSchemaReference)schema).Reference.Id.Should().Be("ExceptionErrorResponse");
        operation.Security.Should().BeEmpty();
    }

    [TestCase("AuthResponseDto", "token,email,role,expiresAt")]
    [TestCase("UserDto", "id,email,nombre,apellidos,role")]
    [TestCase("CategoriaDto", "id,nombre,isDeleted,createdAt,updatedAt")]
    [TestCase("ProveedorDto", "id,nombre,isDeleted,createdAt,updatedAt")]
    [TestCase("FunkoDto", "id,nombre,precio,stock,imagen,categoria,isDeleted,createdAt,updatedAt")]
    public void ModelosDeSalida_ContratoActual_ConservaCamposSinExponerDatosInternos(string modelo, string campos) {
        // Arrange
        var schema = _document.Components!.Schemas![modelo];

        // Act
        var propiedades = schema.Properties!.Keys.ToList();

        // Assert
        propiedades.Should().BeEquivalentTo(campos.Split(','));
        propiedades.Should().NotContain("password").And.NotContain("passwordHash");
    }

    [Test]
    public void GetMe_ExigeJwtSinRol_NoAnuncia403PorRoles() {
        // Arrange
        var operation = _document.Paths["/api/Auth/me"].Operations!.Values.Single();

        // Act
        var responses = operation.Responses!;

        // Assert
        operation.Description.Should().Contain("Requiere un JWT válido; no exige un rol concreto.");
        responses.Should().ContainKey("401").And.ContainKey("404");
        responses.Should().NotContainKey("403");
        responses["401"].Content!["application/json"].Schema.Should().BeOfType<OpenApiSchemaReference>()
            .Which.Reference.Id.Should().Be("ExceptionErrorResponse");
    }

    [TestCase("/api/Categorias", "Categorias_Create")]
    [TestCase("/api/Proveedores", "Proveedores_Create")]
    [TestCase("/api/Auth/login", "Auth_Login")]
    [TestCase("/api/Auth/signup", "Auth_Signup")]
    public void EntradaJson_Documenta415ConProblemDetails(string path, string id) {
        // Arrange
        var operation = _document.Paths[path].Operations!.Values.Single(o => o.OperationId == id);

        // Act
        var response = operation.Responses!["415"];

        // Assert
        response.Content.Should().ContainKey("application/problem+json");
        response.Content!["application/problem+json"].Schema.Should().BeOfType<OpenApiSchemaReference>()
            .Which.Reference.Id.Should().Be("ProblemDetails");
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

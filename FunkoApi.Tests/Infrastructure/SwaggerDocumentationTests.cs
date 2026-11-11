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
        }).AddApplicationPart(typeof(AuthController).Assembly);
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
    public void Documento_ControladoresCompletos_Contiene24OperacionesDocumentadas() {
        // Arrange
        var operations = _document.Paths.Values.SelectMany(p => p.Operations!.Values).ToList();

        // Act
        var ids = operations.Select(o => o.OperationId).ToList();

        // Assert
        operations.Should().HaveCount(24);
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
        operations.Should().HaveCount(21);
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

    [Test]
    public void Error500_TodasLasOperaciones_DocumentanDominioYExcepcionSinDuplicados() {
        // Arrange
        var operations = _document.Paths.Values.SelectMany(p => p.Operations!.Values).ToList();

        foreach (var operation in operations) {
            // Act
            var schemas = operation.Responses!["500"].Content!["application/json"].Schema!.AnyOf!
                .Cast<OpenApiSchemaReference>().Select(s => s.Reference.Id).ToList();

            // Assert
            schemas.Should().BeEquivalentTo("MessageErrorResponse", "ExceptionErrorResponse");
            schemas.Should().OnlyHaveUniqueItems();
        }
    }

    [TestCase("/api/Funkos", "Funkos_Create")]
    [TestCase("/api/Funkos/{id}", "Funkos_Update")]
    [TestCase("/api/Funkos/{id}/imagen", "Funkos_UpdateImagen")]
    [TestCase("/api/Categorias/{id}", "Categorias_Delete")]
    public void Error400_EntradasConErrorDeDominio_IncluyeValidacionAutomaticaEnJson(string path, string id) {
        // Arrange
        var operation = _document.Paths[path].Operations!.Values.Single(o => o.OperationId == id);

        // Act
        var schemas = operation.Responses!["400"].Content!["application/json"].Schema!.AnyOf!
            .Cast<OpenApiSchemaReference>().Select(s => s.Reference.Id).ToList();

        // Assert
        schemas.Should().BeEquivalentTo("MessageErrorResponse", "ExceptionErrorResponse", "ValidationProblemDetails");
        schemas.Should().OnlyHaveUniqueItems();
        operation.Responses["400"].Content!["application/problem+json"].Schema.Should()
            .BeOfType<OpenApiSchemaReference>().Which.Reference.Id.Should().Be("ValidationProblemDetails");
    }

    [Test]
    public void ExceptionErrorResponse_Path_DocumentaElObjetoRealDelMiddleware() {
        // Arrange
        var exceptionSchema = _document.Components!.Schemas!["ExceptionErrorResponse"];

        // Act
        var pathReference = (OpenApiSchemaReference)exceptionSchema.Properties!["path"];
        var pathSchema = _document.Components.Schemas[pathReference.Reference.Id!];

        // Assert
        pathSchema.Type.Should().Be(JsonSchemaType.Object);
        pathSchema.Properties!.Keys.Should().BeEquivalentTo("value", "hasValue");
        pathSchema.Properties["value"].Type!.Value.HasFlag(JsonSchemaType.String).Should().BeTrue();
        pathSchema.Properties["hasValue"].Type.Should().Be(JsonSchemaType.Boolean);
    }

    [Test]
    public void Funkos_BusquedaPorNombre_UsaRutaSeparadaDeLaConsultaPorId() {
        // Arrange
        var operation = _document.Paths["/api/Funkos/nombre/{nombre}"].Operations!.Values.Single();

        // Act
        var parameter = operation.Parameters!.Single();

        // Assert
        operation.OperationId.Should().Be("Funkos_GetByNombre");
        parameter.Name.Should().Be("nombre");
        parameter.In.Should().Be(ParameterLocation.Path);
        parameter.Required.Should().BeTrue();
        _document.Paths.Should().ContainKey("/api/Funkos/{id}");
        _document.Paths.Should().NotContainKey("/api/Funkos/{nombre}");
    }

    [Test]
    public void Documento_PlantillasSinNombresDeParametros_NoRepiteMetodoYRuta() {
        // Arrange
        var rutas = new List<string>();

        // Act
        foreach (var path in _document.Paths) {
            var plantilla = string.Join("/", path.Key.Split('/').Select(segmento =>
                segmento.StartsWith("{") ? "{}" : segmento));
            foreach (var operation in path.Value.Operations!)
                rutas.Add($"{operation.Key} {plantilla}");
        }

        // Assert
        rutas.Should().HaveCount(24);
        rutas.Should().OnlyHaveUniqueItems();
    }

    [TestCase("/api/Auth/login", "Auth_Login", "200")]
    [TestCase("/api/Auth/signup", "Auth_Signup", "200")]
    [TestCase("/api/Auth/me", "Auth_Me", "200")]
    [TestCase("/api/Categorias", "Categorias_GetAll", "200")]
    [TestCase("/api/Categorias/paged", "Categorias_GetPaged", "200")]
    [TestCase("/api/Categorias/{id}", "Categorias_GetById", "200")]
    [TestCase("/api/Categorias/nombre", "Categorias_GetByName", "200")]
    [TestCase("/api/Categorias", "Categorias_Create", "201")]
    [TestCase("/api/Categorias/{id}", "Categorias_Update", "200")]
    [TestCase("/api/Categorias/{id}", "Categorias_Delete", "200")]
    [TestCase("/api/Proveedores", "Proveedores_GetAll", "200")]
    [TestCase("/api/Proveedores/paged", "Proveedores_GetPaged", "200")]
    [TestCase("/api/Proveedores/{id}", "Proveedores_GetById", "200")]
    [TestCase("/api/Proveedores", "Proveedores_Create", "201")]
    [TestCase("/api/Proveedores/{id}", "Proveedores_Update", "200")]
    [TestCase("/api/Proveedores/{id}", "Proveedores_Delete", "200")]
    [TestCase("/api/Funkos", "Funkos_GetAll", "200")]
    [TestCase("/api/Funkos/{id}", "Funkos_GetById", "200")]
    [TestCase("/api/Funkos/nombre/{nombre}", "Funkos_GetByNombre", "200")]
    [TestCase("/api/Funkos/paged", "Funkos_GetPaged", "200")]
    [TestCase("/api/Funkos", "Funkos_Create", "201")]
    [TestCase("/api/Funkos/{id}", "Funkos_Update", "200")]
    [TestCase("/api/Funkos/{id}", "Funkos_Delete", "200")]
    [TestCase("/api/Funkos/{id}/imagen", "Funkos_UpdateImagen", "200")]
    public void Inventario_EndpointActual_DocumentaRutaYSalida(string path, string id, string status) {
        // Arrange
        var operation = _document.Paths[path].Operations!.Values.Single(o => o.OperationId == id);

        // Act
        var response = operation.Responses![status];

        // Assert
        operation.Summary.Should().NotBeNullOrWhiteSpace();
        response.Description.Should().NotBeNullOrWhiteSpace();
        response.Content!["application/json"].Schema.Should().NotBeNull();
    }

    [TestCase("/api/Auth/login", "Auth_Login", "email,password")]
    [TestCase("/api/Auth/signup", "Auth_Signup", "email,password,nombre,apellidos")]
    [TestCase("/api/Categorias", "Categorias_Create", "nombre")]
    [TestCase("/api/Categorias/{id}", "Categorias_Update", "nombre")]
    [TestCase("/api/Proveedores", "Proveedores_Create", "nombre")]
    [TestCase("/api/Proveedores/{id}", "Proveedores_Update", "nombre")]
    public void EntradaJson_ContratoActual_DocumentaCamposYTipos(string path, string id, string campos) {
        // Arrange
        var operation = _document.Paths[path].Operations!.Values.Single(o => o.OperationId == id);
        var reference = (OpenApiSchemaReference)operation.RequestBody!.Content!["application/json"].Schema!;

        // Act
        var schema = _document.Components!.Schemas![reference.Reference.Id!];

        // Assert
        schema.Properties!.Keys.Should().BeEquivalentTo(campos.Split(','));
        schema.Properties.Values.Should().OnlyContain(p => p.Type == JsonSchemaType.String);
        schema.Required.Should().BeEquivalentTo(campos.Split(','));
        operation.RequestBody.Required.Should().BeTrue();
    }

    [TestCase("LoginDto")]
    [TestCase("SignupDto")]
    public void Autenticacion_RecordsPosicionales_DocumentaLasRestriccionesEfectivas(string modelo) {
        // Arrange
        var schema = _document.Components!.Schemas![modelo];

        // Act
        var email = schema.Properties!["email"];
        var password = schema.Properties["password"];

        // Assert
        email.Format.Should().Be("email");
        password.MinLength.Should().Be(8);
        password.MaxLength.Should().Be(100);
        password.Format.Should().Be("password");
        password.WriteOnly.Should().BeTrue();
        if (modelo == "SignupDto") {
            schema.Properties["nombre"].MaxLength.Should().Be(50);
            schema.Properties["apellidos"].MaxLength.Should().Be(100);
        }
    }

    [TestCase("/api/Funkos", "Funkos_Create")]
    [TestCase("/api/Funkos/{id}", "Funkos_Update")]
    public void EntradaFormulario_ContratoActual_NoPublicaLosRangosDelDtoComoValidacionEfectiva(string path, string id) {
        // Arrange
        var operation = _document.Paths[path].Operations!.Values.Single(o => o.OperationId == id);

        // Act
        var schema = operation.RequestBody!.Content!["multipart/form-data"].Schema!;

        // Assert
        schema.Properties!.Keys.Should().BeEquivalentTo("nombre", "precio", "stock", "categoriaId", "proveedorId", "archivo");
        schema.Properties["nombre"].MaxLength.Should().BeNull();
        schema.Properties["precio"].Minimum.Should().BeNull();
        schema.Properties["precio"].Maximum.Should().BeNull();
        schema.Properties["stock"].Minimum.Should().BeNull();
        operation.Description.Should().Contain("no se");
    }
}

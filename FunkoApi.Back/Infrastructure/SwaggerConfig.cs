using FunkoApi.Back.Swagger;
using Microsoft.OpenApi;

namespace FunkoApi.Back.Infrastructure;

/// <summary>Configura la documentación OpenAPI y Swagger UI sin cambiar el contrato HTTP.</summary>
public static class SwaggerConfig {
    /// <summary>Registra metadatos, comentarios XML, anotaciones y seguridad JWT.</summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services) {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => {
            options.SwaggerDoc("v1", new OpenApiInfo {
                Title = "Funko API",
                Version = "v1",
                Description = "API REST de autenticación, categorías, funkos y proveedores. " +
                    "Las operaciones protegidas requieren un JWT; cada operación indica los roles admitidos. " +
                    "Los errores de dominio usan message, la validación automática usa ValidationProblemDetails " +
                    "y las excepciones usan errorId/status/message/timestamp/path/method; path es un objeto con value y hasValue. " +
                    "La creación y el borrado de Funkos encolan un email: una respuesta correcta no garantiza su entrega. " +
                    "Consultar Doc/Auditoria-Endpoints-OpenAPI.md para las limitaciones detectadas."
            });

            var xmlFile = $"{typeof(SwaggerConfig).Assembly.GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);

            options.EnableAnnotations();
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Introduce únicamente el token JWT obtenido con login o signup, sin el prefijo Bearer."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement {
                { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
            });
            options.OperationFilter<EndpointDocumentationFilter>();
        });
        return services;
    }
}

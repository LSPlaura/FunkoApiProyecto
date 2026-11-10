using System.Reflection;
using FunkoApi.Back.Dto.Documentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FunkoApi.Back.Swagger;

/// <summary>
/// Adapta la documentación a los errores reales del middleware y a los permisos de los controladores.
/// Solo modifica OpenAPI, no las respuestas de la aplicación.
/// </summary>
public class EndpointDocumentationFilter : IOperationFilter {
    public void Apply(OpenApiOperation operation, OperationFilterContext context) {
        var permissions = context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>()
            .Concat(context.MethodInfo.DeclaringType!.GetCustomAttributes<AuthorizeAttribute>()).ToList();

        if (permissions.Count == 0) {
            // Login y signup son públicos aunque el documento tenga un requisito JWT global.
            operation.Security = [];
        } else {
            var roles = string.Join(", ", permissions.Select(p => p.Roles).Where(r => !string.IsNullOrEmpty(r)));
            operation.Description += string.IsNullOrEmpty(roles)
                ? "\n\nRequiere un JWT válido; no exige un rol concreto."
                : $"\n\nRequiere JWT y alguno de estos roles: {roles}.";
            operation.Responses!["401"] = JsonResponse("JWT ausente, inválido o expirado (normalmente sin cuerpo). " +
                "El middleware también traduce UnauthorizedAccessException a JSON con este estado.", typeof(ExceptionErrorResponse));
            if (!string.IsNullOrEmpty(roles))
                operation.Responses["403"] = new OpenApiResponse { Description = "JWT válido, pero sin un rol autorizado. Sin cuerpo." };
        }

        // Excepciones transversales: solo llegan aquí si no las transforma previamente un servicio.
        AddException("400", "ArgumentException no tratada; también puede haber errores de binding o validación automática.");
        AddException("408", "TimeoutException no tratada. Tiempo de espera agotado.");
        AddException("409", "DbUpdateException no tratada. Conflicto al escribir en la base de datos.");
        AddException("500", "Excepción no tratada; los InternalError de dominio devuelven únicamente message.");
        operation.Responses!["406"] = new OpenApiResponse { Description = "No hay un formateador que pueda producir el formato solicitado en Accept. Sin cuerpo." };

        var badRequest = (OpenApiResponse)operation.Responses["400"];
        badRequest.Content ??= new Dictionary<string, OpenApiMediaType>();
        badRequest.Content["application/problem+json"] = new OpenApiMediaType {
            Schema = context.SchemaGenerator.GenerateSchema(typeof(ValidationProblemDetails), context.SchemaRepository)
        };

        if (context.ApiDescription.SupportedRequestFormats.Count > 0)
            operation.Responses["415"] = new OpenApiResponse {
                Description = "Content-Type no admitido para el cuerpo de esta operación.",
                Content = new Dictionary<string, OpenApiMediaType> {
                    ["application/problem+json"] = new() {
                        Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository)
                    }
                }
            };

        if (operation.Responses.TryGetValue("201", out var created)) {
            var createdResponse = (OpenApiResponse)created;
            createdResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
            createdResponse.Headers["Location"] = new OpenApiHeader {
                Description = "URL del recurso creado, generada por CreatedAtAction.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            };
        }
        if (context.MethodInfo.Name == "GetPaged") {
            var ok = (OpenApiResponse)operation.Responses["200"];
            ok.Headers ??= new Dictionary<string, IOpenApiHeader>();
            ok.Headers["Link"] = new OpenApiHeader {
                Description = "Enlaces de navegación calculados por PaginationHelper; se omite cuando no hay enlaces.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            };
        }

        void AddException(string code, string description) {
            if (!operation.Responses!.TryGetValue(code, out var existing)) {
                operation.Responses[code] = existing = JsonResponse(description, typeof(ExceptionErrorResponse));
                if (code != "500") return;
            } else {
                existing.Description += " " + description;
            }
            var response = (OpenApiResponse)existing;
            response.Content ??= new Dictionary<string, OpenApiMediaType>();
            response.Content["application/json"] = new OpenApiMediaType {
                Schema = new OpenApiSchema {
                    AnyOf = [
                        context.SchemaGenerator.GenerateSchema(typeof(MessageErrorResponse), context.SchemaRepository),
                        context.SchemaGenerator.GenerateSchema(typeof(ExceptionErrorResponse), context.SchemaRepository)
                    ]
                }
            };
        }

        OpenApiResponse JsonResponse(string description, Type type) => new() {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType> {
                ["application/json"] = new() {
                    Schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository)
                }
            }
        };
    }
}

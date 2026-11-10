using System.Reflection;
using FunkoApi.Back.Dto.Auth;
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
        DocumentJsonInput();
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
            operation.Responses!["401"] = new OpenApiResponse {
                Description = "JWT ausente, inválido o expirado (normalmente sin cuerpo)."
            };
            if (!string.IsNullOrEmpty(roles))
                operation.Responses["403"] = new OpenApiResponse { Description = "JWT válido, pero sin un rol autorizado. Sin cuerpo." };
        }

        // Excepciones transversales: solo llegan aquí si no las transforma previamente un servicio.
        AddException("400", "ArgumentException no tratada; también puede haber errores de binding o validación automática.");
        AddException("401", "UnauthorizedAccessException no tratada; el middleware devuelve JSON con este estado.");
        AddException("408", "TimeoutException no tratada. Tiempo de espera agotado.");
        AddException("409", "DbUpdateException no tratada. Conflicto al escribir en la base de datos.");
        AddException("500", "Excepción no tratada; los InternalError de dominio devuelven únicamente message.");
        operation.Responses!["406"] = new OpenApiResponse { Description = "No hay un formateador que pueda producir el formato solicitado en Accept. Sin cuerpo." };

        // ApiController valida las entradas antes de ejecutar la acción. Ese 400
        // no tiene el mismo cuerpo que un BadRequestError del servicio.
        if (context.ApiDescription.ParameterDescriptions.Count > 0) {
            var badRequest = (OpenApiResponse)operation.Responses["400"];
            AddJsonSchema(badRequest, typeof(ValidationProblemDetails));
            badRequest.Content!["application/problem+json"] = new OpenApiMediaType {
                Schema = context.SchemaGenerator.GenerateSchema(typeof(ValidationProblemDetails), context.SchemaRepository)
            };
        }

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

        void DocumentJsonInput() {
            if (operation.RequestBody?.Content is null ||
                !operation.RequestBody.Content.TryGetValue("application/json", out var content)) return;

            // Los seis cuerpos JSON de los controladores actuales son obligatorios.
            ((OpenApiRequestBody)operation.RequestBody).Required = true;
            if (content.Schema is not OpenApiSchemaReference reference ||
                (reference.Reference.Id != nameof(LoginDto) && reference.Reference.Id != nameof(SignupDto))) return;

            // En estos records los atributos de validación están en los parámetros
            // del constructor. Swagger no los está trasladando a las propiedades.
            // Reproducir aquí las restricciones efectivas sin cambiar el binding.
            var schema = (OpenApiSchema)context.SchemaRepository.Schemas[reference.Reference.Id];
            schema.Required = new HashSet<string>(schema.Properties!.Keys);
            foreach (var property in schema.Properties.Values)
                ((OpenApiSchema)property).Type = JsonSchemaType.String;
            ((OpenApiSchema)schema.Properties["email"]).Format = "email";
            var password = (OpenApiSchema)schema.Properties["password"];
            password.MinLength = 8;
            password.MaxLength = 100;
            password.Format = "password";
            password.WriteOnly = true;
            if (reference.Reference.Id == nameof(SignupDto)) {
                ((OpenApiSchema)schema.Properties["nombre"]).MaxLength = 50;
                ((OpenApiSchema)schema.Properties["apellidos"]).MaxLength = 100;
            }
        }

        void AddException(string code, string description) {
            if (!operation.Responses!.TryGetValue(code, out var existing)) {
                // El 500 también puede proceder de la traducción de InternalError.
                // No crear primero un esquema de excepción para después repetirlo.
                operation.Responses[code] = existing = JsonResponse(description,
                    code == "500" ? typeof(MessageErrorResponse) : typeof(ExceptionErrorResponse));
                if (code != "500") return;
            } else {
                existing.Description += " " + description;
            }
            AddJsonSchema((OpenApiResponse)existing, typeof(ExceptionErrorResponse));
        }

        void AddJsonSchema(OpenApiResponse response, Type type) {
            response.Content ??= new Dictionary<string, OpenApiMediaType>();
            var previousSchema = response.Content.TryGetValue("application/json", out var json)
                ? json.Schema
                : null;
            var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
            if (previousSchema is null) {
                response.Content["application/json"] = new OpenApiMediaType {
                    Schema = schema
                };
                return;
            }

            // Mantener una lista plana, sin repetir la validación ya declarada
            // en las anotaciones ni perder el esquema de dominio de la acción.
            var schemas = previousSchema.AnyOf?.Count > 0
                ? previousSchema.AnyOf.ToList()
                : new List<IOpenApiSchema> { previousSchema };
            if (schema is OpenApiSchemaReference reference && schemas.Any(s =>
                s is OpenApiSchemaReference previous && previous.Reference.Id == reference.Reference.Id)) return;
            schemas.Add(schema);
            response.Content["application/json"] = new OpenApiMediaType {
                Schema = new OpenApiSchema { AnyOf = schemas }
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

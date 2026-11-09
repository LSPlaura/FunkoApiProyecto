using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Back.Middleware;

/// <summary>
/// Manejador global de excepciones.
/// </summary>
public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger) {
    /// <summary>
    /// Ejecuta el siguiente middleware y captura las excepciones que no se hayan tratado.
    /// </summary>
    /// <param name="context">Contexto de la petición y de la respuesta HTTP.</param>
    /// <returns>Una tarea que finaliza tras procesar la petición o escribir su respuesta de error.</returns>
    public async Task InvokeAsync(HttpContext context) {
        try {
            await next(context);
        } catch (Exception e) {
            var errorId = Guid.NewGuid().ToString()[..8];
            logger.LogError(e, "Excepción no manejada. ErrorId: {ErrorId}", errorId);
            await HandleExceptionAsync(context, e, errorId);
        }
    }

    /// <summary>
    /// Traduce la excepción a un código HTTP y escribe una respuesta JSON con su identificador de seguimiento.
    /// </summary>
    /// <param name="context">Contexto en cuya respuesta se escribirá el error.</param>
    /// <param name="exception">Excepción capturada que determina el estado y el mensaje.</param>
    /// <param name="errorId">Identificador que permite relacionar la respuesta con el registro del error.</param>
    /// <returns>Una tarea que representa la escritura de la respuesta.</returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string errorId) {
        context.Response.ContentType = "application/json";

        var statusCode = exception switch {
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            DbUpdateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var message = exception switch {
            ArgumentException arg => arg.Message,
            UnauthorizedAccessException => "No autorizado",
            TimeoutException => "Tiempo de espera agotado",
            DbUpdateException => "Error al procesar la operación en la base de datos.",
            _ => "Ha ocurrido un error interno"
        };

        context.Response.StatusCode = statusCode;

        var response = new {
            errorId,
            status = statusCode,
            message,
            timestamp = DateTime.UtcNow.ToString("o"),
            path = context.Request.Path,
            method = context.Request.Method
        };

        var jsonOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

/// <summary>
/// Extensión para registrar el middleware de excepciones.
/// </summary>
public static class GlobalExceptionHandlerExtensions {
    /// <summary>
    /// Añade el manejador global de excepciones al pipeline de la aplicación.
    /// </summary>
    /// <param name="app">Pipeline que recibirá el middleware.</param>
    /// <returns>El pipeline para continuar configurando la aplicación.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}

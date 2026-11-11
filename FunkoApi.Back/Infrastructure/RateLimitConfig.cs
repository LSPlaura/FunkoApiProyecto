using System.Globalization;
using System.Threading.RateLimiting;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
/// Configura el limitador global nativo por IP y ámbito, con ventanas de un minuto.
/// Adaptado del ejemplo ProductosOptimization del profesor.
/// </summary>
public static class RateLimitConfig {
    
    private const int EscrituraLimit = 200;
    private const int GeneralLimit = 1000;
    private const int LocalLimit = 1000;

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    
    /// <summary>Registra las reglas y la respuesta JSON para las solicitudes rechazadas.</summary>
    public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services) {
        Log.Information("Configurando rate limiting...");
        services.AddRateLimiter(options => {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => {
                    var ip = ResolveClientIp(context);
                    var esEscritura = HttpMethods.IsPost(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/funkos");

                    // POST de Funkos tiene prioridad incluso para clientes locales.
                    var (ambito, limite) = esEscritura
                        ? ("escritura", EscrituraLimit)
                        : IsLoopback(ip)
                            ? ("local", LocalLimit)
                            : ("general", GeneralLimit);

                    // Las rutas del mismo ámbito comparten contador para una misma IP.
                    var partitionKey = string.Create(CultureInfo.InvariantCulture, $"{ip}_{ambito}");

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey, _ => new FixedWindowRateLimiterOptions {
                            PermitLimit = limite,
                            Window = Window,
                            QueueLimit = 0, // Rechazar inmediatamente, sin cola de espera.
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true 
                        }
                    );
            });
            // Indicar al cliente cuánto debe esperar antes de reintentar.
            options.OnRejected = async (context, cancellationToken) => {
                var retryAfter = 1;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var remaining)) {
                    retryAfter = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
                }

                context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);

                await context.HttpContext.Response.WriteAsJsonAsync(new {
                    message = "Demasiadas solicitudes. Por favor, intente mas tarde.",
                    errorType = "RateLimitError",
                    retryAfter
                }, cancellationToken);
            };
        });
        return services;
    }
    
    /// <summary>
    /// Lee primero las cabeceras de proxy del ejemplo y después la IP de conexión.
    /// </summary>
    private static string ResolveClientIp(HttpContext context) {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && forwarded.Count > 0) {
            var first = forwarded.ToString().Split(',')[0].Trim();
            if (first.Length > 0) return first;
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && realIp.Count > 0) {
            var value = realIp.ToString().Trim();
            if (value.Length > 0) return value;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
    
    /// <summary>Reconoce las direcciones locales contempladas en el ejercicio.</summary>
    private static bool IsLoopback(string ip) => ip is "127.0.0.1" or "::1" or "::ffff:127.0.0.1";
}



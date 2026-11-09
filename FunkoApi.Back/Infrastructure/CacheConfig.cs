using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Factory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using StackExchange.Redis;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
/// Configuración de caché.
/// </summary>
public static class CacheConfig {
    /// <summary>
    /// Registra el servicio de caché
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la app.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration) {
        // Registrar opciones de caché desde appsettings.json
        services.Configure<CacheOptions>(
            configuration.GetSection(CacheOptions.SectionName));

        var cacheOptions = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        var redisHost = configuration["Redis:Host"] ?? "localhost";
        var redisPort = int.TryParse(configuration["Redis:Port"], out var p) ? p : 6379;
        var redisPassword = configuration["Redis:Password"] ?? string.Empty;

        Log.Information("💾 Configurando caché Redis en {Host}:{Port}...", redisHost, redisPort);

        // IDistributedCache (caché simple: Get/Set/Remove)
        services.AddStackExchangeRedisCache(options => {
            options.Configuration = ConnectionStringGenerator.BuildRedis(redisHost, redisPort, redisPassword);
            options.InstanceName = configuration["Redis:RedisInstanceName"] ?? "funkoapi:";
        });

        // IConnectionMultiplexer (operaciones avanzadas: pub/sub, locks, etc.)
        services.AddSingleton<IConnectionMultiplexer>(sp => {
            Log.Information("🔗 Creando ConnectionMultiplexer de Redis...");
            return ConnectionMultiplexer.Connect(ConnectionStringGenerator.BuildRedis(redisHost, redisPort));
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        Log.Information(
            "Caché: Redis (Producción) — TTL: {Minutes} min, Instancia: {Instance}",
            cacheOptions.DefaultExpirationMinutes,
            configuration["Cache:RedisInstanceName"] ?? "funkoapi:");

        return services;
    }
}
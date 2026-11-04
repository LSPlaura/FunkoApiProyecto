using System.Text.Json;
using FunkoApi.Back.Config;
using FunkoApi.Back.Conventers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FunkoApi.Back.Cache;

/// <summary>
/// Implementación de caché distribuida usando IDistributedCache (Redis).
/// Ideal para producción y entornos multi-server.
/// </summary>
public class RedisCacheService(
    IDistributedCache cache,
    IConnectionMultiplexer redis,
    IOptions<CacheOptions> options,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly SemaphoreSlim _factoryLock = new(1, 1);
    private readonly CacheOptions _options = options.Value;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new ObjectIdJsonConverter() }
    };
    private IDatabase db => redis.GetDatabase();

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        logger.LogDebug("Obteniendo elemento de caché con clave '{Key}'...", key);
        
        var bytes = await cache.GetAsync(key, ct);
        if (bytes is null)
        {
            logger.LogDebug("Elemento con clave '{Key}' no encontrado en caché (Cache Miss).", key);
            return default;
        }

        logger.LogDebug("Elemento con clave '{Key}' recuperado exitosamente de la caché (Cache Hit).", key);
        return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        logger.LogDebug("Guardando elemento en caché con clave '{Key}'...", key);
        
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration,
            SlidingExpiration = _options.SlidingExpiration
        };
        
        await cache.SetAsync(key, bytes, cacheOptions, ct);
        logger.LogDebug("Elemento con clave '{Key}' guardado exitosamente en la caché.", key);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        logger.LogInformation("Eliminando elemento de la caché con clave '{Key}'...", key);
        await cache.RemoveAsync(key, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        var exists = bytes is not null;
        
        logger.LogDebug("Comprobando existencia de clave '{Key}' en caché: {Exists}", key, exists);
        return exists;
    }

    /// <inheritdoc />
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory,
        TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        logger.LogDebug("Cache Miss en GetOrSet para la clave '{Key}'. Adquiriendo bloqueo...", key);
        
        await _factoryLock.WaitAsync(ct);
        try
        {
            var lockKey = $"lock:{key}";
            var ganado = await db.StringSetAsync(lockKey, "1",
                TimeSpan.FromSeconds(10), When.NotExists);
                
            if (!ganado)
            {
                logger.LogDebug("No se pudo adquirir el bloqueo distribuido para la clave '{Key}'. Reintentando lectura...", key);
                await Task.Delay(100, ct);
                return await GetAsync<T>(key, ct);
            }

            try
            {
                logger.LogDebug("Bloqueo adquirido para la clave '{Key}'. Ejecutando función de fábrica...", key);
                
                var value = await factory();
                if (value is not null)
                {
                    await SetAsync(key, value, expiration, ct);
                }
                return value;
            }
            finally
            {
                await db.KeyDeleteAsync(new RedisKey(lockKey));
                logger.LogDebug("Bloqueo liberado para la clave '{Key}'.", key);
            }
        }
        finally
        {
            _factoryLock.Release();
        }
    }
}

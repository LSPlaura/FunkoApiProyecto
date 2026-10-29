using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
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
    IOptions<CacheOptions> options) : ICacheService
{
    private readonly SemaphoreSlim _factoryLock = new(1, 1);
    private readonly CacheOptions _options = options.Value;
    private IDatabase db => redis.GetDatabase();

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        if (bytes is null) return default;

        return JsonSerializer.Deserialize<T>(bytes);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration,
            SlidingExpiration = _options.SlidingExpiration
        };
        await cache.SetAsync(key, bytes, cacheOptions, ct);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        return cache.RemoveAsync(key, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        return bytes is not null;
    }

    /// <inheritdoc />
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory,
        TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        await _factoryLock.WaitAsync(ct);
        try
        {
            var lockKey = $"lock:{key}";
            var ganado = await db.StringSetAsync(lockKey, "1",
                TimeSpan.FromSeconds(10), When.NotExists);
            if (!ganado)
            {
                await Task.Delay(100, ct);
                return await GetAsync<T>(key, ct);
            }

            try
            {
                var value = await factory();
                if (value is not null)
                    await SetAsync(key, value, expiration, ct);
                return value;
            }
            finally
            {
                await db.KeyDeleteAsync(new RedisKey(lockKey));
            }
        }
        finally
        {
            _factoryLock.Release();
        }
    }
}
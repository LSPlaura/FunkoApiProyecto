using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace FunkoApi.Back.Services.Proveedor;

public class ProveedorService(IProveedorRepository repository, ICacheService cache, IOptions<CacheOptions> options): IProveedorService
{
    private readonly CacheOptions _options = options.Value;
    
    private readonly string _cacheKey = "proveedor";
    /// <inheritdoc/>
    public async Task<Result<ProveedorDto, DomainError>> Create(CreateProveedorDto item)
    {
        var model = new Models.Proveedor(new ObjectId(""), item.Nombre, false, DateTime.UtcNow, DateTime.UtcNow);
        var result = await repository.Create(model);
        return result.ToDto();
    }

    /// <inheritdoc/>
    public async Task<Result<ProveedorDto, DomainError>> GetById(string key)
    {
        var cached = await cache.GetOrSetAsync<Models.Proveedor>(_cacheKey + key, async() => await repository.GetById(key),
        _options.DefaultExpiration);
        if (cached == null) return ProveedorError.ProveedoraNotFound(key);
        return cached.ToDto();
    }

    /// <inheritdoc/>
    public async Task<Result<bool, DomainError>> SoftDelete(string id)
    {
        await cache.RemoveAsync(_cacheKey + id);
        var funko = await repository.GetById(id);
        if (funko == null) return ProveedorError.ProveedoraNotFound(id);
        return await repository.SoftDelete(id);
    }

    /// <inheritdoc/>
    public async Task<Result<bool, DomainError>> HardDelete(string id)
    {
        await cache.RemoveAsync(_cacheKey + id);
        var funko = await repository.GetById(id);
        if (funko == null) return ProveedorError.ProveedoraNotFound(id);
        return await repository.HardDelete(funko);
    }

    /// <inheritdoc/>
    public async Task<Result<ProveedorDto, DomainError>> Update(UpdateProveedorDto item, string key)
    {
        var oldFunko = await cache.GetAsync<Models.Proveedor>(cache + key);
        if (oldFunko == null)
        {
            oldFunko = await repository.GetById(key);
            if (oldFunko == null)
                return ProveedorError.ProveedoraNotFound(key);
        }

        var updating = oldFunko with
        {
            Nombre = item.Nombre,
            UpdatedAt = DateTime.UtcNow
        };

        var updated = await repository.Update(updating);
        await cache.RemoveAsync(_cacheKey + key);
        return updated != null ? updated.ToDto() : ProveedorError.ProveedoraNotFound(key);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProveedorDto>> GetAll()
    {
        var result = await repository.GetAll();
        return result.Select(p => p.ToDto());
    }
}
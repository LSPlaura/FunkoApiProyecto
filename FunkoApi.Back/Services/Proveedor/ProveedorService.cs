using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace FunkoApi.Back.Services.Proveedor;

public class ProveedorService(
    IProveedorRepository repository,
    ICacheService cache,
    IOptions<CacheOptions> options,
    IFunkoRepository funkoRepository,
    ILogger<ProveedorService> logger) : IProveedorService
{
    private readonly CacheOptions _options = options.Value;
    private readonly string _cacheKey = "proveedor:";

    /// <inheritdoc/>
    public async Task<Result<ProveedorDto, DomainError>> Create(CreateProveedorDto item)
    {
        logger.LogInformation("Creando proveedor con nombre {Nombre}...", item.Nombre);

        var model = new Models.Proveedor(new ObjectId(""), item.Nombre, false, DateTime.UtcNow, DateTime.UtcNow);
        var result = await repository.Create(model);
        logger.LogInformation("Proveedor creado con Id {Id}.", result.Id);

        return result.ToDto();
    }

    /// <inheritdoc/>
    public async Task<Result<ProveedorDto, DomainError>> GetById(string key)
    {
        logger.LogInformation("Obteniendo proveedor con Id {Id}...", key);

        var cached = await cache.GetOrSetAsync<Models.Proveedor>(
            _cacheKey + key,
            async () => await repository.GetById(key),
            _options.DefaultExpiration);

        if (cached == null)
        {
            logger.LogWarning("Proveedor con Id {Id} no encontrado.", key);
            return ProveedorError.ProveedoraNotFound(key);
        }

        logger.LogDebug("Proveedor {Id} obtenido (desde caché o repositorio).", key);
        return cached.ToDto();
    }

    /// <inheritdoc/>
    public async Task<Result<bool, DomainError>> SoftDelete(string id)
    {
        if (await funkoRepository.ExistAnyFunkoProveedor(id))
        {
            logger.LogWarning("No se puede eliminar proveedor {Id} porque tiene funkos asociados.", id);
            return ConflictError.Dependency("proveedor", id);
        }

        logger.LogInformation("Eliminando proveedor {Id} de caché...", id);
        await cache.RemoveAsync(_cacheKey + id);

        var funko = await repository.GetById(id);
        if (funko == null)
        {
            logger.LogWarning("Proveedor con Id {Id} no encontrado para eliminación lógica.", id);
            return ProveedorError.ProveedoraNotFound(id);
        }

        var deleted = await repository.SoftDelete(id);
        logger.LogInformation("Proveedor {Id} eliminado lógicamente. Resultado: {Deleted}", id, deleted);
        return deleted;
    }

    /// <inheritdoc/>
    public async Task<Result<bool, DomainError>> HardDelete(string id)
    {
        if (await funkoRepository.ExistAnyFunkoProveedor(id))
        {
            logger.LogWarning("No se puede eliminar físicamente proveedor {Id} porque tiene funkos asociados.", id);
            return ConflictError.Dependency("proveedor", id);
        }

        logger.LogInformation("Eliminando proveedor {Id} de caché...", id);
        await cache.RemoveAsync(_cacheKey + id);

        var funko = await repository.GetById(id);
        if (funko == null)
        {
            logger.LogWarning("Proveedor con Id {Id} no encontrado para eliminación física.", id);
            return ProveedorError.ProveedoraNotFound(id);
        }

        var deleted = await repository.HardDelete(funko);
        logger.LogInformation("Proveedor {Id} eliminado físicamente. Resultado: {Deleted}", id, deleted);
        return deleted;
    }

    /// <inheritdoc/>
    public async Task<Result<ProveedorDto, DomainError>> Update(UpdateProveedorDto item, string key)
    {
        logger.LogInformation("Actualizando proveedor con Id {Id}...", key);

        var oldFunko = await cache.GetAsync<Models.Proveedor>(_cacheKey + key);
        if (oldFunko == null)
        {
            logger.LogDebug("Proveedor {Id} no está en caché, consultando repositorio...", key);
            oldFunko = await repository.GetById(key);
            if (oldFunko == null)
            {
                logger.LogWarning("Proveedor con Id {Id} no encontrado para actualización.", key);
                return ProveedorError.ProveedoraNotFound(key);
            }
        }

        var updating = oldFunko with
        {
            Nombre = item.Nombre,
            UpdatedAt = DateTime.UtcNow
        };

        var updated = await repository.Update(updating);
        await cache.RemoveAsync(_cacheKey + key);

        if (updated == null)
        {
            logger.LogWarning("Proveedor con Id {Id} no encontrado tras intento de actualización.", key);
            return ProveedorError.ProveedoraNotFound(key);
        }

        logger.LogInformation("Proveedor {Id} actualizado correctamente.", key);
        return updated.ToDto();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProveedorDto>> GetAll()
    {
        logger.LogInformation("Obteniendo todos los proveedores...");

        var result = await repository.GetAll();
        var dtos = result.Select(p => p.ToDto());
        logger.LogInformation("Se obtuvieron {Count} proveedores.", dtos.Count());
        return dtos;
    }
}
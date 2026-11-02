using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Repositories.Proveedor;
using FunkoApi.Back.Storage;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace FunkoApi.Back.Services.Funko;

/// <summary>
///     Servicio encargado de gestionar las operaciones CRUD de Funkos.
///     Comprueba las reglas de negocio, utiliza los repositorios y
///     convierte las entidades a DTOs mediante los mapeadores.
/// </summary>
public class FunkoServices(
    IFunkoRepository funkoRepository,
    ICategoriaRepository categoriaRepository,
    IProveedorRepository proveedorRepository,
    ICacheService cache,
    IOptions<CacheOptions> options,
    IStorageService storage) : IFunkoService
{
    private readonly CacheOptions _options = options.Value;
    
    private string _funkoKey => "funko:";
    private string _funkoFolder => "funkos";

    /// <inheritdoc cref="IFunkoService.Create"/>
    public async Task<Result<FunkoDto, DomainError>> Create(CreateFunkoDto item)
    {
        
        if (!await funkoRepository.ExitsName(item.Nombre))
            return FunkoError.FunkoAlreadyExists(item.Nombre);

        if (await categoriaRepository.ExistsById(item.Categoria))
            return CategoriaError.CategoriaBadRequest(item.Categoria);

        if (await proveedorRepository.ExistsById(item.Proveedor))
            ProveedorError.ProveedorBadRequest(item.Categoria);

        var categoria = await categoriaRepository.GetById(item.Categoria);

        if (categoria is null) return CategoriaError.CategoriaNotFound(item.Categoria);

        if (!storage.IsEmpty(item.Imagen)) return await CreateNoImage(item, categoria);
        
        return await CreateWithImage(item, categoria);
    }
    
    private async Task<Result<FunkoDto, DomainError>> CreateNoImage(CreateFunkoDto item, Models.Categoria categoria)
    {

        var model = new Models.Funko(
            0,
            item.Nombre,
            item.Precio,
            item.Stock,
            Path.Combine(path1, _funkoFolder, fileName),
            item.Categoria,
            categoria,
            ObjectId.Parse(item.Proveedor),
            false,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var result = await funkoRepository.Create(model);
        return result.ToDto();
    }
    
    private async Task<Result<FunkoDto, DomainError>> CreateWithImage(CreateFunkoDto item, Models.Categoria categoria)
    {
        var file = await storage.SaveFileAsync(item.Imagen!, _funkoFolder);
        if (file.IsFailure) return file.Error;

        var model = new Models.Funko(
            0,
            item.Nombre,
            item.Precio,
            item.Stock,
            file.Value,
            item.Categoria,
            categoria,
            ObjectId.Parse(item.Proveedor),
            false,
            DateTime.UtcNow,
            DateTime.UtcNow
        );
        var result = await funkoRepository.Create(model);
        return result.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetById" />
    public async Task<Result<FunkoDto, DomainError>> GetById(long key)
    {
        var funko = await cache.GetOrSetAsync<Models.Funko>(_funkoKey + key,
            async () => await funkoRepository.GetById(key),
            _options.DefaultExpiration);
        if (funko == null) return FunkoError.FunkoNotFound(key);
        return funko.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetByName" />
    public async Task<Result<FunkoDto, DomainError>> GetByName(string name)
    {
        var funko = await cache.GetOrSetAsync<Models.Funko>(_funkoKey + name,
            async () => await funkoRepository.GetByName(name),
            _options.DefaultExpiration);
        if (funko == null) return FunkoError.FunkoNotFoundByName(name);
        return funko.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(long id)
    {
        await cache.RemoveAsync(_funkoKey + id);
        var funko = await funkoRepository.GetById(id);
        if (funko == null) return FunkoError.FunkoNotFound(id);
        return await funkoRepository.SoftDelete(id);
    }

    /// <inheritdoc cref="IFunkoService.HardDelete" />
    public async Task<Result<bool, DomainError>> HardDelete(long id)
    {
        await cache.RemoveAsync(_funkoKey + id);
        var funko = await funkoRepository.GetById(id);
        if (funko == null) return FunkoError.FunkoNotFound(id);
        return await funkoRepository.HardDelete(funko);
    }

    /// <inheritdoc cref="IFunkoService.Update" />
    public async Task<Result<FunkoDto, DomainError>> Update(UpdateFunkoDto item, long key)
    {
        var oldFunkoCached = await cache.GetAsync<Models.Funko>(_funkoKey + key);
        if (oldFunkoCached == null)
            return FunkoError.FunkoNotFound(key);

        var oldFunko = await funkoRepository.GetById(key);
        if (oldFunko == null)
            return FunkoError.FunkoNotFound(key);

        if (!await proveedorRepository.ExistsById(item.Proveedor))
            return ProveedorError.ProveedorBadRequest(item.Proveedor);

        if (!await categoriaRepository.ExistsById(item.Categoria))
            return CategoriaError.CategoriaBadRequest(item.Categoria);

        if (await funkoRepository.ExitsName(item.Nombre) && item.Nombre != oldFunko.Nombre)
            return FunkoError.FunkoAlreadyExists(item.Nombre);

        var updating = oldFunko with
        {
            Nombre = item.Nombre,
            Precio = item.Precio,
            Stock = item.Stock,
            CategoriaId = item.Categoria,
            UpdatedAt = DateTime.UtcNow
        };

        var updated = await funkoRepository.Update(updating);
        await cache.RemoveAsync(_funkoKey + key);
        return updated.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetAll" />
    public async Task<IEnumerable<FunkoDto>> GetAll()
    {
        var lista = await funkoRepository.GetAll();
        return lista.Select(f => f.ToDto());
    }
}

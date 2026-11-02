using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Config;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Repositories.Proveedor;
using FunkoApi.Back.Services.Funko.Transactions;
using FunkoApi.Back.Storage;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Serilog;

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
    IOptions<CacheOptions> cacheOptions,
    IStorageService storage,
    IOptions<StorageSettings> storageOptions,
    FunkoTransactionsService transactionsService
    ) : IFunkoService
{
    private readonly CacheOptions _cacheOptions = cacheOptions.Value;
    private readonly StorageSettings _storageOptions = storageOptions.Value;
    
    private string _funkoKey => "funko:";

    /// <inheritdoc cref="IFunkoService.Create"/>
    public async Task<Result<FunkoDto, DomainError>> Create(CreateFunkoDto item)
    {
        
        if (await funkoRepository.ExitsName(item.Nombre))
            return FunkoError.FunkoAlreadyExists(item.Nombre);

        if (!await categoriaRepository.ExistsById(item.Categoria))
            return CategoriaError.CategoriaBadRequest(item.Categoria);

        if (!await proveedorRepository.ExistsById(item.Proveedor))
            return ProveedorError.ProveedorBadRequest(item.Proveedor);

        var categoria = await categoriaRepository.GetById(item.Categoria);

        if (categoria is null) return CategoriaError.CategoriaNotFound(item.Categoria);

        if (storage.IsEmpty(item.Imagen)) return await CreateNoImage(item, categoria);
        
        return await CreateWithImage(item, categoria);
    }
    
    private async Task<Result<FunkoDto, DomainError>> CreateNoImage(CreateFunkoDto item, Models.Categoria categoria)
    {

        var model = new Models.Funko(
            0,
            item.Nombre,
            item.Precio,
            item.Stock,
            storage.GetRelativePath(_storageOptions.DefaultImage, _storageOptions.Folders["Funkos"]),
            item.Categoria,
            categoria,
            ObjectId.Parse(item.Proveedor),
            false,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        try
        {
            var result = await funkoRepository.Create(model);
            return result.ToDto();
        }
        catch (Exception ex)
        {
            return new ConflictError(ex.Message);
        }
    }
    
    private async Task<Result<FunkoDto, DomainError>> CreateWithImage(CreateFunkoDto item, Models.Categoria categoria)
    {
        var file = await storage.SaveFileAsync(item.Imagen!, _storageOptions.Folders["Funkos"]);
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
        try
        {
            var result = await funkoRepository.Create(model);
            return result.ToDto();
        }
        catch (Exception ex)
        {
            var resultFile = await storage.DeleteFileAsync(file.Value);
            return new ConflictError(ex.Message);
        }
    }

    /// <inheritdoc cref="IFunkoService.GetById" />
    public async Task<Result<FunkoDto, DomainError>> GetById(long key)
    {
        var funko = await cache.GetOrSetAsync<Models.Funko>(_funkoKey + key,
            async () => await funkoRepository.GetById(key),
            _cacheOptions.DefaultExpiration);
        if (funko == null) return FunkoError.FunkoNotFound(key);
        return funko.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetByName" />
    public async Task<Result<FunkoDto, DomainError>> GetByName(string name)
    {
        var funko = await cache.GetOrSetAsync<Models.Funko>(_funkoKey + name,
            async () => await funkoRepository.GetByName(name),
            _cacheOptions.DefaultExpiration);
        if (funko == null) return FunkoError.FunkoNotFoundByName(name);
        return funko.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetPaged" />
    public async Task<(IEnumerable<FunkoDto> Items, int TotalPages, int TotalItems)> GetPagedAsync(int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        var all = await funkoRepository.GetAll();
        var list = all.ToList();
        var totalItems = list.Count;
        
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        
        var items = list.Skip((page - 1) * pageSize).Take(pageSize);
        
        return (items.Select(p => p.ToDto()), totalPages, totalItems);
    }

    /// <inheritdoc cref="IFunkoService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(long id)
    {
        var funko = await cache.GetAsync<Models.Funko>( _funkoKey + id);
        if (funko is null)
        {
            funko = await funkoRepository.GetById(id);
            if (funko is null)
            {
                Log.Warning("Funko con ID {Id} no encontrado ni en caché ni en base de datos", id);
                return FunkoError.FunkoNotFound(id);
            }
        }
        var result = await funkoRepository.SoftDelete(id);
    
        if (result)
        {
            await cache.RemoveAsync(_funkoKey + id);
        }

        return result;
    }

    /// <inheritdoc cref="IFunkoService.HardDelete" />
    public async Task<Result<bool, DomainError>> HardDelete(long id)
    {
        var funko = await cache.GetAsync<Models.Funko>( _funkoKey + id);
        if (funko is null)
        {
            funko = await funkoRepository.GetById(id);
            if (funko is null)
            {
                Log.Warning("Funko con ID {Id} no encontrado ni en caché ni en base de datos", id);
                return FunkoError.FunkoNotFound(id);
            }
        }
        var result = await transactionsService.Delete(funko);
    
        if (result.IsSuccess)
        {
            await cache.RemoveAsync(_funkoKey + id);
        }

        return result;
    }

    /// <inheritdoc cref="IFunkoService.Update" />
    public async Task<Result<FunkoDto, DomainError>> Update(UpdateFunkoDto item, long key)
    {
       
        var oldFunko = await cache.GetAsync<Models.Funko>( _funkoKey + key);
        if (oldFunko is null)
        {
            oldFunko = await funkoRepository.GetById(key);
            if (oldFunko is null)
            {
                Log.Warning("Funko con ID {Id} no encontrado ni en caché ni en base de datos", key);
                return FunkoError.FunkoNotFound(key);
            }
        }
        
        if (!await proveedorRepository.ExistsById(item.Proveedor))
            return ProveedorError.ProveedorBadRequest(item.Proveedor);

        if (!await categoriaRepository.ExistsById(item.Categoria))
            return CategoriaError.CategoriaBadRequest(item.Categoria);
        
        if (item.Nombre != oldFunko.Nombre && await funkoRepository.ExitsName(item.Nombre))
            return FunkoError.FunkoAlreadyExists(item.Nombre);

        if (storage.IsEmpty(item.Imagen)) return new BadRequestError("No se ha mandado la imagen");
        var file = await storage.SaveFileAsync(item.Imagen!, _storageOptions.Folders["Funkos"]);
        if (file.IsFailure) return file.Error;
        
        var updating = oldFunko with
        {
            Nombre = item.Nombre,
            Precio = item.Precio,
            Stock = item.Stock,
            Imagen = file.Value,
            CategoriaId = item.Categoria,
            ProveedorId = ObjectId.Parse(item.Proveedor),
            UpdatedAt = DateTime.UtcNow
        };

       var result = await transactionsService.Update(updating, file.Value, oldFunko.Imagen);
       if (result.IsFailure) return result.Error;
       await cache.RemoveAsync(_funkoKey + key);
       return result.Value.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetAll" />
    public async Task<IEnumerable<FunkoDto>> GetAll()
    {
        var lista = await funkoRepository.GetAll();
        return lista.Select(f => f.ToDto());
    }
}

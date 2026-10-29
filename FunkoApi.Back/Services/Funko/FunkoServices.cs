using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Dtos.Funko;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Services.Funko;

namespace FunkoApi.Back.Services.Crud.Funko;

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
    IOptions<CacheOptions> options) : IFunkoService
{
    private readonly CacheOptions _options = options.Value;

    /// <inheritdoc cref="IFunkoService.Create" />
    private string _funkoKey => "funko:";

    public async Task<Result<FunkoDto, DomainError>> Create(CreateFunkoDto item)
    {
        if (!await funkoRepository.ExitsName(item.Nombre))
            return FunkoError.FunkoAlreadyExists(item.Nombre);

        if (await categoriaRepository.ExistsById(item.Categoria))
            return CategoriaError.CategoriaBadRequest(item.Categoria);

        if (await proveedorRepository.ExistsById(item.Proveedor))
            ProveedorError.ProveedorBadRequest(item.Categoria);

        var model = new Models.Funko(
            0,
            item.Nombre,
            item.Precio,
            item.Stock,
            item.Categoria,
            null,
            item.Proveedor,
            null,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var result = await funkoRepository.Create(model);
        return result.ToDto();
    }

    /// <inheritdoc cref="IFunkoService.GetById" />
    public async Task<Result<FunkoDto, DomainError>> GetById(int key)
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
    public async Task<Result<bool, DomainError>> SoftDelete(int id)
    {
        await cache.RemoveAsync(_funkoKey + id);
        var funko = await funkoRepository.GetById(id);
        if (funko == null) return FunkoError.FunkoNotFound(id);
        return await funkoRepository.SoftDelete(id);
    }

    /// <inheritdoc cref="IFunkoService.HardDelete" />
    public async Task<Result<bool, DomainError>> HardDelete(int id)
    {
        await cache.RemoveAsync(_funkoKey + id);
        var funko = await funkoRepository.GetById(id);
        if (funko == null) return FunkoError.FunkoNotFound(id);
        return await funkoRepository.HardDelete(funko);
    }

    /// <inheritdoc cref="IFunkoService.Update" />
    public async Task<Result<FunkoDto, DomainError>> Update(UpdateFunkoDto item, int key)
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
            ProveedorId = item.Proveedor,
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
using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Services.Get.Categoria;
using Serilog;

namespace FunkoApi.Back.Services.Categoria;

/// <summary>
/// Servicio encargado de gestionar las operaciones de consulta de categorías.
/// Utiliza el repositorio de categorías y los mapeadores para devolver los DTOs.
/// </summary>
public class CategoriaService(
    ICategoriaRepository repository,
    IFunkoRepository funkoRepository,
    ICacheService cache
     ) : ICategoriaService {
    private string _categoriaKey => "categoria:";
    
    /// <inheritdoc cref="ICategoriaService.GetAll" />
    public async Task<IEnumerable<CategoriaDto>> GetAll() {
        Log.Debug("Consultando todas las categorías");
        var lista = await repository.GetAll();
        return lista.Select(c => c.ToDto());
    }
    
    /// <inheritdoc cref="ICategoriaService.GetById" />
    public async Task<Result<CategoriaDto, DomainError>> GetById(string key) {
        Log.Debug("Consultando categoría con ID {Id}", key);
        var cached = await cache.GetAsync<CategoriaDto>(_categoriaKey + key);
        if(cached != null) return Result.Success<CategoriaDto, DomainError>(cached);
        
        var categoria = await repository.GetById(key);
        if (categoria == null) return Result.Failure<CategoriaDto, DomainError>(CategoriaError.CategoriaNotFound(key));
        await cache.SetAsync(_categoriaKey + key, categoria.ToDto());       
        return Result.Success<CategoriaDto, DomainError>(categoria.ToDto());
    }
    
    /// <inheritdoc cref="ICategoriaService.GetByName" />
    public async Task<Result<CategoriaDto, DomainError>> GetByName(string name) {
        Log.Debug("Consultando categoría con nombre {Nombre}", name);
        var categoria = await repository.GetByName(name);
        if (categoria == null) return Result.Failure<CategoriaDto, DomainError>(CategoriaError.CategoriaNotFoundName(name));
        return Result.Success<CategoriaDto, DomainError>(categoria.ToDto());
    }
    
    /// <inheritdoc cref="ICategoriaService.Create" />
    public async Task<Result<CategoriaDto, DomainError>> Create(CreateCategoriaDto item) {
        Log.Debug("Creando categoría con nombre {Nombre}", item.Nombre);
        if (await repository.ExistsByName(item.Nombre)) return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NombreDuplicado(item.Nombre));

        var categoria = new Models.Categoria(item.Nombre, false, DateTime.UtcNow, DateTime.UtcNow);
        var creado = await repository.Create(categoria);
        return Result.Success<CategoriaDto, DomainError>(creado.ToDto());
    }
    
    /// <inheritdoc cref="ICategoriaService.Update" />
    public async Task<Result<CategoriaDto, DomainError>> Update(UpdateCategoriaDto item, string key) {
        Log.Debug("Actualizando categoría con ID {Id}", key);
        
        var existente = await repository.GetById(key);
        if (existente == null) return Result.Failure<CategoriaDto, DomainError>(CategoriaError.CategoriaNotFound(key));
        if (item.Nombre != existente.Nombre && await repository.ExistsByName(item.Nombre)) return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NombreDuplicado(item.Nombre));

        existente = existente with { Nombre = item.Nombre, UpdatedAt = DateTime.UtcNow };
        var actualizado = await repository.Update(existente);
        if (actualizado == null) return Result.Failure<CategoriaDto, DomainError>(CategoriaError.CategoriaNotFound(key));

        await cache.RemoveAsync(_categoriaKey + key);
        return Result.Success<CategoriaDto, DomainError>(actualizado.ToDto());
    }

    /// <inheritdoc cref="ICategoriaService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(string id) {
        Log.Debug("Borrado lógico de categoría con ID {Id}", id);
        if(await funkoRepository.ExistAnyFunkoCategory(id)) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaCanNotDelete(id));
        if(!await repository.ExistsById(id)) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaNotFound(id));
        
        if(!await repository.SoftDelete(id)) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaBadRequest(id));
        
        await cache.RemoveAsync(_categoriaKey + id);
        return Result.Success<bool, DomainError>(true);
    }

    /// <inheritdoc cref="ICategoriaService.HardDelete" />
    public async Task<Result<bool, DomainError>> HardDelete(string id) {
        Log.Debug("Borrado físico de categoría con ID {Id}", id);
        if(await funkoRepository.ExistAnyFunkoCategory(id)) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaCanNotDelete(id));
        if(!await repository.ExistsById(id)) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaNotFound(id));
        
        var eliminado = await repository.GetById(id);
        if(!await repository.HardDelete(eliminado!)) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaBadRequest(id));
        
        await cache.RemoveAsync(_categoriaKey + id);
        return Result.Success<bool, DomainError>(true);
    }
}

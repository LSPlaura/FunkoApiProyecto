using CSharpFunctionalExtensions;
using FunkoApi.Back.Cache;
using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Services.Get.Categoria;

namespace FunkoApi.Back.Services.Categoria;

/// <summary>
/// Servicio encargado de gestionar las operaciones de consulta de categorías.
/// Utiliza el repositorio de categorías y los mapeadores para devolver los DTOs.
/// </summary>
public class CategoriaService(
    ICategoriaRepository repository,
    IFunkoRepository funkoRepository,
    ICacheService cache,
    ILogger<CategoriaService> logger    
    ) : ICategoriaService {
    
    /// <inheritdoc cref="ICategoriaService.GetAll" />
    public async Task<IEnumerable<CategoriaDto>> GetAll() {
        var lista = await repository.GetAll();
        return lista.Select(c => c.ToDto());
    }
    
    /// <inheritdoc cref="ICategoriaService.GetById" />
    public async Task<Result<CategoriaDto, DomainError>> GetById(string key) {
        var cached = await cache.GetAsync<CategoriaDto>(key);
        if(cached != null) return Result.Success<CategoriaDto, DomainError>(cached);
        
        var categoria = await repository.GetById(key);
        if (categoria == null) return Result.Failure<CategoriaDto, DomainError>(CategoriaError.CategoriaNotFound(key));
        return Result.Success<CategoriaDto, DomainError>(categoria.ToDto());
    }
    
    /// <inheritdoc cref="ICategoriaService.GetByName" />
    public async Task<Result<CategoriaDto, DomainError>> GetByName(string name) {
        var categoria = await repository.GetByName(name);
        if (categoria == null) return Result.Failure<CategoriaDto, DomainError>(CategoriaError.CategoriaNotFoundName(name));
        return Result.Success<CategoriaDto, DomainError>(categoria.ToDto());
    }
    
    /// <inheritdoc cref="ICategoriaService.Create" />
    public async Task<Result<CategoriaDto, DomainError>> Create(CreateCategoriaDto item) {
        throw new NotImplementedException();
    }
    
    /// <inheritdoc cref="ICategoriaService.Update" />
    public async Task<Result<CategoriaDto, DomainError>> Update(UpdateCategoriaDto item, string key) {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="ICategoriaService.SoftDelete" />
    public async Task<Result<bool, DomainError>> SoftDelete(string id) {
        if(funkoRepository.ExistAnyFunkoCategory(id).Result) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaCanNotDelete(id));
        if(!repository.ExistsById(id).Result) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaBadRequest(id));
        
        await repository.SoftDelete(id);
        return Result.Success<bool, DomainError>(true);
    }

    /// <inheritdoc cref="ICategoriaService.HardDelete" />
    public async Task<Result<bool, DomainError>> HardDelete(string id) {
        if(funkoRepository.ExistAnyFunkoCategory(id).Result) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaCanNotDelete(id));
        if(!repository.ExistsById(id).Result) return Result.Failure<bool, DomainError>(CategoriaError.CategoriaBadRequest(id));
        
        await repository.HardDelete(repository.GetById(id).Result!);
        return Result.Success<bool, DomainError>(true);
    }
}
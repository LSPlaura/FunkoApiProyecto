using CSharpFunctionalExtensions;
using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Mappers;
using FunkoApi.Back.Repositories.Categoria;

namespace FunkoApi.Back.Services.Get.Categoria;

/// <summary>
///     Servicio encargado de gestionar las operaciones de consulta de categorías.
///     Utiliza el repositorio de categorías y los mapeadores para devolver los DTOs.
/// </summary>
public class CategoriaService(ICategoriaRepository repository) : ICategoriaService
{
    public Task<Result<CategoriaDto, DomainError>> Create(CreateCategoriaDto item)
    {
        throw new NotImplementedException();
    }

    public Task<Result<CategoriaDto, DomainError>> GetById(long key)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool, DomainError>> SoftDelete(long id)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool, DomainError>> HardDelete(long id)
    {
        throw new NotImplementedException();
    }

    public Task<Result<CategoriaDto, DomainError>> Update(UpdateCategoriaDto item, long key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="ICategoriaService.GetAll" />
    public async Task<IEnumerable<CategoriaDto>> GetAll()
    {
        var lista = await repository.GetAll();
        return lista.Select(c => c.ToDto());
    }

    /// <inheritdoc cref="ICategoriaService.GetByName" />
    public async Task<Result<CategoriaDto, DomainError>> GetByName(string name)
    {
        var categoria = await repository.GetByName(name);
        if (categoria == null) return CategoriaError.CategoriaNotFound(name);
        return categoria.ToDto();
    }

    /// <inheritdoc cref="ICategoriaService.GetById" />
    public async Task<Result<CategoriaDto, DomainError>> GetById(int key)
    {
        var categoria = await repository.GetById(key);
        if (categoria == null) return CategoriaError.CategoriaNotFound(key);
        return categoria.ToDto();
    }
}
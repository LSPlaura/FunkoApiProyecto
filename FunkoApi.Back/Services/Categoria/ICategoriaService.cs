using CSharpFunctionalExtensions;
using FunkoApi.Back.Dto.Categoria;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Common;

namespace FunkoApi.Back.Services.Categoria;

/// <summary>
/// Interfaz que define las operaciones de consulta del servicio de categorías.
/// Hereda las operaciones de IGetService y añade la búsqueda por nombre.
/// </summary>
public interface ICategoriaService : ICrudService<CreateCategoriaDto, UpdateCategoriaDto, CategoriaDto, string> {
    /// <summary>
    /// Obtiene una categoría mediante su nombre.
    /// </summary>
    /// <param name="name">Nombre de la categoría que se desea buscar.</param>
    /// <returns>Resultado con el DTO de la categoría encontrada o un error de dominio.</returns>
    Task<Result<CategoriaDto, DomainError>> GetByName(string name);
}
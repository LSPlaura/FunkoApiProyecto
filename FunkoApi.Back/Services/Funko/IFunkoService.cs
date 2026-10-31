using CSharpFunctionalExtensions;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Crud.Common;

namespace FunkoApi.Back.Services.Funko;

/// <summary>
///     Interfaz que define las operaciones del servicio de Funkos.
///     Hereda las operaciones CRUD y añade la búsqueda por nombre.
/// </summary>
public interface IFunkoService : ICrudService<CreateFunkoDto, UpdateFunkoDto, FunkoDto, long> {
    /// <summary>
    /// Obtiene un Funko mediante su nombre.
    /// </summary>
    /// <param name="name">Nombre del Funko que se desea buscar.</param>
    /// <returns>Resultado con el DTO del Funko encontrado o un error de dominio.</returns>
    Task<Result<FunkoDto, DomainError>> GetByName(string name);
}
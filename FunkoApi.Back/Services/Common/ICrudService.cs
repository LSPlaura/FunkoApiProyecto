using CSharpFunctionalExtensions;
using FunkoApi.Back.Errors;

namespace FunkoApi.Back.Services.Common;

/// <summary>
/// Interfaz genérica que define las operaciones CRUD de un servicio,
/// gestionando los resultados y errores de dominio.
/// </summary>
/// <typeparam name="TCreate">Tipo de DTO utilizado para crear una entidad.</typeparam>
/// <typeparam name="TUpdate">Tipo de DTO utilizado para actualizar una entidad.</typeparam>
/// <typeparam name="TDto">Tipo de DTO utilizado para devolver los datos.</typeparam>
/// <typeparam name="TKey">Tipo del identificador de la entidad.</typeparam>
public interface ICrudService<TCreate, TUpdate, TDto, TKey> {
    /// <summary>
    /// Crea una nueva entidad a partir de los datos proporcionados.
    /// </summary>
    /// <param name="item">DTO con los datos necesarios para crear la entidad.</param>
    /// <returns>Resultado con el DTO de la entidad creada o un error de dominio.</returns>
    Task<Result<TDto, DomainError>> Create(TCreate item);

    /// <summary>
    /// Obtiene una entidad mediante su identificador.
    /// </summary>
    /// <param name="key">Identificador de la entidad que se desea consultar.</param>
    /// <returns>Resultado con el DTO de la entidad encontrada o un error de dominio.</returns>
    Task<Result<TDto, DomainError>> GetById(TKey key);

    /// <summary>
    /// Elimina lógicamente una entidad mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador de la entidad que se desea eliminar.</param>
    /// <returns>Resultado con un valor booleano que indica si se ha eliminado o un error de dominio.</returns>
    Task<Result<bool, DomainError>> SoftDelete(TKey id);

    /// <summary>
    /// Elimina físicamente una entidad mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador de la entidad que se desea eliminar definitivamente.</param>
    /// <returns>Resultado con un valor booleano que indica si se ha eliminado o un error de dominio.</returns>
    Task<Result<bool, DomainError>> HardDelete(TKey id);

    /// <summary>
    /// Actualiza una entidad existente a partir de los datos proporcionados.
    /// </summary>
    /// <param name="item">DTO con los nuevos datos de la entidad.</param>
    /// <param name="key">Identificador de la entidad que se desea actualizar.</param>
    /// <returns>Resultado con el DTO de la entidad actualizada o un error de dominio.</returns>
    Task<Result<TDto, DomainError>> Update(TUpdate item, TKey key);

    /// <summary>
    /// Obtiene todas las entidades disponibles.
    /// </summary>
    /// <returns>Colección de DTOs de las entidades encontradas.</returns>
    Task<IEnumerable<TDto>> GetAll();
}
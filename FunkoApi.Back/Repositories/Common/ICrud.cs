namespace FunkoApi.Back.Repositories.Crud.Common;

/// <summary>
///     Interfaz genérica que define las operaciones CRUD de un repositorio.
/// </summary>
/// <typeparam name="TValue">Tipo de entidad que gestiona el repositorio.</typeparam>
/// <typeparam name="TKey">Tipo del identificador de la entidad.</typeparam>
public interface ICrud<TValue, TKey>
{
    /// <summary>
    ///     Crea una nueva entidad en el repositorio.
    /// </summary>
    /// <param name="item">Entidad que se desea crear.</param>
    /// <returns>Entidad creada.</returns>
    Task<TValue> Create(TValue item);

    /// <summary>
    ///     Obtiene una entidad mediante su identificador.
    /// </summary>
    /// <param name="key">Identificador de la entidad que se desea consultar.</param>
    /// <returns>Entidad encontrada o null si no existe.</returns>
    Task<TValue?> GetById(TKey key);

    /// <summary>
    ///     Elimina lógicamente una entidad mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador de la entidad que se desea eliminar.</param>
    /// <returns>True si se elimina correctamente o false en caso contrario.</returns>
    Task<bool> SoftDelete(TKey id);

    /// <summary>
    ///     Elimina físicamente una entidad del repositorio.
    /// </summary>
    /// <param name="item">Entidad que se desea eliminar definitivamente.</param>
    /// <returns>True si se elimina correctamente o false en caso contrario.</returns>
    Task<bool> HardDelete(TValue item);

    /// <summary>
    ///     Actualiza una entidad existente en el repositorio.
    /// </summary>
    /// <param name="item">Entidad con los datos que se desean actualizar.</param>
    /// <returns>Entidad actualizada.</returns>
    Task<TValue> Update(TValue item);

    /// <summary>
    ///     Obtiene todas las entidades del repositorio.
    /// </summary>
    /// <returns>Colección de entidades encontradas.</returns>
    Task<IEnumerable<TValue>> GetAll();
}
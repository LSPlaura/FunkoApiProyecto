using FunkoApi.Back.Repositories.Crud.Common;

namespace FunkoApi.Back.Repositories.Crud.Funko.Common;

/// <summary>
///     Interfaz que define las operaciones del repositorio de Funkos.
///     Hereda las operaciones CRUD y añade consultas por nombre.
/// </summary>
public interface IFunkoRepository : ICrud<Models.Funko, int>
{
    /// <summary>
    ///     Obtiene un Funko por su nombre.
    /// </summary>
    /// <param name="name">Nombre del Funko que se desea buscar.</param>
    /// <returns>Funko encontrado o null si no existe.</returns>
    Task<Models.Funko?> GetByName(string name);

    /// <summary>
    ///     Comprueba si existe un Funko con el nombre indicado.
    /// </summary>
    /// <param name="name">Nombre del Funko que se desea comprobar.</param>
    /// <returns>True si existe un Funko con ese nombre o false en caso contrario.</returns>
    Task<bool> ExitsName(string name);
}
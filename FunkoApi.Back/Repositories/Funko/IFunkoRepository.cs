using FunkoApi.Back.Repositories.Crud.Common;

namespace FunkoApi.Back.Repositories.Funko;

/// <summary>
/// Interfaz que define las operaciones del repositorio de Funkos.
/// Hereda las operaciones CRUD y añade consultas por nombre.
/// </summary>
public interface IFunkoRepository : ICrud<Models.Funko, long> {
    /// <summary>
    /// Obtiene un Funko por su nombre.
    /// </summary>
    /// <param name="name">Nombre del Funko que se desea buscar.</param>
    /// <returns>Funko encontrado o null si no existe.</returns>
    Task<Models.Funko?> GetByName(string name);

    /// <summary>
    /// Obtiene los funkos asociados a una categoría.
    /// </summary>
    /// <param name="key">Identificador de la categoría.</param>
    /// <returns>Funkos de la categoría o una colección vacía si no hay ninguno.</returns>
    Task<IEnumerable<Models.Funko>> GetByCategoriaId(string key);

    /// <summary>
    /// Comprueba si existe un Funko con el nombre indicado.
    /// </summary>
    /// <param name="name">Nombre del Funko que se desea comprobar.</param>
    /// <returns>True si existe un Funko con ese nombre o false en caso contrario.</returns>
    Task<bool> ExitsName(string name);
    
    /// <summary>
    /// Comprueba si hay algun funko que este relacionado con la categoria proporcionada.
    /// </summary>
    /// <param name="key">Id categoria</param>
    /// <returns>True si existe un Funko con esa categoria false si no.</returns>
    Task<bool> ExistAnyFunkoProveedor(string key);
    
    /// <summary>
    /// Comprueba si hay algun funko que este relacionado con el proveedor proporcionado.
    /// </summary>
    /// <param name="key">Id proveedor</param>
    /// <returns>True si existe un Funko con ese proveedor false si no.</returns>
    Task<bool> ExistAnyFunkoCategory(string key);
}

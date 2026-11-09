using FunkoApi.Back.Repositories.Crud.Common;

namespace FunkoApi.Back.Repositories.Proveedor;

/// <summary>
/// Interfaz que define las operaciones del repositorio de proveedores.
/// Hereda las operaciones CRUD base.
/// </summary>
public interface IProveedorRepository : ICrud<Models.Proveedor, string> {
    /// <summary>
    /// Comprueba si existe un proveedor con el identificador indicado.
    /// </summary>
    /// <param name="key">Identificador del proveedor que se desea comprobar.</param>
    /// <returns>True si existe un proveedor con ese id o false en caso contrario.</returns>
    Task<bool> ExistsById(string key);
}
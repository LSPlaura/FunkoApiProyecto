using FunkoApi.Back.Repositories.Crud.Common;

namespace FunkoApi.Back.Repositories.Get.Proveedor;

/// <summary>
///     Interfaz que define las operaciones del repositorio de proveedores.
///     Hereda las operaciones CRUD base y añade consultas específicas por nombre.
/// </summary>
public interface IProveedorRepository : ICrud<Models.Proveedor, long>
{
    /// <summary>
    ///     Obtiene un proveedor mediante su nombre.
    /// </summary>
    /// <param name="name">Nombre del proveedor que se desea buscar.</param>
    /// <returns>Proveedor encontrado o null si no existe.</returns>
    Task<Models.Proveedor?> GetByName(string name);

    /// <summary>
    ///     Comprueba si existe un proveedor con el nombre indicado.
    /// </summary>
    /// <param name="name">Nombre del proveedor que se desea comprobar.</param>
    /// <returns>True si existe un proveedor con ese nombre o false en caso contrario.</returns>
    Task<bool> ExistsByName(string name);
}
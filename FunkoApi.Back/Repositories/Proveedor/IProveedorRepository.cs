using FunkoApi.Back.Repositories.Crud.Common;

namespace FunkoApi.Back.Repositories.Proveedor;

/// <summary>
///     Interfaz que define las operaciones del repositorio de proveedores.
///     Hereda las operaciones CRUD base.
/// </summary>
public interface IProveedorRepository : ICrud<Models.Proveedor, string>
{
    
}
using FunkoApi.Back.Repositories.Crud.Common;
using MongoDB.Bson;

namespace FunkoApi.Back.Repositories.Get.Proveedor;

/// <summary>
///     Interfaz que define las operaciones del repositorio de proveedores.
///     Hereda las operaciones CRUD base.
/// </summary>
public interface IProveedorRepository : ICrud<Models.Proveedor, ObjectId>
{
    
}
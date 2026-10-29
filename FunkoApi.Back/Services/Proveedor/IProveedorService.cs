using CSharpFunctionalExtensions;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Crud.Common;

namespace FunkoApi.Back.Services.Proveedor;

/// <summary>
///     Interfaz que define las operaciones del servicio de proveedores.
///     Hereda las operaciones CRUD base y añade la búsqueda por nombre.
/// </summary>
public interface IProveedorService : ICrudService<CreateProveedorDto, UpdateProveedorDto, ProveedorDto, long>
{
    /// <summary>
    ///     Obtiene un proveedor mediante su nombre.
    /// </summary>
    /// <param name="name">Nombre del proveedor que se desea buscar.</param>
    /// <returns>Resultado con el DTO del proveedor encontrado o un error de dominio.</returns>
    Task<Result<ProveedorDto, DomainError>> GetByName(string name);
}
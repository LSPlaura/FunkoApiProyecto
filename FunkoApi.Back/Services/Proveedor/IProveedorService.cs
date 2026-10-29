using CSharpFunctionalExtensions;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Crud.Common;
using MongoDB.Bson;

namespace FunkoApi.Back.Services.Proveedor;

/// <summary>
///     Interfaz que define las operaciones del servicio de proveedores.
///     Hereda las operaciones CRUD base y añade la búsqueda por nombre.
/// </summary>
public interface IProveedorService : ICrudService<CreateProveedorDto, UpdateProveedorDto, ProveedorDto, ObjectId>
{
}
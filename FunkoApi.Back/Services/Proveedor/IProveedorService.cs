using FunkoApi.Back.Dto.Proveedor;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Services.Common;

namespace FunkoApi.Back.Services.Proveedor;

/// <summary>
/// Interfaz que define las operaciones del servicio de proveedores.
/// Hereda las operaciones CRUD para trabajar con los DTOs y los identificadores de proveedores.
/// </summary>
public interface IProveedorService : ICrudService<CreateProveedorDto, UpdateProveedorDto, ProveedorDto, string> { }

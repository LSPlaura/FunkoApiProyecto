using FunkoApi.Back.Dto.Proveedor;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Services.Common;

namespace FunkoApi.Back.Services.Proveedor;

/// <summary>
/// Interfaz que define las operaciones del servicio de proveedores.
/// Hereda las operaciones CRUD para trabajar con los DTOs y los identificadores de proveedores.
/// </summary>
public interface IProveedorService : ICrudService<CreateProveedorDto, UpdateProveedorDto, ProveedorDto, string> {
    /// <summary>Obtiene una página de proveedores y sus totales.</summary>
    /// <param name="page">Número de página, desde 1.</param>
    /// <param name="pageSize">Tamaño de página, ajustado entre 1 y 100.</param>
    /// <returns>DTOs de la página, número de páginas y total de registros.</returns>
    Task<(IEnumerable<ProveedorDto> Items, int TotalPages, int TotalItems)> GetPaged(int page, int pageSize);
}

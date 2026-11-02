using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Dtos.Proveedor;

namespace FunkoApi.Back.Dto.Funko;

/// <summary>
/// DTO que representa los datos de un Funko, incluyendo su categoría,
/// proveedor, estado de eliminación y fechas de creación y actualización.
/// </summary>
public record FunkoDto(
    long Id,
    string Nombre,
    decimal Precio,
    int Stock,
    string Imagen,
    CategoriaDto Categoria,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
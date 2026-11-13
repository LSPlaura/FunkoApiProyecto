using FunkoApi.Back.Dto.Categoria;
using FunkoApi.Back.Dtos.Proveedor;

namespace FunkoApi.Back.Dto.Funko;

/// <summary>
/// Datos de un funko que se devuelven al cliente, con su categoría y fechas de registro.
/// </summary>
/// <param name="Id">Identificador numérico del funko.</param>
/// <param name="Nombre">Nombre registrado del funko.</param>
/// <param name="Precio">Precio de venta.</param>
/// <param name="Stock">Unidades disponibles.</param>
/// <param name="Imagen">Referencia relativa al archivo de imagen.</param>
/// <param name="Categoria">Datos de la categoría asociada.</param>
/// <param name="IsDeleted">Indica si se ha marcado como eliminado lógicamente.</param>
/// <param name="CreatedAt">Fecha UTC de creación.</param>
/// <param name="UpdatedAt">Fecha UTC de la última actualización.</param>
public record FunkoDto(
    long Id,
    string Nombre,
    decimal Precio,
    int Stock,
    string Imagen,
    CategoriaDto Categoria,
    string Proveedor,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
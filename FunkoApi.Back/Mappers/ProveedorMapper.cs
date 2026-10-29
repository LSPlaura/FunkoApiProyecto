using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Models;
using MongoDB.Bson;

namespace FunkoApi.Back.Mappers;

/// <summary>
///     Métodos de extensión para mapear entre el modelo <see cref="Proveedor" /> y el DTO <see cref="ProveedorDto" />.
/// </summary>
public static class ProveedorMapper
{
    /// <summary>
    ///     Convierte <see cref="Proveedor" /> a su respectivo <see cref="ProveedorDto" />
    /// </summary>
    /// <param name="proveedor">Entidad ProveedorDto a mapear.</param>
    /// <returns>ProveedorDto resultante.</returns>
    public static ProveedorDto ToDto(this Proveedor proveedor)
    {
        return new ProveedorDto(
            proveedor.Id.ToString(),
            proveedor.Nombre,
            proveedor.IsDeleted,
            proveedor.CreatedAt,
            proveedor.UpdatedAt
        );
    }

    /// <summary>
    ///     Convierte un <see cref="ProveedorDto" /> a su respectivo <see cref="Proveedor" />
    /// </summary>
    /// <param name="dto">DTO de categoría a mapear.</param>
    /// <returns>ProveedorDto resultante.</returns>
    public static Proveedor ToModel(this ProveedorDto dto)
    {
        return new Proveedor(
            ObjectId.Parse(dto.Id),
            dto.Nombre,
            dto.IsDeleted,
            dto.CreatedAt,
            dto.UpdatedAt
        );
    }
}
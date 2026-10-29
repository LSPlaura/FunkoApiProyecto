using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Models;

namespace FunkoApi.Back.Mappers;

/// <summary>
///     Métodos de extensión para mapear entre el modelo <see cref="Categoria" /> y el DTO <see cref="CategoriaDto" />.
/// </summary>
public static class CategoriaMapper
{
    /// <summary>
    ///     Convierte <see cref="Categoria" /> a su respectivo <see cref="CategoriaDto" />
    /// </summary>
    /// <param name="categoria">Entidad Categoria a mapear.</param>
    /// <returns>CategoriaDto resultante.</returns>
    public static CategoriaDto ToDto(this Categoria categoria)
    {
        return new CategoriaDto(
            categoria.Id,
            categoria.Nombre,
            categoria.IsDeleted,
            categoria.CreatedAt,
            categoria.UpdatedAt
        );
    }

    /// <summary>
    ///     Convierte un <see cref="CategoriaDto" /> a su respectivo <see cref="Categoria" />
    /// </summary>
    /// <param name="dto">DTO de categoría a mapear.</param>
    /// <returns>Categoria resultante.</returns>
    public static Categoria ToModel(this CategoriaDto dto)
    {
        return new Categoria(
            dto.Id,
            dto.Nombre,
            dto.IsDeleted,
            dto.CreatedAt,
            dto.UpdatedAt
        );
    }
}
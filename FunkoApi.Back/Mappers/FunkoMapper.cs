using FunkoApi.Back.Dtos.Categoria;
using FunkoApi.Back.Dtos.Funko;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Models;

namespace FunkoApi.Back.Mappers;

/// <summary>
///     Métodos de extensión para mapear entre el <see cref="Funko" /> y el DTO <see cref="FunkoDto" />.
/// </summary>
public static class FunkoMapper
{
    /// <summary>
    ///     Convierte <see cref="Funko" /> a su respectivo <see cref="FunkoDto" />
    /// </summary>
    /// <param name="funko">Entidad Funko a mapear.</param>
    /// <returns>FunkoDto resultante.</returns>
    public static FunkoDto ToDto(this Funko funko)
    {
        return new FunkoDto(
            funko.Id,
            funko.Nombre,
            funko.Precio,
            funko.Stock,
            funko.Categoria?.ToDto() ?? new CategoriaDto(string.Empty, "", false, DateTime.UtcNow, DateTime.UtcNow),
            funko.Proveedor?.ToDto() ?? new ProveedorDto("", "", false, DateTime.UtcNow, DateTime.UtcNow),
            funko.IsDeleted,
            funko.CreatedAt,
            funko.UpdatedAt
        );
    }

    // /// <summary>
    // /// Convierte un <see cref="FunkoDto"/>. a su respectivo <see cref="Funko"/>
    // /// </summary>
    // /// <param name="dto">DTO de Funko a mapear.</param>
    // /// <param name="idCategoria">El id de la categoria</param>
    // /// <returns>Funko resultante.</returns>
    // public static Funko ToModel(this FunkoDto dto)
    // {
    //     return new Funko(
    //         dto.Id,
    //         dto.Nombre,
    //         dto.Precio,
    //         dto.Stock,
    //         dto.Categoria.ToModel(),
    //         dto.Proveedor.ToModel(),
    //         dto.IsDeleted,
    //         dto.CreatedAt,
    //         dto.UpdatedAt
    //     );
    // }
}
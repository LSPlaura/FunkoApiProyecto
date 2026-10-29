namespace FunkoApi.Back.Dtos.Categoria;

/// <summary>
///     DTO que representa los datos de una categoría.
/// </summary>
public record CategoriaDto(
    string Id,
    string Nombre,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
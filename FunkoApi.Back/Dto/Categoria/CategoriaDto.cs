namespace FunkoApi.Back.Dto.Categoria;

/// <summary>
/// Representa los datos de una categoría de productos para ser transferidos en las respuestas o consultas de la API.
/// </summary>
/// <param name="Id">Identificador único de la categoría.</param>
/// <param name="Nombre">Nombre descriptivo de la categoría.</param>
/// <param name="IsDeleted">Indica si la categoría se encuentra marcada como eliminada lógicamente (soft delete).</param>
/// <param name="CreatedAt">Fecha y hora en la que se creó originalmente el registro de la categoría.</param>
/// <param name="UpdatedAt">Fecha y hora de la última modificación o actualización realizada en la categoría.</param>
public record CategoriaDto(
    string Id,
    string Nombre,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
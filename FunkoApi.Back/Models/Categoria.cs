namespace FunkoApi.Back.Models;

/// <summary>
///     Modelo que representa una categoría de Funkos en la base de datos.
/// </summary>
public record Categoria(long Id, string Nombre, bool IsDeleted, DateTime CreatedAt, DateTime UpdatedAt);
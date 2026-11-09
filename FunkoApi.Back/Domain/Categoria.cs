namespace FunkoApi.Back.Models;

/// <summary>
///     Modelo que representa una categoría de Funkos en la base de datos.
/// </summary>
public record Categoria(string Nombre, bool IsDeleted, DateTime CreatedAt, DateTime UpdatedAt, string Id = null!);
namespace FunkoApi.Back.Models;

/// <summary>
/// Modelo que representa una categoría de Funkos en la base de datos.
/// </summary>
/// <param name="Nombre">Nombre identificativo de la categoría.</param>
/// <param name="IsDeleted">Marca de borrado lógico.</param>
/// <param name="CreatedAt">Fecha de creación del registro.</param>
/// <param name="UpdatedAt">Fecha de su última actualización.</param>
/// <param name="Id">Identificador de la categoría; si no se aporta, lo genera el contexto al insertarla.</param>
public record Categoria(string Nombre, bool IsDeleted, DateTime CreatedAt, DateTime UpdatedAt, string Id = null!);

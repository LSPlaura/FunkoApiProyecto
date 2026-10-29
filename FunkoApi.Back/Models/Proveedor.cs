namespace FunkoApi.Back.Models;

/// <summary>
///     Modelo que representa un proveedor de Funkos en la base de datos.
/// </summary>
public record Proveedor(long Id, string Nombre, bool IsDeleted, DateTime CreatedAt, DateTime UpdatedAt);
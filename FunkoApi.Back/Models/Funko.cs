namespace FunkoApi.Back.Models;

/// <summary>
/// Modelo que representa un Funko en la base de datos,
/// incluyendo su categoría, proveedor, precio, stock y fechas.
/// </summary>
public record Funko(
    long Id,
    string Nombre,
    decimal Precio,
    int Stock,
    string CategoriaId,
    Categoria? Categoria,
    long ProveedorId,
    Proveedor? Proveedor,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
) {
    /// <summary>
    /// Constructor reservado para que Entity Framework Core materialice la entidad.
    /// EF Core asigna posteriormente las propiedades persistidas, incluidas las
    /// navegaciones <see cref="Categoria" /> y <see cref="Proveedor" />.
    /// El constructor posicional público es el que debe utilizar el código de aplicación.
    /// </summary>
    private Funko() : this(0, string.Empty, 0m, 0, string.Empty, null, 0, null, false, default, default) { }
}
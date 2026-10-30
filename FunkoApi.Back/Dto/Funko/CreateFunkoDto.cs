using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dtos.Funko;

/// <summary>
///     DTO que contiene los datos necesarios para crear un nuevo Funko.
///     Incluye las validaciones de los campos mediante Data Annotations.
/// </summary>
public record CreateFunkoDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio")]
    [Range(0.01, 999999.99, ErrorMessage = "El precio debe estar entre 0.01 y 999999.99")]
    public decimal Precio { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "El stock debe ser mayor a 0")]
    public int Stock { get; init; }

    [Required(ErrorMessage = "La categoría es obligatoria")]
    public long Categoria { get; init; }

    [Required(ErrorMessage = "El proveedor es obligatorio")]
    public long Proveedor { get; init; }
}
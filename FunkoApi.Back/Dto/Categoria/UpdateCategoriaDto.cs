using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dto.Categoria;

/// <summary>
/// Representa los datos necesarios para actualizar una categoría existente en el sistema.
/// </summary>
public record UpdateCategoriaDto {
    
    /// <summary>
    /// Obtiene o establece el nuevo nombre para la categoría.
    /// </summary>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;
}
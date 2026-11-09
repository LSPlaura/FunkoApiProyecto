using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dto.Categoria;

/// <summary>
/// Representa los datos necesarios para crear una nueva categoría en el sistema.
/// </summary>
public record CreateCategoriaDto {
    
    /// <summary>
    /// Obtiene o establece el nombre de la nueva categoría.
    /// </summary>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;
}
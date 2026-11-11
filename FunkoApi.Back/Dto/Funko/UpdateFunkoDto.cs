using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dto.Funko;

/// <summary>
/// DTO que contiene los datos necesarios para actualizar un Funko.
/// Incluye las validaciones de los campos mediante Data Annotations.
/// </summary>
public record UpdateFunkoDto {
    
    /// <summary>Nombre que tendrá el funko después de actualizarlo.</summary>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Nuevo precio de venta sujeto al intervalo de validación.</summary>
    [Required(ErrorMessage = "El precio es obligatorio")]
    [Range(0.01, 999999.99, ErrorMessage = "El precio debe estar entre 0.01 y 999999.99")]
    public decimal Precio { get; init; }

    /// <summary>Número de unidades que se almacenará tras la actualización.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser 0 o mayor")]
    public int Stock { get; init; }
    
    /// <summary>Archivo de reemplazo; aunque el tipo admite null, el servicio actual exige una imagen no vacía.</summary>
    public IFormFile? Imagen { get; init; }

    /// <summary>Identificador de la categoría que se asignará al funko.</summary>
    [Required(ErrorMessage = "La categoría es obligatoria")]
    public string Categoria { get; init; } = string.Empty;

    /// <summary>Identificador textual del proveedor que quedará asociado al funko.</summary>
    [Required(ErrorMessage = "El proveedor es obligatorio")]
    public string Proveedor { get; init; } = string.Empty;
}
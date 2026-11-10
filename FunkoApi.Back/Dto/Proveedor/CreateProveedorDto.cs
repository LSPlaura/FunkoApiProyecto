using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dto.Proveedor;

/// <summary>
/// Datos que recibe el servicio para dar de alta un proveedor.
/// </summary>
public record CreateProveedorDto {
    
    /// <summary>Nombre del proveedor que se quiere incorporar al catálogo.</summary>
    /// <example>Distribuciones Coleccionables</example>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;
}

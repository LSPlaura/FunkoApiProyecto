using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dto.Proveedor;

/// <summary>
/// Datos editables de un proveedor cuyo identificador se recibe por separado.
/// </summary>
public record UpdateProveedorDto {
    /// <summary>Nombre que reemplazará al que tiene actualmente el proveedor.</summary>
    /// <example>Distribuciones Coleccionables Europa</example>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dtos.Proveedor;

public record UpdateProveedorDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;
}
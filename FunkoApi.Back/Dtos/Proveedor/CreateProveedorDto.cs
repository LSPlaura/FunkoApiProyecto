using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dtos.Proveedor;

public record CreateProveedorDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;
}
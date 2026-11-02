using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dto.Auth;

/// <summary>
/// Representa las credenciales de acceso necesarias para que un usuario inicie sesión en el sistema.
/// </summary>
/// <param name="Email">Correo electrónico registrado y único del usuario.</param>
/// <param name="Password">Contraseña de acceso del usuario (mínimo 8 caracteres).</param>
public record LoginDto(
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    string Email,

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener una longitud de al menos 8 caracteres.")]
    string Password
);
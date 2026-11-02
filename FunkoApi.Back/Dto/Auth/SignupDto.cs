using System.ComponentModel.DataAnnotations;

namespace FunkoApi.Back.Dto.Auth;

/// <summary>
/// Representa los datos necesarios para registrar un nuevo usuario en el sistema.
/// </summary>
/// <param name="Email">Correo electrónico único del usuario (servirá como identificador de acceso).</param>
/// <param name="Password">Contraseña segura del usuario (debe cumplir con los requisitos de complejidad: mínimo 8 caracteres, mayúsculas, minúsculas, números y caracteres especiales).</param>
/// <param name="Nombre">Nombre de pila del usuario.</param>
/// <param name="Apellidos">Apellidos del usuario.</param>
public record SignupDto(
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    string Email,

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    string Password,

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(100, ErrorMessage = "Los apellidos no pueden superar los 100 caracteres.")]
    string Apellidos
);
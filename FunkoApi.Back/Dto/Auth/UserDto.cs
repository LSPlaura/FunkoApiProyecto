namespace FunkoApi.Back.Dto.Auth;

/// <summary>
/// Representa la información detallada de un usuario para ser transferida como respuesta al cliente.
/// </summary>
/// <param name="Id">Identificador único del usuario.</param>
/// <param name="Email">Correo electrónico asociado a la cuenta del usuario.</param>
/// <param name="Nombre">Nombre de pila del usuario.</param>
/// <param name="Apellidos">Apellidos del usuario.</param>
/// <param name="Role">Rol o nivel de acceso asignado al usuario en el sistema.</param>
public record UserDto(long Id, string Email, string Nombre, string Apellidos, string Role);
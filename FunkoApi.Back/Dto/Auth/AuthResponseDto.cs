namespace FunkoApi.Back.Dto.Auth;

/// <summary>
/// Representa la respuesta de autenticación exitosa enviada al cliente tras iniciar sesión o registrarse, 
/// incluyendo el token de acceso JWT, los datos básicos del usuario y su tiempo de expiración.
/// </summary>
/// <param name="Token">Token de acceso JWT generado para autenticar las peticiones posteriores del usuario.</param>
/// <param name="Email">Correo electrónico asociado a la cuenta del usuario autenticado.</param>
/// <param name="Role">Rol o perfil de seguridad asignado al usuario: ADMIN o USER.</param>
/// <param name="ExpiresAt">Fecha y hora UTC en la que el token perderá su validez.</param>
public record AuthResponseDto(string Token, string Email, string Role, DateTime ExpiresAt);

using System.Security.Claims;
using FunkoApi.Back.Dto.Auth;
using FunkoApi.Back.Dto.Documentation;
using Swashbuckle.AspNetCore.Annotations;
using FunkoApi.Back.Extensions;
using FunkoApi.Back.Services.Auth.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Back.Controllers;

/// <summary>
/// Controller de autenticación: login, signup y perfil.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase {
    /// <summary>
    /// Inicia sesión con email y contraseña.
    /// </summary>
    /// <param name="dto">Email válido y contraseña de entre 8 y 100 caracteres.</param>
    /// <remarks>Operación pública. Devuelve token, email, role y expiresAt. No envía un email ni crea un usuario.</remarks>
    [HttpPost("login")]
    [SwaggerOperation(OperationId = "Auth_Login")]
    [SwaggerResponse(200, "Credenciales correctas; JWT y datos de autenticación.", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Credenciales incompletas, JSON inválido o validación de campos.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(401, "Email no registrado o contraseña incorrecta.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto) {
        var result = await authService.LoginAsync(dto);
        return result.IsSuccess ? Ok(result.Value) : result.Error.ToHttpResult<AuthResponseDto>();
    }

    /// <summary>
    /// Registra un nuevo usuario.
    /// </summary>
    /// <param name="dto">Email único, contraseña de 8 a 100 caracteres, nombre (máximo 50) y apellidos (máximo 100).</param>
    /// <remarks>
    /// Operación pública. Identity exige mayúscula, minúscula, dígito y carácter no alfanumérico en la contraseña.
    /// Asigna el rol USER. Actualmente responde 200 con un JWT, no 201. No envía un correo de bienvenida.
    /// </remarks>
    [HttpPost("signup")]
    [SwaggerOperation(OperationId = "Auth_Signup")]
    [SwaggerResponse(200, "Usuario registrado con rol USER; JWT y datos de autenticación.", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "JSON o campos inválidos.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(409, "Ya existe un usuario con ese email.", typeof(MessageErrorResponse))]
    [SwaggerResponse(422, "Identity rechaza la creación, por ejemplo por la complejidad de contraseña.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<AuthResponseDto>> Signup([FromBody] SignupDto dto) {
        var result = await authService.SignupAsync(dto);
        return result.IsSuccess ? Ok(result.Value) : result.Error.ToHttpResult<AuthResponseDto>();
    }

    /// <summary>
    /// Obtiene la información del usuario autenticado.
    /// </summary>
    /// <remarks>El identificador se obtiene del claim NameIdentifier del JWT, no de un parámetro de entrada.</remarks>
    [Authorize]
    [HttpGet("me")]
    [SwaggerOperation(OperationId = "Auth_Me")]
    [SwaggerResponse(200, "Perfil: id, email, nombre, apellidos y role. No incluye contraseña ni token.", typeof(UserDto))]
    [SwaggerResponse(404, "El usuario identificado por el token ya no existe.", typeof(MessageErrorResponse))]
    public async Task<ActionResult<UserDto>> GetMe() {
        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await authService.GetMeAsync(userId);
        return result.IsSuccess ? Ok(result.Value) : result.Error.ToHttpResult<UserDto>();
    }
}

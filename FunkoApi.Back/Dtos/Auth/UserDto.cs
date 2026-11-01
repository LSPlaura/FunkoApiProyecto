namespace FunkoApi.Back.Dtos.Auth;

public record UserDto(long Id, string Email, string Nombre, string Apellidos, string Role);
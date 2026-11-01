namespace FunkoApi.Back.Dtos.Auth;

public record AuthResponseDto(string Token, string Email, string Role, DateTime ExpiresAt);
using Microsoft.AspNetCore.Identity;

namespace FunkoApi.Back.Models;

/// <summary>
/// Usuario personalizado que hereda de IdentityUser.
/// Los campos Nombre y Apellidos se usan en el JWT y en la API.
/// </summary>
public class AppUser : IdentityUser<long>
{
    public string Nombre { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;
    
    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

using Microsoft.AspNetCore.Identity;

namespace FunkoApi.Back.Models;

/// <summary>
/// Usuario personalizado que hereda de IdentityUser.
/// Los campos Nombre y Apellidos se usan en el JWT y en la API.
/// </summary>
public class AppUser : IdentityUser<long> {
    /// <summary>Nombre del usuario utilizado en sus datos de perfil y en el JWT.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Apellidos que completan los datos del perfil del usuario.</summary>
    public string Apellidos { get; set; } = string.Empty;
    
    /// <summary>Marca de borrado lógico almacenada en el registro del usuario.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Fecha UTC de creación del registro.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Fecha UTC almacenada para la última actualización del registro.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

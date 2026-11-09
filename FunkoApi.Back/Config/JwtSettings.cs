namespace FunkoApi.Back.Config;

/// <summary>
/// Representa la configuración para la autenticación y emisión de JSON Web Tokens (JWT).
/// Se mapea automáticamente desde la sección correspondiente en el archivo de configuración (<c>appsettings.json</c>).
/// </summary>
public class JwtSettings {
    /// <summary>
    /// Nombre de la sección en el archivo de configuración (por defecto: "Jwt").
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Obtiene o establece la clave secreta utilizada para firmar criptográficamente y verificar los tokens. 
    /// Debe mantenerse segura y tener una longitud mínima recomendada.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el emisor (<c>issuer</c>) del token, que identifica a la aplicación o servicio que genera el JWT.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la audiencia (<c>audience</c>) del token, que identifica a los destinatarios o clientes permitidos para consumir la API.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el tiempo de expiración del token expresado en minutos. 
    /// Por defecto está configurado a <c>30</c> minutos.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 30;
}

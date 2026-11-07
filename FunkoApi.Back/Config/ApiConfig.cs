namespace FunkoApi.Back.Config;

/// <summary>
/// Configuración de la API.
/// Se lee de la sección "Api" de appsettings.json.
/// Permite cambiar nombre y mensaje según el entorno.
/// </summary>
public class ApiConfig {
    /// <summary>
    /// Nombre con el que se identifica la API en los mensajes de arranque.
    /// </summary>
    public string Nombre { get; set; } = "Funko API";
    
    /// <summary>
    /// Texto informativo que se muestra junto al nombre de la API al iniciar.
    /// </summary>
    public string Mensaje { get; set; } = "Modo producción";
}

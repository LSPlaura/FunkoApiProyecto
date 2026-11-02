namespace FunkoApi.Back.Config;

/// <summary>
/// Representa la configuración para la gestión y almacenamiento de archivos y ficheros subidos al servidor.
/// Se mapea automáticamente desde la sección correspondiente en el archivo de configuración (<c>appsettings.json</c>).
/// </summary>
public class StorageSettings
{
    /// <summary>
    /// Nombre de la sección en el archivo de configuración (por defecto: "Storage").
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// Obtiene o establece la ruta base en el servidor donde se almacenarán físicamente los archivos cargados.
    /// </summary>
    public string UploadPath { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el tamaño máximo permitido para los archivos subidos (por ejemplo, representado como cadena tipo "5MB").
    /// </summary>
    public string MaxFileSize { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece las extensiones de archivo permitidas para la validación de seguridad (separadas por comas o en formato listado).
    /// </summary>
    public string AllowedExtensions { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece los tipos de contenido MIME permitidos para validar las subidas (por ejemplo, "image/jpeg,image/png").
    /// </summary>
    public string AllowedContentTypes { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece la ruta o nombre de la imagen predeterminada que se mostrará o utilizará como respaldo.
    /// </summary>
    public string DefaultImage { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece un diccionario que mapea nombres lógicos de carpetas o subdirectorios específicos, 
    /// configurado con sensibilidad independiente de mayúsculas y minúsculas (<see cref="StringComparer.OrdinalIgnoreCase"/>).
    /// </summary>
    public Dictionary<string, string> Folders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
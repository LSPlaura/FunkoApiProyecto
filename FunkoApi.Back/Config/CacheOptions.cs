namespace FunkoApi.Back.Config;

/// <summary>
///     Opciones de configuración de caché.
/// </summary>
public class CacheOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string RedisInstanceName { get; set; } = string.Empty;
    /// <summary>
    ///     Sección de configuración.
    /// </summary>
    public const string SectionName = "Redis";

    /// <summary>
    ///     Tiempo de vida por defecto de las entradas en caché (en minutos), se borra aunque se consulte.
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    ///     Tiempo de expiración deslizante (en minutos).
    ///     Si el dato se cosulta antes de que acabe el tiemppo el tiempos se renueva.
    /// </summary>
    public int SlidingExpirationMinutes { get; set; } = 10;

    /// <summary>
    ///     Convierte los minutos a TimeSpan.
    /// </summary>
    public TimeSpan DefaultExpiration => TimeSpan.FromMinutes(DefaultExpirationMinutes);

    /// <summary>
    ///     Convierte los minutos a TimeSpan.
    /// </summary>
    public TimeSpan SlidingExpiration => TimeSpan.FromMinutes(SlidingExpirationMinutes);
}
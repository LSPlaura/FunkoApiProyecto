using FunkoApi.Back.Storage;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
/// Configuración del registro del servicio de almacenamiento de imágenes.
/// </summary>
public static class StorageConfig {
    /// <summary>
    /// Registra FileSystemStorageService como implementación de IStorageService con duración Scoped.
    /// </summary>
    /// <param name="services">Colección de servicios del contenedor de dependencias.</param>
    /// <returns>La colección de servicios para permitir encadenar otros registros.</returns>
    public static IServiceCollection AddStorage(this IServiceCollection services) {
        Log.Information("Configurando servicio de almacenamiento de imagenes..");
        return services.AddScoped<IStorageService, FileSystemStorageService>();
    }
}

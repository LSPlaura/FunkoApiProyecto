using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
/// Extensiones para preparar el directorio de almacenamiento de imágenes.
/// </summary>
public static class StorageInitializationExtensions {
    
    /// <summary>
    /// Prepara el directorio uploads: elimina sus archivos y subdirectorios si existe,
    /// o lo crea si no existe.
    /// </summary>
    /// <param name="app">Aplicación de la que se obtienen las rutas del entorno.</param>
    public static void InitializeStorage(this WebApplication app) {
        var rootPath = app.Environment.WebRootPath ?? app.Environment.ContentRootPath;
        var storagePath = Path.Combine(rootPath, "uploads");
        var storageDirectory = new DirectoryInfo(storagePath);

        try {
            Log.Information("Preparando directorio de almacenamiento: {Path}", storagePath);
            if (storageDirectory.Exists) {
                foreach (var file in storageDirectory.GetFiles()) file.Delete();
                foreach (var dir in storageDirectory.GetDirectories()) dir.Delete(true);
                Log.Information("Se ha borrado el contenido del directorio.");
            }
            if (!storageDirectory.Exists) storageDirectory.Create();
        } catch (Exception e) {
            Log.Error(e, "Ha ocurrido un error al preparar el almacenamiento de imagenes.");
        }
    }
}

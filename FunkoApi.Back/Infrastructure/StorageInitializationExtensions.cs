using FunkoApi.Back.Config;
using Path = System.IO.Path;
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
    /// <param name="configuration">Configuración de la app.</param>
   public static void InitializeStorage(this WebApplication app, IConfiguration configuration) {
    var rootPath = app.Environment.WebRootPath ?? app.Environment.ContentRootPath;
    var storagePath = Path.Combine(rootPath, "uploads");
    var storageDirectory = new DirectoryInfo(storagePath);

    try {
        Log.Information("Preparando directorio de almacenamiento: {Path}", storagePath);
        
        // Si la carpeta uploads no existe, la creamos y añadimos un .gitkeep
        if (!storageDirectory.Exists) {
            Directory.CreateDirectory(storagePath);
            
            // Creamos un archivo .gitkeep vacío para que Git/Docker reconozcan la carpeta
            var gitKeepPath = Path.Combine(storagePath, ".gitkeep");
            File.WriteAllText(gitKeepPath, string.Empty);
            
            Log.Information("Se ha creado el directorio uploads y el archivo .gitkeep.");
        } else {
            // Si ya existe, limpiamos su contenido pero podemos conservar el .gitkeep si queremos
            foreach (var file in storageDirectory.GetFiles()) {
                if (file.Name != ".gitkeep") {
                    file.Delete();
                }
            }
            foreach (var dir in storageDirectory.GetDirectories()) dir.Delete(true);
            Log.Information("Se ha limpiado el contenido del directorio (preservando .gitkeep si existía).");
        }

        var sourceSeedPath = Path.Combine(app.Environment.ContentRootPath, "SeedImages");
        
        if (Directory.Exists(sourceSeedPath)) {
            var storageSettings = configuration
                .GetSection(StorageSettings.SectionName)
                .Get<StorageSettings>() ?? new StorageSettings();
            
            foreach (var (key, domainFolderName) in storageSettings.Folders) {
                var targetDirectory = Path.Combine(storagePath, domainFolderName);
                Directory.CreateDirectory(targetDirectory);
                
                // Opcional: crear un .gitkeep en las subcarpetas
                var subGitKeep = Path.Combine(targetDirectory, ".gitkeep");
                if (!File.Exists(subGitKeep)) {
                    File.WriteAllText(subGitKeep, string.Empty);
                }

                Log.Information("Carpeta creada/configurada -> {Nombre}: {Path}", key, targetDirectory);
                
                var seedDirectory = Path.Combine(sourceSeedPath, domainFolderName);
                if (Directory.Exists(seedDirectory)) {
                    CopyDirectoryContents(seedDirectory, targetDirectory);
                    Log.Information("Archivos iniciales copiados para {Nombre} desde {Source}", key, seedDirectory);
                }
            }
        } else {
            Log.Warning("La carpeta SeedImages no existe en el contenedor, se omite el sembrado inicial.");
        }
        
    } catch (Exception e) {
        Log.Error(e, "Ha ocurrido un error al preparar y sembrar el almacenamiento de imágenes.");
    }
}

    /// <summary>
    /// Método auxiliar para copiar recursivamente archivos y subcarpetas.
    /// </summary>
    private static void CopyDirectoryContents(string sourceDir, string targetDir) {
        foreach (var file in Directory.GetFiles(sourceDir)) {
            
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(targetDir, fileName);
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir)) {
            var dirName = Path.GetFileName(subDir);
            var destSubDir = Path.Combine(targetDir, dirName);
            Directory.CreateDirectory(destSubDir);
            CopyDirectoryContents(subDir, destSubDir);
        }
    }
}

using CSharpFunctionalExtensions;
using FunkoApi.Back.Errors;

namespace FunkoApi.Back.Storage;

/// <summary>
/// Gestiona los archivos en el disco del servidor y las rutas utilizadas para localizarlos.
/// Comprueba los archivos recibidos antes de guardarlos y devuelve los fallos mediante Result.
/// </summary>
public class FileSystemStorageService : IStorageService {
    
    private readonly string _rootPath;
    private readonly string _uploadPath;
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;
    private readonly string[] _allowedContentTypes;
    private readonly ILogger<FileSystemStorageService> _logger;

    /// <summary>
    /// Lee los límites y tipos admitidos, calcula la ubicación del almacenamiento
    /// y crea su carpeta si todavía no existe, sin borrar su contenido.
    /// </summary>
    /// <param name="configuration">Configuración que contiene la sección Storage.</param>
    /// <param name="logger">Registrador de los mensajes y errores de este servicio.</param>
    /// <param name="env">Entorno del que se obtiene la raíz web o, si es nula, la raíz del proyecto.</param>
    public FileSystemStorageService(IConfiguration configuration, ILogger<FileSystemStorageService> logger, IWebHostEnvironment env) {
        _logger = logger;

        _uploadPath = configuration["Storage:UploadPath"] ?? "uploads";
        _maxFileSize = configuration.GetValue<long>("Storage:MaxFileSize", 5 * 1024 * 1024);
        _allowedExtensions = configuration.GetSection("Storage:AllowedExtensions").Get<string[]>() ?? [".jpg", ".jpeg", ".png", ".gif"];
        _allowedContentTypes = configuration.GetSection("Storage:AllowedContentTypes").Get<string[]>() ?? ["image/jpeg", "image/png", "image/gif"];

        var webRoot = env.WebRootPath ?? env.ContentRootPath;
        _rootPath = Path.Combine(webRoot, _uploadPath);

        if (!Directory.Exists(_rootPath)) Directory.CreateDirectory(_rootPath);

        _logger.LogInformation("Storage service inicializado en: {Path}", _rootPath);
    }

    /// <summary>
    /// Valida el archivo y lo escribe en la carpeta indicada con un nombre generado.
    /// </summary>
    /// <param name="file">Archivo recibido que se quiere guardar.</param>
    /// <param name="folder">Subcarpeta de destino dentro del almacenamiento.</param>
    /// <returns>Un resultado con la referencia relativa al archivo o con el error de validación o guardado.</returns>
    public Task<Result<string, DomainError>> SaveFileAsync(IFormFile file, string folder) {
        var validation = ValidateFile(file);
        if (validation.IsFailure) return Task.FromResult(Result.Failure<string, DomainError>(validation.Error));

        try {
            var filename = GenerateUniqueFilename(file.FileName);
            var folderPath = Path.Combine(_rootPath, folder);
            Directory.CreateDirectory(folderPath);
            
            var filePath = Path.Combine(folderPath, filename);
            var relativePath = GetRelativePath(filename, folder);
            
            using var stream = new FileStream(filePath, FileMode.Create);
            file.CopyTo(stream);
            
            _logger.LogInformation("Archivo guardado: {Path}", relativePath);
            return Task.FromResult(Result.Success<string, DomainError>(relativePath));
        } catch (Exception e) {
            _logger.LogError(e, "Error guardando archivo");
            return Task.FromResult(Result.Failure<string, DomainError>(StorageError.ErrorGuardando()));
        }
    }

    /// <summary>
    /// Intenta borrar el archivo indicado. Si no existe o no se proporciona una ruta,
    /// considera la operación completada sin error.
    /// </summary>
    /// <param name="filename">Nombre o ruta del archivo que se quiere eliminar.</param>
    /// <returns>Un resultado con true si no se produce una excepción, o con el error de borrado.</returns>
    public Task<Result<bool, DomainError>> DeleteFileAsync(string filename) {
        if (string.IsNullOrEmpty(filename)) return Task.FromResult(Result.Success<bool, DomainError>(true));
        
        try {
            var fullPath = GetFullPath(filename);
            if (File.Exists(fullPath)) {
                File.Delete(fullPath);
                _logger.LogInformation("Archivo eliminado: {Filename}", filename);
            }
            return Task.FromResult(Result.Success<bool, DomainError>(true));
        } catch (Exception e) {
            _logger.LogError(e, "Error eliminando archivo {Filename}", filename);
            return Task.FromResult(Result.Failure<bool, DomainError>(StorageError.ErrorEliminando()));
        }
    }

    /// <summary>
    /// Consulta si hay un archivo en la ubicación resuelta por GetFullPath.
    /// </summary>
    /// <param name="filename">Nombre o ruta que identifica el archivo.</param>
    /// <returns>True si existe; false si no se encuentra o el argumento es nulo o vacío.</returns>
    public bool FileExists(string filename) {
        if (string.IsNullOrEmpty(filename)) return false;
        return File.Exists(GetFullPath(filename));
    }

    /// <summary>
    /// Devuelve sin cambios las rutas que Path.IsPathRooted reconoce como enraizadas.
    /// Para las demás, retira el prefijo de uploads si aparece y las combina con la raíz del almacenamiento.
    /// </summary>
    /// <param name="filename">Nombre o ruta que se quiere resolver.</param>
    /// <returns>La ruta recibida si está enraizada, o la ruta combinada con el almacenamiento.</returns>
    public string GetFullPath(string filename) {
        if (Path.IsPathRooted(filename)) return filename;
        var prefix = $"/{_uploadPath}/";
        
        if (filename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) filename = filename[prefix.Length..];
        return Path.Combine(_rootPath, filename);
    }

    /// <summary>
    /// Construye una referencia al archivo sin incluir la ubicación física del proyecto.
    /// </summary>
    /// <param name="filename">Nombre del archivo que aparecerá en la referencia.</param>
    /// <param name="folder">Subcarpeta del archivo; se utiliza funkos si no se indica otra.</param>
    /// <returns>Una ruta con el formato /{carpeta de subidas}/{subcarpeta}/{archivo}.</returns>
    public string GetRelativePath(string filename, string folder = "funkos") {
        return $"/{_uploadPath}/{folder}/{filename}";
    }
    
    /// <summary>
    /// Revisa que el archivo tenga contenido, no supere el tamaño permitido
    /// y cumpla las comprobaciones de extensión, tipo de contenido y nombre.
    /// </summary>
    /// <param name="file">Archivo que se comprueba antes de escribirlo en disco.</param>
    /// <returns>Éxito sin un valor asociado, o el primer error detectado.</returns>
    private UnitResult<DomainError> ValidateFile(IFormFile file) {
        if (file is null or { Length: 0 }) return UnitResult.Failure<DomainError>(StorageError.ArchivoVacio());

        if (file.Length > _maxFileSize) return UnitResult.Failure<DomainError>(StorageError.ArchivoMuyGrande());

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension)) return UnitResult.Failure<DomainError>(StorageError.ExtensionNoPermitida());

        var contentType = file.ContentType?.ToLowerInvariant();
        if (contentType is null || !_allowedContentTypes.Any(ct => contentType.Contains(ct.Split('/')[1]))) return UnitResult.Failure<DomainError>(StorageError.TipoContenidoNoPermitido());

        var filename = Path.GetFileName(file.FileName);
        if (filename.Contains("..") || filename.Contains('/') || filename.Contains('\\')) return UnitResult.Failure<DomainError>(StorageError.NombreArchivoInvalido());

        return UnitResult.Success<DomainError>();
    }
    
    /// <summary>
    /// Crea un nombre combinando la fecha UTC, parte de un GUID y el nombre original
    /// con espacios y guiones sustituidos por guiones bajos. Conserva la extensión en minúsculas.
    /// </summary>
    /// <param name="originalFilename">Nombre recibido del que se extraen la base y la extensión.</param>
    /// <returns>Un nombre generado para reducir las coincidencias entre archivos guardados.</returns>
    private static string GenerateUniqueFilename(string originalFilename) {
        var extension = Path.GetExtension(originalFilename).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var sanitizedName = Path.GetFileNameWithoutExtension(originalFilename).Replace(" ", "_").Replace("-", "_");
        return $"{timestamp}_{uniqueId}_{sanitizedName}{extension}";
    }
}

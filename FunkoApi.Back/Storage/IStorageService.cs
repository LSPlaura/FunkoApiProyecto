using CSharpFunctionalExtensions;
using FunkoApi.Back.Errors;

namespace FunkoApi.Back.Storage;

/// <summary>
/// Define las operaciones que necesita la API para gestionar los archivos y sus rutas.
/// </summary>
public interface IStorageService {
    
    /// <summary>
    /// Almacena el archivo recibido en la carpeta indicada y devuelve su ubicación relativa.
    /// </summary>
    /// <param name="file">Archivo enviado a la API que se quiere almacenar.</param>
    /// <param name="folder">Carpeta del almacenamiento en la que se guardará.</param>
    /// <returns>Un resultado con la ruta del archivo si se guarda correctamente, o con el error ocurrido.</returns>
    Task<Result<string, DomainError>> SaveFileAsync(IFormFile file, string folder);
    
    /// <summary>
    /// Borra del almacenamiento el archivo identificado por su ruta relativa.
    /// </summary>
    /// <param name="filename">Ruta relativa que permite localizar el archivo que se quiere borrar.</param>
    /// <returns>Un resultado con true si se borra, false si no se encuentra, o un error si falla la operación.</returns>
    Task<Result<bool, DomainError>> DeleteFileAsync(string filename);
    
    /// <summary>
    /// Indica si el archivo indicado se encuentra en el almacenamiento.
    /// </summary>
    /// <param name="filename">Nombre o ruta relativa del archivo que se quiere buscar.</param>
    /// <returns>True si se encuentra el archivo; false si no existe.</returns>
    bool FileExists(string filename);
    
    /// <summary>
    /// Resuelve la ubicación completa del archivo en el disco del servidor.
    /// </summary>
    /// <param name="filename">Nombre o ruta relativa a partir de la que se localizará el archivo.</param>
    /// <returns>La ruta física completa, no una dirección web.</returns>
    string GetFullPath(string filename);

    /// <summary>
    /// Construye la referencia al archivo que se puede guardar en la base de datos,
    /// sin incluir la ruta física del servidor.
    /// </summary>
    /// <param name="filename">Nombre que tendrá el archivo en la referencia.</param>
    /// <param name="folder">Carpeta a la que pertenece; se utiliza funkos si no se indica otra.</param>
    /// <returns>Una referencia con la estructura /uploads/{carpeta}/{nombre}.</returns>
    string GetRelativePath(string filename, string folder = "funkos");
}

namespace FunkoApi.Back.Errors;

/// <summary>
/// Clase que proporciona los errores relacionados con las operaciones de categorías.
/// </summary>
public static class CategoriaError {
    /// <summary>Crea un error cuando otra categoría ya tiene ese nombre.</summary>
    /// <param name="nombre">Nombre duplicado.</param>
    /// <returns>Error de conflicto.</returns>
    public static ConflictError NombreDuplicado(string nombre) {
        return new ConflictError($"Ya existe una categoría con el nombre {nombre}");
    }

    /// <summary>
    /// Genera un error cuando no se encuentra una categoría por su identificador.
    /// </summary>
    /// <param name="id">Identificador de la categoría que no se ha encontrado.</param>
    /// <returns>Error de tipo NotFoundError.</returns>
    public static NotFoundError CategoriaNotFound(string id) {
        return new  NotFoundError($"Categoria con ID {id} no encontrado");
    }

    /// <summary>
    /// Genera un error cuando no se encuentra una categoría por su nombre.
    /// </summary>
    /// <param name="name">Nombre de la categoría que no se ha encontrado.</param>
    /// <returns>Error de tipo NotFoundError.</returns>
    public static NotFoundError CategoriaNotFoundName(string name) {
        return NotFoundError.FromName(name, "Categoria");
    }

    /// <summary>
    /// Genera un error de solicitud incorrecta relacionado con una categoría.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <returns>Error de tipo BadRequestError.</returns>
    public static BadRequestError CategoriaBadRequest(string id) {
        return new BadRequestError($"Categoria con el ID {id} no encontrada");
    }
    
    /// <summary>
    /// Genera un error de solicitud incorrecta relacionado con una categoría.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <returns>Error de tipo BadRequestError.</returns>
    public static BadRequestError CategoriaCanNotDelete(string id) {
        return new BadRequestError($"Categoria con el ID {id} se esta usando en funkos existentes y no se puede borrar.");
    }

    /// <summary>
    /// Genera un error de conflicto al realizar una operación sobre una categoría.
    /// </summary>
    /// <param name="id">Identificador de la categoría con la que se ha producido el conflicto.</param>
    /// <returns>Error de tipo ConflictError.</returns>
    public static ConflictError CategoriaConflictError(int id) {
        return new ConflictError($"Se han tenido problemas a la hora de operar con la Categoria con el ID {id}");
    }
}

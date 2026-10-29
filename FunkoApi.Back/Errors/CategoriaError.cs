namespace FunkoApi.Back.Errors;

/// <summary>
///     Clase que proporciona los errores relacionados con las operaciones de categorías.
/// </summary>
public static class CategoriaError
{
    /// <summary>
    ///     Genera un error cuando no se encuentra una categoría por su identificador.
    /// </summary>
    /// <param name="id">Identificador de la categoría que no se ha encontrado.</param>
    /// <returns>Error de tipo NotFoundError.</returns>
    public static NotFoundError CategoriaNotFound(long id)
    {
        return NotFoundError.FromId(id, "Categoria");
    }

    /// <summary>
    ///     Genera un error cuando no se encuentra una categoría por su nombre.
    /// </summary>
    /// <param name="name">Nombre de la categoría que no se ha encontrado.</param>
    /// <returns>Error de tipo NotFoundError.</returns>
    public static NotFoundError CategoriaNotFound(string name)
    {
        return NotFoundError.FromName(name, "Categoria");
    }

    /// <summary>
    ///     Genera un error de solicitud incorrecta relacionado con una categoría.
    /// </summary>
    /// <param name="id">Identificador de la categoría.</param>
    /// <returns>Error de tipo BadRequestError.</returns>
    public static BadRequestError CategoriaBadRequest(int id)
    {
        return new BadRequestError($"Categoria con el ID {id} no encontrada");
    }

    /// <summary>
    ///     Genera un error de conflicto al realizar una operación sobre una categoría.
    /// </summary>
    /// <param name="id">Identificador de la categoría con la que se ha producido el conflicto.</param>
    /// <returns>Error de tipo ConflictError.</returns>
    public static ConflictError CategoriaConflictError(int id)
    {
        return new ConflictError($"Se han tenido problemas a la hora de operar con la Categoria con el ID {id}");
    }
}
using FunkoApi.Back.Repositories.Crud.Common;

namespace FunkoApi.Back.Repositories.Categoria;

/// <summary>
///     Interfaz que define las operaciones de consulta del repositorio de categorías.
///     Hereda las operaciones de IGet y añade consultas por nombre.
/// </summary>
public interface ICategoriaRepository : ICrud<Models.Categoria, string>
{
    /// <summary>
    ///     Obtiene una categoría mediante su nombre.
    /// </summary>
    /// <param name="name">Nombre de la categoría que se desea buscar.</param>
    /// <returns>Categoría encontrada o null si no existe.</returns>
    Task<Models.Categoria?> GetByName(string name);

    /// <summary>
    /// Comprueba si existe una categoría con el nombre indicado.
    /// </summary>
    /// <param name="name">Nombre de la categoría que se desea comprobar.</param>
    /// <returns>True si existe una categoría con ese nombre o false en caso contrario.</returns>
    Task<bool> ExistsByName(string name);
    
    /// <summary>
    /// Comprueba si existe una categoría con el Id indicado.
    /// </summary>
    /// <param name="name">Nombre de la categoría que se desea comprobar.</param>
    /// <returns>True si existe una categoría con ese id o false en caso contrario.</returns>
    Task<bool> ExistsById(string name);
}
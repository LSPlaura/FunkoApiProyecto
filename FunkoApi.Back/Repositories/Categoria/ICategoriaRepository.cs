using FunkoApi.Back.Repositories.Crud.Common;

namespace FunkoApi.Back.Repositories.Get.Categoria.Common;

/// <summary>
///     Interfaz que define las operaciones de consulta del repositorio de categorías.
///     Hereda las operaciones de IGet y añade consultas por nombre.
/// </summary>
public interface ICategoriaRepository : ICrud<Models.Categoria, long>
{
    /// <summary>
    ///     Obtiene una categoría mediante su nombre.
    /// </summary>
    /// <param name="name">Nombre de la categoría que se desea buscar.</param>
    /// <returns>Categoría encontrada o null si no existe.</returns>
    Task<Models.Categoria?> GetByName(string name);

    /// <summary>
    ///     Comprueba si existe una categoría con el nombre indicado.
    /// </summary>
    /// <param name="name">Nombre de la categoría que se desea comprobar.</param>
    /// <returns>True si existe una categoría con ese nombre o false en caso contrario.</returns>
    Task<bool> ExistsByName(string name);
}
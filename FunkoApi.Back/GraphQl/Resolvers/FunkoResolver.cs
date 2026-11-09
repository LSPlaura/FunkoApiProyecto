using FunkoApi.Back.GraphQl.DataLoaders;
using FunkoApi.Back.Models;
using HotChocolate;
using HotChocolate.Types;

namespace FunkoApi.Back.GraphQl.Resolvers;

/// <summary>
/// Proporciona campos calculados y resolución de relaciones para la entidad <see cref="Funko"/> 
/// en el esquema de GraphQL de Hot Chocolate.
/// </summary>
[ExtendObjectType(typeof(Funko))]
public class FunkoResolver
{
    /// <summary>
    /// Indica si el Funko está disponible para la venta basándose en si no ha sido eliminado.
    /// </summary>
    /// <param name="funko">El objeto <see cref="Funko"/> padre al que pertenece este campo calculado.</param>
    /// <returns>
    /// <see langword="true"/> si el producto no está eliminado (<c>IsDeleted</c> es falso); 
    /// de lo contrario, <see langword="false"/>.
    /// </returns>
    public bool GetEstaDisponible([Parent] Funko funko)
    {
        return !funko.IsDeleted;
    }
    
    /// <summary>
    /// Obtiene la categoría asociada al Funko de forma optimizada mediante el uso de un DataLoader, 
    /// previniendo el problema de consultas N+1 en GraphQL.
    /// </summary>
    /// <param name="funko">El objeto <see cref="Funko"/> padre que contiene el identificador de la categoría.</param>
    /// <param name="categoriaDataLoader">El DataLoader encargado de agrupar y recuperar las categorías eficientemente.</param>
    /// <returns>
    /// La <see cref="Categoria"/> correspondiente si existe; de lo contrario, devuelve una nueva instancia por defecto.
    /// </returns>
    public async Task<Categoria> GetCategoria(
        [Parent] Funko funko,
        [Service] CategoriaByIdDataLoader categoriaDataLoader)
    { 
        return await categoriaDataLoader.LoadAsync(funko.CategoriaId) ?? new Categoria("", false, DateTime.UtcNow, DateTime.UtcNow, "");
    }
}
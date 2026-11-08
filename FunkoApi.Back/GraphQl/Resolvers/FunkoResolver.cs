using FunkoApi.Back.Models;
using HotChocolate;
using HotChocolate.Types;

namespace FunkoApi.Back.GraphQl.Resolvers;

[ExtendObjectType(typeof(Funko))]
public class FunkoResolver
{
    /// <summary>
    /// Indica si el funko Funko está disponible para la venta basándose en si no ha sido eliminado.
    /// </summary>
    /// <param name="funko">El objeto <see cref="Funko"/> padre al que pertenece este campo calculado.</param>
    /// <returns>
    /// <see langword="true"/> si el funko no está eliminado (<c>IsDeleted</c> es falso); 
    /// de lo contrario, <see langword="false"/>.
    /// </returns>
    public bool GetEstaDisponible([Parent] Funko funko)
    {
        return !funko.IsDeleted;
    }
}
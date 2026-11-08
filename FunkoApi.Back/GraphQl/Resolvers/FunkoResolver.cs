using FunkoApi.Back.Models;
using HotChocolate;
using HotChocolate.Types;

namespace FunkoApi.Back.GraphQl.Resolvers;

[ExtendObjectType(typeof(Funko))]
public class FunkoResolver
{
    /// <summary>
    /// Campo calculado: disponible si hay stock mayor a 0.
    /// </summary>
    public bool GetEstaDisponible([Parent] Funko producto)
    {
        return !producto.IsDeleted;
    }
}
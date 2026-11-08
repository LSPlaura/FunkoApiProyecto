using FunkoApi.Back.Models;
using HotChocolate.Types;

namespace FunkoApi.Back.GraphQl.Types;

public class FunkoType : ObjectType<Funko>
{
    protected override void Configure(IObjectTypeDescriptor<Funko> descriptor)
    {
        descriptor.Name("Funko");
        descriptor.Description("Funko de colección");

        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>();
        descriptor.Field(f => f.Nombre).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.Precio).Type<NonNullType<DecimalType>>();
        descriptor.Field(f => f.Stock).Type<NonNullType<IntType>>();
        descriptor.Field(f => f.Imagen).Type<StringType>();
        
        descriptor.Field(f => f.Categoria)
            .Type<NonNullType<CategoriaType>>()
            .Description("Categoría del funko");
        
        descriptor.Field(f => f.ProveedorId)
            .Type<NonNullType<ProveedorType>>()
            .Description("Proveedor del funko");
    }
}
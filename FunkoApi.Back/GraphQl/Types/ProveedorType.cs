using FunkoApi.Back.Models;
using HotChocolate.Types;

namespace FunkoApi.Back.GraphQl.Types;

public class ProveedorType : ObjectType<Proveedor>
{
    protected override void Configure(IObjectTypeDescriptor<Proveedor> descriptor)
    {
        descriptor.Name("Proveedor");
        descriptor.Description("Proveedor de funkos");

        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>();
        descriptor.Field(f => f.Nombre).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.IsDeleted).Type<BooleanType>();
    }
}
using FunkoApi.Back.Models;
using HotChocolate.Types;

namespace FunkoApi.Back.GraphQl.Types;

public class CategoriaType : ObjectType<Categoria>
{
    protected override void Configure(IObjectTypeDescriptor<Categoria> descriptor)
    {
        descriptor.Name("Categoria");
        descriptor.Description("Categoría de funkos");

        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>();
        descriptor.Field(f => f.Nombre).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.IsDeleted).Type<BooleanType>();
    }
}
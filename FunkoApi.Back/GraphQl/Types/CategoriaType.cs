using FunkoApi.Back.Models;
using HotChocolate.Types;

namespace FunkoApi.Back.GraphQl.Types;

/// <summary>
/// Define el tipo GraphQL para la entidad Categoria, mapeando las propiedades del modelo de dominio
/// a fields disponibles en el esquema GraphQL.
/// </summary>
public class CategoriaType : ObjectType<Categoria>
{
    /// <summary>
    /// Configura el tipo Categoria mapeando cada propiedad de la entidad Categoria a un field GraphQL.
    /// </summary>
    /// <param name="descriptor">El descriptor que permite configurar fields, tipos y metadatos del tipo GraphQL.</param>
    protected override void Configure(IObjectTypeDescriptor<Categoria> descriptor)
    {
        descriptor.Name("Categoria");
        descriptor.Description("Categoría de funkos");

        descriptor.Field(f => f.Id).Type<NonNullType<IdType>>();
        descriptor.Field(f => f.Nombre).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.IsDeleted).Type<BooleanType>();
    }
}
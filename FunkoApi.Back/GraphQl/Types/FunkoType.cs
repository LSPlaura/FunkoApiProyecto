using FunkoApi.Back.Models;
using HotChocolate.Types;

namespace FunkoApi.Back.GraphQl.Types;

/// <summary>
/// Define el tipo GraphQL para la entidad Funko, mapeando las propiedades del modelo de dominio
/// a fields disponibles en el esquema GraphQL.
/// </summary>
/// <remarks>
/// Este type expone las características principales de un funko de colección, incluyendo
/// sus datos básicos, relaciones con categoría y proveedor.
/// </remarks>
public class FunkoType : ObjectType<Funko>
{
    /// <summary>
    /// Configura el tipo Funko mapeando cada propiedad de la entidad Funko a un field GraphQL.
    /// </summary>
    /// <param name="descriptor">El descriptor que permite configurar fields, tipos y metadatos del tipo GraphQL.</param>
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
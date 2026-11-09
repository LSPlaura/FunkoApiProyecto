using FunkoApi.Back.GraphQl.DataLoaders;
using FunkoApi.Back.GraphQl.Queries;
using FunkoApi.Back.GraphQl.Resolvers;
using FunkoApi.Back.GraphQl.Types;
using HotChocolate.Execution.Configuration;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
/// Proporciona métodos de extensión para configurar y mapear el servidor de GraphQL (Hot Chocolate)
/// y sus componentes asociados dentro del contenedor de dependencias y la aplicación web.
/// </summary>
public static class GraphQlConfig
{
    /// <summary>
    /// Configura el servidor de GraphQL, registrando tipos, consultas, extensiones, DataLoaders,
    /// características de filtrado/ordenamiento/proyecciones y las políticas de excepciones.
    /// </summary>
    /// <param name="services">La colección de servicios de la aplicación donde se registrará GraphQL.</param>
    /// <param name="environment">El entorno de hospedaje actual (utilizado para determinar si se incluyen detalles de excepciones).</param>
    /// <returns>
    /// Un objeto <see cref="IRequestExecutorBuilder"/> que permite continuar encadenando configuraciones del servidor de GraphQL.
    /// </returns>
    public static IRequestExecutorBuilder AddGraphQlConfig(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        Log.Information("Configurando GraphQL con HotChocolate...");

        var builder = services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddQueryType<FunkoQuery>()
            .AddType<FunkoType>()
            .AddType<CategoriaType>()
            .AddType<ProveedorType>()
            .AddTypeExtension<FunkoResolver>()
            .AddDataLoader<CategoriaByIdDataLoader>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .ModifyRequestOptions(opt =>
            {
                opt.IncludeExceptionDetails = environment.IsDevelopment();
            });
        
        if (environment.IsProduction()) builder.DisableIntrospection(); 
        return builder;
    }

    /// <summary>
    /// Mapea los puntos de entrada (endpoints) de GraphQL y soporte para WebSockets en la aplicación web.
    /// </summary>
    /// <param name="app">La instancia de la aplicación web (<see cref="WebApplication"/>).</param>
    public static void MapGraphQlEndpoints(this WebApplication app)
    {
        app.MapGraphQL();
        app.MapGraphQLWebSocket();
    }
}
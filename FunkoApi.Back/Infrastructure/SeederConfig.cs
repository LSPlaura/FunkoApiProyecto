using FunkoApi.Back.Data.Seed.Efc;
using FunkoApi.Back.Data.Seed.Mongo;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
///     Configuración de los seeders.
/// </summary>
public static class SeederConfig
{
    /// <summary>
    ///     Registra todos los seeders en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddSeeders(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("Registrando seeders...");
        
        services.AddScoped<EfcSeeder>();
        services.AddScoped<MongoSeeder>();

        Log.Information("Seeders registrados correctamente.");
        return services;
    }
}
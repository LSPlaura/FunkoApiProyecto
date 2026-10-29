using FunkoApi.Back.Config;
using FunkoApi.Back.Data;
using FunkoApi.Back.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
///     Configuración de la base de datos.
/// </summary>
public static class DatabaseConfig
{
    /// <summary>
    ///     Configura el proveedor de base de datos según las variables de entorno.
    /// </summary>
    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        // ---------- MongoDB (driver nativo) ----------
        var mongoSettings = configuration.GetSection("MongoDbSettings").Get<MongoConfig>();

        Log.Information("Configurando MongoDB con driver nativo...");

        var mongoClient = new MongoClient(mongoSettings?.ConnectionString ?? "");
        var mongoDatabase = mongoClient.GetDatabase(mongoSettings?.DatabaseName ?? "");

        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton<IMongoDatabase>(mongoDatabase);
        services.AddSingleton<DbContextMongo>(sp => 
            new DbContextMongo(sp.GetRequiredService<IMongoDatabase>()));
        
        // ---------- PostgreSQL (EF Core) ----------
        var postgresSettings = configuration.GetSection("PostgresDbSettings").Get<PostgresConfig>();

        Log.Information("Configurando PostgreSQL...");
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(postgresSettings?.ConnectionString ?? ""));

        Log.Information("Bases de datos configuradas correctamente.");
        return services;
    }
}
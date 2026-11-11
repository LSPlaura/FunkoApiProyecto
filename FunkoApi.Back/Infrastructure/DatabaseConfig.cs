using FunkoApi.Back.Config;
using FunkoApi.Back.Data;
using FunkoApi.Back.Factory;
using FunkoApi.Back.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
/// Configuración de la base de datos.
/// </summary>
public static class DatabaseConfig {
    /// <summary>
    /// Configura el proveedor de base de datos según las variables de entorno.
    /// </summary>
    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration configuration) {
        // ==========================================
        // ---------- MongoDB (driver nativo) -------
        // ==========================================
        Log.Information("Iniciando configuración de MongoDB...");

        var mongoHost = configuration["MongoDb:Host"] ?? "";
        var mongoUser = configuration["MongoDb:User"] ?? "";
        var mongoPassword = configuration["MongoDb:Password"] ?? "";
        var mongoDatabaseName = configuration["MongoDb:Database"] ?? "";

        var mongoPortStr = configuration["MongoDb:Port"];
        int mongoPort = int.TryParse(mongoPortStr, out var parsedPort) ? parsedPort : 27017;

        if (string.IsNullOrEmpty(mongoPortStr)) Log.Warning("No se encontró 'MongoDb:Port' en la configuración. Se utilizará el puerto por defecto: {DefaultPort}", 27017);

        var connectionString = ConnectionStringGenerator.BuildMongoDb(mongoHost, mongoPort, mongoDatabaseName, mongoUser, mongoPassword);

        services.AddSingleton<IMongoClient>(_ => {
            Log.Information("Instanciando MongoClient para el host: {Host}:{Port}", mongoHost, mongoPort);
            return new MongoClient(connectionString);
        });

        services.AddSingleton<IMongoDatabase>(sp => {
            Log.Information("Obteniendo la base de datos de MongoDB: {DatabaseName}", mongoDatabaseName);
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoDatabaseName);
        });

        services.AddSingleton<DbContextMongo>(sp => {
            Log.Information("Registrando DbContextMongo en el contenedor DI...");
            return new DbContextMongo(sp.GetRequiredService<IMongoDatabase>());
        });

        Log.Information("MongoDB configurado correctamente.");

        // ==========================================
        // ---------- PostgreSQL (EF Core) ----------
        // ==========================================
        Log.Information("Iniciando configuración de PostgreSQL...");

        var pgHost = configuration["Postgres:Host"] ?? "";
        var pgPortStr = configuration["Postgres:Port"];
        int pgPort = int.TryParse(pgPortStr, out var parsedPgPort) ? parsedPgPort : 5432;
        var pgDatabase = configuration["Postgres:Database"] ?? "";
        var pgUser = configuration["Postgres:User"] ?? "";
        var pgPassword = configuration["Postgres:Password"] ?? "";

        if (string.IsNullOrEmpty(pgPortStr)) Log.Warning("No se encontró 'Postgres:Port' en la configuración. Se utilizará el puerto por defecto: {DefaultPort}", 5432);

        // Construir la cadena de conexión directamente (o usar un generador si lo tienes disponible)
        var pgConnectionString = $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword}";

        services.AddDbContext<AppDbContext>(options => {
            Log.Information("Configurando el DbContext de PostgreSQL para el host: {Host}:{Port}", pgHost, pgPort);
            options.UseNpgsql(pgConnectionString);
        });

        Log.Information("PostgreSQL configurado correctamente.");

        Log.Information("Todas las bases de datos se han configurado con éxito.");
        return services;
    }
}

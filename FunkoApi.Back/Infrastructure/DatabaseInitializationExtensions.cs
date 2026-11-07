using FunkoApi.Back.Data;
using FunkoApi.Back.Data.Seed.Efc;
using FunkoApi.Back.Data.Seed.Identity;
using FunkoApi.Back.Data.Seed.Mongo;
using MongoDB.Driver;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
/// Reúne la preparación de las bases de datos y sus datos iniciales al arrancar la API.
/// </summary>
public static class DatabaseInitializationExtensions {
    /// <summary>
    /// Elimina la base de datos de MongoDB y recrea la de PostgreSQL antes de sembrar
    /// proveedores, categorías, funkos y usuarios con sus roles.
    /// </summary>
    /// <param name="app">Aplicación de la que se resuelven los contextos y seeders.</param>
    /// <returns>Una tarea que finaliza cuando termina la inicialización.</returns>
    public static async Task InitializeDatabaseAsync(this WebApplication app) {
        using var scope = app.Services.CreateScope();
        
        // ---------- MongoDB (driver nativo) ----------
        Log.Information("Configurando MongoDB con driver nativo...");

        var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        
        await mongoDatabase.Client.DropDatabaseAsync(mongoDatabase.DatabaseNamespace.DatabaseName);
        Log.Information("[MongoDB] Base de datos eliminada y lista para resembrar.");
        var mongoSeeder = scope.ServiceProvider.GetRequiredService<MongoSeeder>();
        
        await mongoSeeder.SeedAsync();

        Log.Information("[MongoDB] Sembrado de datos realizado");

        // ---------- PostgreSQL (EF Core) ----------
        Log.Information("Configurando PostgreSQL con EF Core...");

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Log.Information("[EF Core] Eliminando y recreando la base de datos...");
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var efSeeder = scope.ServiceProvider.GetRequiredService<EfcSeeder>();
        await efSeeder.SeedAsync();

        Log.Information("[EF Core] Base de datos recreada con sembrado de datos");
        
        // ── Identity Seeder (después de recrear BD) ──────────────────────────
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        Log.Information("[Identity] Sembrado de datos realizado");
    }
}

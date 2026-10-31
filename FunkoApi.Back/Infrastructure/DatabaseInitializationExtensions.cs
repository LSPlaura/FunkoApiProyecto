using FunkoApi.Back.Config;
using FunkoApi.Back.Data;
using FunkoApi.Back.Data.Seed.Efc;
using FunkoApi.Back.Data.Seed.Mongo;
using MongoDB.Driver;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(
        this WebApplication app)
    {
        // ---------- MongoDB (driver nativo) ----------
        Log.Information("Configurando MongoDB con driver nativo...");

        var mongoSeeder = app.Services.GetRequiredService<MongoSeeder>();
        await mongoSeeder.SeedAsync();

        Log.Information("[MongoDB]Sembrado de datos realizado");

        // ---------- PostgreSQL (EF Core) ----------
        Log.Information("Configurando PostgreSQL con EF Core...");

        var context = app.Services.GetRequiredService<AppDbContext>();

        Log.Information("[EF Core] Eliminando y recreando la base de datos...");
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var efSeeder = app.Services.GetRequiredService<EfcSeeder>();
        await efSeeder.SeedAsync();

        Log.Information("[EF Core] Base de datos recreada con sembrado de datos");
    }
}
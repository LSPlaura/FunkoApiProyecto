using FunkoApi.Back.Data;
using FunkoApi.Back.Data.Seed.Efc;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            Log.Information("Produccion: inicializacion omitida.");
            return;
        }

        Log.Information("Inicializando base de datos...");

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var dbProvider = configuration["DB_PROVIDER"] ?? "postgres";

        // 1. Inicializacion para PostgreSQL (o EF Core relacional)
        if (dbProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        {
            var context = scope.ServiceProvider.GetService<AppDbContext>();
            if (context != null)
            {
                logger.LogWarning("[DESARROLLO] Eliminando y recreando base de datos PostgreSQL...");
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();

                var sqlSeeder = scope.ServiceProvider.GetService<EfcSeeder>();
                if (sqlSeeder != null) await sqlSeeder.SeedAsync();
                logger.LogInformation("Base de datos PostgreSQL recreada con datos semilla.");
            }
        }
    }
}
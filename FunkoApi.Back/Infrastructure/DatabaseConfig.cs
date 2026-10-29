using FunkoApi.Back.Data;
using Microsoft.EntityFrameworkCore;
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
        var dbProvider = configuration["DB_PROVIDER"] ?? "postgres";
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

        if (dbProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Configurando PostgreSQL...");
            services.AddDbContext<AppDbContext>(options => { options.UseNpgsql(connectionString); });
            Log.Information("PostgreSQL configurado correctamente.");
        }

        return services;
    }
}
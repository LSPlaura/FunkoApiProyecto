using FunkoApi.Back.Cache;
using FunkoApi.Back.Data;
using FunkoApi.Back.Services.Auth;
using FunkoApi.Back.Services.Auth.Common;
using FunkoApi.Back.Services.Categoria;
using FunkoApi.Back.Services.Funko;
using FunkoApi.Back.Services.Funko.Transactions;
using FunkoApi.Back.Services.Proveedor;
using FunkoApi.Back.Storage;
using Microsoft.Extensions.Options;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

/// <summary>
/// Configuración de servicios de negocio.
/// </summary>
public static class ServicesConfig {
    /// <summary>
    /// Registra todos los servicios de negocio en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services) {
        Log.Information("Registrando servicios...");

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFunkoService, FunkoServices>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IProveedorService, ProveedorService>();
        services.AddScoped<FunkoTransactionsService>(sp => new FunkoTransactionsService(
            sp.GetRequiredService<AppDbContext>(),
            "funko:",
            sp.GetRequiredService<IOptions<CacheOptions>>(),
            sp.GetRequiredService<ICacheService>(),
            sp.GetRequiredService<IStorageService>(),
            sp.GetRequiredService<ILogger<FunkoTransactionsService>>()
        ));

        Log.Information("Servicios registrados correctamente.");
        return services;
    }
}

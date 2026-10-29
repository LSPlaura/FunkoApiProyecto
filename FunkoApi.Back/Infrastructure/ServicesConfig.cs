using FunkoApi.Back.Services.Categoria;
using FunkoApi.Back.Services.Crud.Funko;
using FunkoApi.Back.Services.Funko;
using FunkoApi.Back.Services.Get.Categoria;
using Serilog;

namespace FunkoApi.Infrastructure;

/// <summary>
///     Configuración de servicios de negocio.
/// </summary>
public static class ServicesConfig
{
    /// <summary>
    ///     Registra todos los servicios de negocio en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        Log.Information("Registrando servicios...");

        services.AddScoped<IFunkoService, FunkoServices>();
        services.AddScoped<ICategoriaService, CategoriaService>();

        Log.Information("Servicios registrados correctamente.");
        return services;
    }
}
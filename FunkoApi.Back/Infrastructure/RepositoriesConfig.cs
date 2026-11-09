using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Repositories.Proveedor;
using Serilog;

namespace FunkoApi.Back.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Proporciona métodos de extensión para el registro centralizado de los repositorios 
/// de la aplicación en el contenedor de inyección de dependencias de .NET.
/// </summary>
public static class RepositoriesConfig {
    /// <summary>
    /// Añade y configura los servicios de los repositorios con un tiempo de vida por ámbito (<c>Scoped</c>) 
    /// en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">La colección de servicios (<see cref="IServiceCollection"/>) donde se registrarán las dependencias.</param>
    /// <param name="configuration">La configuración de la aplicación (<see cref="IConfiguration"/>) disponible para los repositorios si lo requieren.</param>
    /// <returns>La misma instancia de <see cref="IServiceCollection"/> para permitir llamadas encadenadas (fluent API).</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration) {
        Log.Information("Registrando repositorio...");
        services.AddScoped<IFunkoRepository, FunkoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProveedorRepository, ProveedorMongoRepository>();
        Log.Information("Repositorio registrados.");
        return services;
    }
}
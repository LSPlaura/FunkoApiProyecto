using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Repositories.Proveedor;

namespace FunkoApi.Back.Infrastructure;

public static class RepositoriesConfig
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFunkoRepository, FunkoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProveedorRepository, IProveedorRepository>();

        return services;
    }
}
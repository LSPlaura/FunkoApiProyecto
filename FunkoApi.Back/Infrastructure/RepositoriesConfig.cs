using FunkoApi.Back.Repositories.Crud.Funko;
using FunkoApi.Back.Repositories.Crud.Funko.Common;
using FunkoApi.Back.Repositories.Get.Categoria;
using FunkoApi.Back.Repositories.Get.Categoria.Common;

namespace FunkoApi.Back.Infrastructure;

public static class RepositoriesConfig
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration["DB_PROVIDER"] ?? "postgres";

        if (dbProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IFunkoRepository, FunkoRepository>();
            services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        }

        return services;
    }
}
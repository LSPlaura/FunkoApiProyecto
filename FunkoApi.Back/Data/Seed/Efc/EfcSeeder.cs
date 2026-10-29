using FunkoApi.Back.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Back.Data.Seed.Efc;

/// <summary>
///     Clase encargada de sembrar los datos iniciales de proveedores,
///     categorías y Funkos en la base de datos PostgreSQL.
/// </summary>
public class EfcSeeder(AppDbContext context, ILogger<EfcSeeder> logger)
{
    /// <summary>
    ///     Ejecuta el sembrado inicial de proveedores, categorías y Funkos,
    ///     y actualiza la secuencia de identificadores de los Funkos en PostgreSQL.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            logger.LogInformation("Sembrando datos iniciales en PostgreSQL...");

            await SeedProveedoresAsync();
            await SeedCategoriasAsync();
            await SeedFunkosAsync();

            await context.Database.ExecuteSqlRawAsync(
                "SELECT setval(pg_get_serial_sequence('\"Funkos\"', 'Id'), COALESCE(MAX(\"Id\"), 1)) FROM \"Funkos\";"
            );

            logger.LogInformation("Datos de PostgreSQL sembrados correctamente.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al sembrar datos en PostgreSQL");
        }
    }

    /// <summary>
    ///     Inserta los proveedores iniciales si la base de datos
    ///     todavía no contiene ningún proveedor.
    /// </summary>
    private async Task SeedProveedoresAsync()
    {
        if (await context.Proveedor.AnyAsync()) return;

        var proveedores = new List<Proveedor>
        {
            new(1, "Funko Corp", false, DateTime.UtcNow, DateTime.UtcNow),
            new(2, "Toy Master", false, DateTime.UtcNow, DateTime.UtcNow)
        };

        await context.Proveedor.AddRangeAsync(proveedores);
        await context.SaveChangesAsync();
        logger.LogInformation("Insertados {Count} proveedores de ejemplo.", proveedores.Count);
    }

    /// <summary>
    ///     Inserta las categorías iniciales si la base de datos
    ///     todavía no contiene ninguna categoría.
    /// </summary>
    private async Task SeedCategoriasAsync()
    {
        if (await context.Categorias.AnyAsync()) return;

        var categorias = new List<Categoria>
        {
            new("Marvel", false, DateTime.UtcNow, DateTime.UtcNow),
            new("Star Wars", false, DateTime.UtcNow, DateTime.UtcNow),
            new("Anime", false, DateTime.UtcNow, DateTime.UtcNow)
        };

        await context.Categorias.AddRangeAsync(categorias);
        await context.SaveChangesAsync();
        logger.LogInformation("Insertadas {Count} categorías de ejemplo.", categorias.Count);
    }

    /// <summary>
    ///     Inserta los Funkos iniciales si la base de datos todavía
    ///     no contiene ninguno, asociándolos con sus categorías
    ///     y proveedores correspondientes.
    /// </summary>
    private async Task SeedFunkosAsync()
    {
        if (await context.Funkos.AnyAsync()) return;

        var categoriaMarvel = await context.Categorias.FirstAsync(c => c.Nombre == "Marvel");
        var categoriaStarWars = await context.Categorias.FirstAsync(c => c.Nombre == "Star Wars");
        var categoriaAnime = await context.Categorias.FirstAsync(c => c.Nombre == "Anime");

        var proveedor1 = await context.Proveedor.FindAsync(1);
        var proveedor2 = await context.Proveedor.FindAsync(2);

        var funkos = new List<Funko>
        {
            new(1, "Iron Man", 14.99m, 10, categoriaMarvel.Id, categoriaMarvel, 1, proveedor1!, false, DateTime.UtcNow,
                DateTime.UtcNow),
            new(2, "Darth Vader", 16.50m, 5, categoriaStarWars.Id, categoriaStarWars, 2, proveedor2!, false, DateTime.UtcNow,
                DateTime.UtcNow),
            new(3, "Goku Super Saiyan", 15.00m, 8, categoriaAnime.Id, categoriaAnime, 1, proveedor1!, false, DateTime.UtcNow,
                DateTime.UtcNow)
        };

        foreach (var funko in funkos) await context.Funkos.AddAsync(funko);

        await context.SaveChangesAsync();
        logger.LogInformation("Insertados {Count} funkos de ejemplo.", funkos.Count);
    }
}

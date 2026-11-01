using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Back.Data.Seed.Efc;

/// <summary>
/// Clase encargada de sembrar categorías y Funkos en PostgreSQL,
/// utilizando los identificadores de los proveedores existentes en MongoDB.
/// </summary>
public class EfcSeeder(
    AppDbContext context,
    ILogger<EfcSeeder> logger,
    IProveedorRepository proveedorRepository
    ) {
    
    /// <summary>
    /// Ejecuta el sembrado inicial de categorías y Funkos,
    /// y actualiza la secuencia de identificadores de los Funkos en PostgreSQL.
    /// </summary>
    public async Task SeedAsync() {
        try {
            logger.LogInformation("Sembrando datos iniciales en PostgreSQL...");
            
            await SeedCategoriasAsync();
            await SeedFunkosAsync();

            await context.Database.ExecuteSqlRawAsync(
                "SELECT setval(pg_get_serial_sequence('\"Funkos\"', 'Id'), COALESCE(MAX(\"Id\"), 1)) FROM \"Funkos\";"
            );

            logger.LogInformation("Datos de PostgreSQL sembrados correctamente.");
        } catch (Exception ex) {
            logger.LogWarning(ex, "Error al sembrar datos en PostgreSQL");
        }
    }

    /// <summary>
    /// Inserta las categorías iniciales que todavía no existen,
    /// conservando los identificadores generados por el contexto.
    /// </summary>
    private async Task SeedCategoriasAsync() {
        var categorias = new List<Categoria> {
            new("Marvel", false, DateTime.UtcNow, DateTime.UtcNow),
            new("Star Wars", false, DateTime.UtcNow, DateTime.UtcNow),
            new("Anime", false, DateTime.UtcNow, DateTime.UtcNow)
        };

        var categoriasNuevas = new List<Categoria>();
        foreach (var categoria in categorias) {
            if (!await context.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre))
                categoriasNuevas.Add(categoria);
        }

        await context.Categorias.AddRangeAsync(categoriasNuevas);
        await context.SaveChangesAsync();
        logger.LogInformation("Insertadas {Count} categorías de ejemplo.", categoriasNuevas.Count);
    }

    /// <summary>
    /// Inserta los Funkos iniciales si la base de datos todavía
    /// no contiene ninguno, asociándolos con sus categorías
    /// y proveedores correspondientes.
    /// </summary>
    private async Task SeedFunkosAsync() {
        if (await context.Funkos.AnyAsync()) return;

        var categoriaMarvel = await context.Categorias.FirstAsync(c => c.Nombre == "Marvel" && !c.IsDeleted);
        var categoriaStarWars = await context.Categorias.FirstAsync(c => c.Nombre == "Star Wars" && !c.IsDeleted);
        var categoriaAnime = await context.Categorias.FirstAsync(c => c.Nombre == "Anime" && !c.IsDeleted);

        var proveedores = (await proveedorRepository.GetAll()).ToList();
        var proveedorFunkoCorp = proveedores.FirstOrDefault(p => p.Nombre == "Funko Corp" && !p.IsDeleted);
        var proveedorToyMaster = proveedores.FirstOrDefault(p => p.Nombre == "Toy Master" && !p.IsDeleted);
        var proveedorGeekToys = proveedores.FirstOrDefault(p => p.Nombre == "Geek Toys" && !p.IsDeleted);

        if (proveedorFunkoCorp == null || proveedorToyMaster == null || proveedorGeekToys == null)
            throw new InvalidOperationException(
                "Deben existir los proveedores activos Funko Corp, Toy Master y Geek Toys en MongoDB antes de sembrar los Funkos.");

        var funkos = new List<Funko> {
            new(1, "Iron Man", 14.99m, 10, string.Empty,
                categoriaMarvel.Id, categoriaMarvel, proveedorFunkoCorp.Id, null,
                false, DateTime.UtcNow, DateTime.UtcNow),
            new(2, "Darth Vader", 16.50m, 5, string.Empty,
                categoriaStarWars.Id, categoriaStarWars, proveedorToyMaster.Id, null,
                false, DateTime.UtcNow, DateTime.UtcNow),
            new(3, "Goku Super Saiyan", 15.00m, 8, string.Empty,
                categoriaAnime.Id, categoriaAnime, proveedorGeekToys.Id, null,
                false, DateTime.UtcNow, DateTime.UtcNow)
        };

        await context.Funkos.AddRangeAsync(funkos);

        await context.SaveChangesAsync();
        logger.LogInformation("Insertados {Count} funkos de ejemplo.", funkos.Count);
    }
}

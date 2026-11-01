using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Back.Data.Seed.Efc;

/// <summary>
/// Clase encargada de sembrar los datos iniciales de proveedores,
/// categorías y Funkos en la base de datos PostgreSQL.
/// </summary>
public class EfcSeeder(
    AppDbContext context,
    ILogger<EfcSeeder> logger,
    IProveedorRepository proveedorRepository
    ) {
    
    /// <summary>
    /// Ejecuta el sembrado inicial de proveedores, categorías y Funkos,
    /// y actualiza la secuencia de identificadores de los Funkos en PostgreSQL.
    /// </summary>
    public async Task SeedAsync() {
        try {
            logger.LogInformation("Sembrando datos iniciales en PostgreSQL...");
            
            await SeedProveedoresAsync();
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
    /// Conserva los proveedores de semilla de MongoDB con sus identificadores reales
    /// para satisfacer la relación que AppDbContext configura en PostgreSQL.
    /// Esta copia es una adaptación al modelo actual del proyecto.
    /// </summary>
    private async Task SeedProveedoresAsync() {
        if (await context.Funkos.AnyAsync()) return;

        var proveedoresMongo = (await proveedorRepository.GetAll()).ToList();
        var proveedorFunkoCorp = proveedoresMongo.FirstOrDefault(p => p.Nombre == "Funko Corp" && !p.IsDeleted);
        var proveedorToyMaster = proveedoresMongo.FirstOrDefault(p => p.Nombre == "Toy Master" && !p.IsDeleted);
        var proveedorGeekToys = proveedoresMongo.FirstOrDefault(p => p.Nombre == "Geek Toys" && !p.IsDeleted);

        if (proveedorFunkoCorp == null || proveedorToyMaster == null || proveedorGeekToys == null)
            throw new InvalidOperationException(
                "Deben existir los proveedores activos Funko Corp, Toy Master y Geek Toys en MongoDB antes de sembrar los Funkos.");

        var proveedores = new List<Proveedor>();
        foreach (var proveedor in new[] { proveedorFunkoCorp, proveedorToyMaster, proveedorGeekToys }) {
            if (!await context.Proveedor.AnyAsync(p => p.Id == proveedor.Id))
                proveedores.Add(proveedor);
        }

        await context.Proveedor.AddRangeAsync(proveedores);
        await context.SaveChangesAsync();
        logger.LogInformation("Insertados {Count} proveedores de MongoDB en PostgreSQL.", proveedores.Count);
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

        var proveedorFunkoCorp = await context.Proveedor.FirstAsync(p => p.Nombre == "Funko Corp" && !p.IsDeleted);
        var proveedorToyMaster = await context.Proveedor.FirstAsync(p => p.Nombre == "Toy Master" && !p.IsDeleted);
        var proveedorGeekToys = await context.Proveedor.FirstAsync(p => p.Nombre == "Geek Toys" && !p.IsDeleted);

        var funkos = new List<Funko> {
            new(1, "Iron Man", 14.99m, 10, string.Empty,
                categoriaMarvel.Id, categoriaMarvel, proveedorFunkoCorp.Id, proveedorFunkoCorp,
                false, DateTime.UtcNow, DateTime.UtcNow),
            new(2, "Darth Vader", 16.50m, 5, string.Empty,
                categoriaStarWars.Id, categoriaStarWars, proveedorToyMaster.Id, proveedorToyMaster,
                false, DateTime.UtcNow, DateTime.UtcNow),
            new(3, "Goku Super Saiyan", 15.00m, 8, string.Empty,
                categoriaAnime.Id, categoriaAnime, proveedorGeekToys.Id, proveedorGeekToys,
                false, DateTime.UtcNow, DateTime.UtcNow)
        };

        await context.Funkos.AddRangeAsync(funkos);

        await context.SaveChangesAsync();
        logger.LogInformation("Insertados {Count} funkos de ejemplo.", funkos.Count);
    }
}

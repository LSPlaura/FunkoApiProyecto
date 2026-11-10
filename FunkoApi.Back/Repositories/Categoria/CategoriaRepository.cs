using FunkoApi.Back.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FunkoApi.Back.Repositories.Categoria;

/// <summary>
/// Repositorio de categorías que implementa las operaciones de consulta
/// utilizando Entity Framework Core.
/// </summary>
internal class CategoriaRepository(
    AppDbContext context,
    ILogger<CategoriaRepository> logger) : ICategoriaRepository {
    
    /// <inheritdoc cref="ICategoriaRepository.GetAll" />
    public async Task<IEnumerable<Models.Categoria>> GetAll() {
        logger.LogDebug("Obteniendo todas las categorías");
        return await context.Categorias
            .OrderBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>Recupera una página de categorías y el total de registros.</summary>
    public async Task<(IEnumerable<Models.Categoria> Items, int TotalItems)> GetPaged(int page, int pageSize) {
        logger.LogDebug("Obteniendo página {Page} de categorías con tamaño {PageSize}", page, pageSize);
        var query = context.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <inheritdoc cref="ICategoriaRepository.GetById" />
    public async Task<Models.Categoria?> GetById(string key) {
        logger.LogDebug("Obteniendo categoría por ID: {Key}", key);
        return await context.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == key);
    }
    
    /// <inheritdoc cref="ICategoriaRepository.GetByName" />
    public async Task<Models.Categoria?> GetByName(string name) {
        logger.LogDebug("Obteniendo categoría por nombre: {Name}", name);
        return await context.Categorias.AsNoTracking().FirstOrDefaultAsync(f => f.Nombre == name);
    }
    
    /// <inheritdoc cref="ICategoriaRepository.Create" />
    public async Task<Models.Categoria> Create(Models.Categoria item) {
        logger.LogInformation("Creando categoría: {Nombre}", item.Nombre);
        item = item with{CreatedAt = DateTime.UtcNow};
        
        await context.Categorias.AddAsync(item);
        await context.SaveChangesAsync();
        return item;
    }
    
    /// <inheritdoc cref="ICategoriaRepository.Update" />
    public async Task<Models.Categoria?> Update(Models.Categoria item) {
        logger.LogInformation("Actualizando categoría ID: {Id}", item.Id);
        var existente = await GetById(item.Id);
        if (existente == null) return null;
        
        context.Categorias.Update(item);
        await context.SaveChangesAsync();
        
        return item;
    }

    /// <inheritdoc cref="ICategoriaRepository.SoftDelete" />
    public async Task<bool> SoftDelete(string id) {
        logger.LogInformation("Realizando borrado lógico de categoría ID: {Id}", id);
        return await context.Categorias
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsDeleted, true)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow)) > 0;
    }

    /// <inheritdoc cref="ICategoriaRepository.HardDelete" />
    public async Task<bool> HardDelete(Models.Categoria item) {
        logger.LogWarning("Realizando borrado físico de categoría ID: {Id}", item.Id);
        if(await context.Categorias.IgnoreQueryFilters().AnyAsync(c => c.Id == item.Id) == false) return false;
        
        context.Categorias.Remove(item);
        await context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc cref="ICategoriaRepository.ExistsByName" />
    public async Task<bool> ExistsByName(string name) {
        logger.LogDebug("Verificando existencia de categoría por nombre: {Name}", name);
        // El índice único reserva también los nombres de las eliminadas.
        return await context.Categorias.IgnoreQueryFilters().AsNoTracking().AnyAsync(c => c.Nombre == name);
    }
    
    /// <inheritdoc cref="ICategoriaRepository.ExistsById" />
    public async Task<bool> ExistsById(string key) {
        logger.LogDebug("Verificando existencia de categoría por ID: {Key}", key);
        return await context.Categorias.AsNoTracking().AnyAsync(c => c.Id == key);
    }
}

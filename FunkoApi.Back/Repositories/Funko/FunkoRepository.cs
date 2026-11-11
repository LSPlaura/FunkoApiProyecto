using FunkoApi.Back.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace FunkoApi.Back.Repositories.Funko;

/// <summary>
/// Repositorio de Funkos que implementa las operaciones CRUD
/// utilizando Entity Framework Core.
/// </summary>
public class FunkoRepository(AppDbContext context, ILogger<FunkoRepository> logger) : IFunkoRepository {
    
    /// <inheritdoc cref="IFunkoRepository.Create" />
    public async Task<Models.Funko> Create(Models.Funko item) {
        logger.LogInformation("Creando Funko: {Nombre}", item.Nombre);
        var categoria = await context.Categorias.FindAsync(item.CategoriaId);
        item = item with { Categoria = categoria };
        await context.Funkos.AddAsync(item);
        await context.SaveChangesAsync();
        return item;
    }

    /// <inheritdoc cref="IFunkoRepository.GetById" />
    public async Task<Models.Funko?> GetById(long key) {
        logger.LogDebug("Obteniendo Funko por ID: {Key}", key);
        return await context.Funkos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .FirstOrDefaultAsync(f => f.Id == key);
    }

    /// <inheritdoc cref="IFunkoRepository.HardDelete" />
    public async Task<bool> HardDelete(Models.Funko item) {
        logger.LogWarning("Realizando borrado físico de Funko ID: {Id}", item.Id);
        var existente = await context.Funkos.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == item.Id);
        if (existente == null) return false;

        context.Funkos.Remove(existente);
        await context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc cref="IFunkoRepository.Update" />
    public async Task<Models.Funko?> Update(Models.Funko item) {
        logger.LogInformation("Actualizando Funko ID: {Id}", item.Id);
        // Consultar la BD aplica el filtro incluso si la entidad sigue rastreada
        // después de un borrado con ExecuteUpdateAsync.
        var existente = await context.Funkos.FirstOrDefaultAsync(f => f.Id == item.Id);
        if (existente == null || existente.IsDeleted) return null;

        var actualizado = existente with {
            Nombre = item.Nombre,
            Precio = item.Precio,
            Stock = item.Stock,
            Imagen = item.Imagen,
            CategoriaId = item.CategoriaId,
            ProveedorId = item.ProveedorId,
            UpdatedAt = DateTime.UtcNow
        };

        context.Entry(existente).CurrentValues.SetValues(actualizado);
        await context.SaveChangesAsync();
        await context.Entry(existente).Reference(f => f.Categoria).LoadAsync();
        return existente;
    }

    /// <inheritdoc cref="IFunkoRepository.GetAll" />
    public async Task<IEnumerable<Models.Funko>> GetAll() {
        logger.LogDebug("Obteniendo todos los Funkos");
        return await context.Funkos.OrderBy(f => f.Nombre)
            .AsNoTracking()
            .Include(f => f.Categoria)
            .ToListAsync();
    }

    /// <summary>Recupera una página de funkos con su categoría y el total de registros.</summary>
    public async Task<(IEnumerable<Models.Funko> Items, int TotalItems)> GetPaged(int page, int pageSize) {
        logger.LogDebug("Obteniendo página {Page} de Funkos con tamaño {PageSize}", page, pageSize);
        var query = context.Funkos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .OrderBy(f => f.Nombre);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <inheritdoc cref="IFunkoRepository.GetByName" />
    public async Task<Models.Funko?> GetByName(string name) {
        logger.LogDebug("Obteniendo Funko por nombre: {Name}", name);
        return await context.Funkos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .FirstOrDefaultAsync(f => f.Nombre == name);
    }

    /// <inheritdoc cref="IFunkoRepository.GetByCategoriaId" />
    public async Task<IEnumerable<Models.Funko>> GetByCategoriaId(string key) {
        logger.LogDebug("Obteniendo Funkos por categoría: {Key}", key);
        return await context.Funkos
            .AsNoTracking()
            .Where(f => f.CategoriaId == key)
            .ToListAsync();
    }

    /// <inheritdoc cref="IFunkoRepository.ExitsName" />
    public async Task<bool> ExitsName(string name) {
        logger.LogDebug("Verificando existencia de Funko por nombre: {Name}", name);
        return await context.Funkos
            // El índice único reserva también los nombres de los eliminados.
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(f => f.Nombre == name);
    }

    /// <inheritdoc cref="IFunkoRepository.ExistAnyFunkoCategory" />
    public async Task<bool> ExistAnyFunkoCategory(string key) {
        logger.LogDebug("Verificando si existen Funkos con la categoría: {Key}", key);
        if(await context.Funkos.IgnoreQueryFilters().AsNoTracking().AnyAsync(f => f.CategoriaId == key)) return true;
        return false;
    }

    /// <inheritdoc cref="IFunkoRepository.ExistAnyFunkoProveedor" />
    public async Task<bool> ExistAnyFunkoProveedor(string key) {
        logger.LogDebug("Verificando si existen Funkos con el proveedor: {Key}", key);
        if(await context.Funkos.IgnoreQueryFilters().AsNoTracking().AnyAsync(f => f.ProveedorId == ObjectId.Parse(key))) return true;
        return false;
    }

    /// <inheritdoc cref="IFunkoRepository.SoftDelete" />
    public async Task<bool> SoftDelete(long id) {
        logger.LogInformation("Realizando borrado lógico de Funko ID: {Id}", id);
        return await context.Funkos
            .Where(f => f.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.IsDeleted, true)
                .SetProperty(f => f.UpdatedAt, DateTime.UtcNow)) > 0;
    }
}

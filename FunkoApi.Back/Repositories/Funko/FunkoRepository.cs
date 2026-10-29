using FunkoApi.Back.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace FunkoApi.Back.Repositories.Funko;

/// <summary>
/// Repositorio de Funkos que implementa las operaciones CRUD
/// utilizando Entity Framework Core.
/// </summary>
public class FunkoRepository(AppDbContext context) : IFunkoRepository {
    /// <inheritdoc cref="IFunkoRepository.Create" />
    public async Task<Models.Funko> Create(Models.Funko item)
    {
        context.Entry(item.Categoria).State = EntityState.Unchanged;
        await context.Funkos.AddAsync(item);
        await context.SaveChangesAsync();
        return item;
    }

    /// <inheritdoc cref="IFunkoRepository.GetById" />
    public async Task<Models.Funko?> GetById(int key)
    {
        return await context.Funkos.AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Proveedor)
            .FirstAsync(f => f.Id == key);
    }

    /// <inheritdoc cref="IFunkoRepository.SoftDelete" />
    public async Task<bool> SoftDelete(int id)
    {
        return await context.Funkos
            .Where(f => f.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(f => f.IsDeleted, true)
                .SetProperty(f => f.UpdatedAt, DateTime.UtcNow)) > 0;
    }

    /// <inheritdoc cref="IFunkoRepository.HardDelete" />
    public async Task<bool> HardDelete(Models.Funko item)
    {
        context.Remove(item);
        await context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc cref="IFunkoRepository.Update" />
    public async Task<Models.Funko> Update(Models.Funko item)
    {
        context.Funkos.Update(item);
        await context.SaveChangesAsync();
        return item;
    }

    /// <inheritdoc cref="IFunkoRepository.GetAll" />
    public async Task<IEnumerable<Models.Funko>> GetAll()
    {
        return await context.Funkos.OrderBy(f => f.Nombre)
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Proveedor)
            .ToListAsync();
    }

    /// <inheritdoc cref="IFunkoRepository.GetByName" />
    public async Task<Models.Funko?> GetByName(string name)
    {
        return await context.Funkos.AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Proveedor)
            .FirstAsync(f => f.Nombre == name);
    }

    /// <inheritdoc cref="IFunkoRepository.ExitsName" />
    public async Task<bool> ExitsName(string name)
    {
        return await context.Funkos.AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Proveedor)
            .AnyAsync(f => f.Nombre == name);
    }

    /// <inheritdoc cref="IFunkoRepository.ExistAnyFunkoProveedor" />
    public Task<bool> ExistAnyFunkoProveedor(string key) {
        if(context.Funkos.Any(f => f.CategoriaId == key)) return Task.FromResult(true);
        return Task.FromResult(false);
    }

    /// <inheritdoc cref="IFunkoRepository.ExistAnyFunkoCategory" />
    public Task<bool> ExistAnyFunkoCategory(string key) {
        if(context.Funkos.Any(f => f.ProveedorId == ObjectId.Parse(key))) return Task.FromResult(true);
        return Task.FromResult(false);
    }
}
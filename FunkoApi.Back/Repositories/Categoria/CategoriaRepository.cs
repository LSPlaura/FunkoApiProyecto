using FunkoApi.Back.Data;
using FunkoApi.Back.Repositories.Get.Categoria.Common;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Back.Repositories.Get.Categoria;

/// <summary>
///     Repositorio de categorías que implementa las operaciones de consulta
///     utilizando Entity Framework Core.
/// </summary>
public class CategoriaRepository(AppDbContext context) : ICategoriaRepository
{
    public Task<Models.Categoria> Create(Models.Categoria item)
    {
        throw new NotImplementedException();
    }

    public Task<Models.Categoria?> GetById(long key)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SoftDelete(long id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HardDelete(Models.Categoria item)
    {
        throw new NotImplementedException();
    }

    public Task<Models.Categoria> Update(Models.Categoria item)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="ICategoriaRepository.GetByName" />
    public async Task<Models.Categoria?> GetByName(string name)
    {
        return await context.Categorias.AsNoTracking().FirstAsync(f => f.Nombre == name);
    }

    /// <inheritdoc cref="ICategoriaRepository.ExistsByName" />
    public async Task<bool> ExistsByName(string name)
    {
        return await context.Categorias.AsNoTracking().AnyAsync(c => c.Nombre == name);
    }

    /// <inheritdoc cref="ICategoriaRepository.GetAll" />
    public async Task<IEnumerable<Models.Categoria>> GetAll()
    {
        return await context.Categorias.OrderBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc cref="ICategoriaRepository.GetById" />
    public async Task<Models.Categoria?> GetById(int key)
    {
        return await context.Categorias.AsNoTracking().FirstAsync(c => c.Id == key);
    }

    public Task<bool> SoftDelete(int id)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="ICategoriaRepository.ExistsById" />
    public async Task<bool> ExistsById(int key)
    {
        return await context.Categorias.AsNoTracking().AnyAsync(c => c.Id == key);
    }
}
using MongoDB.Driver;

namespace FunkoApi.Back.Repositories.Get.Proveedor;

/// <summary>
///     Repositorio de proveedores que implementa las operaciones de acceso a datos
///     utilizando el driver oficial de MongoDB para .NET.
/// </summary>
public class ProveedorMongoRepository : IProveedorRepository
{
    private readonly IMongoCollection<Models.Proveedor> _collection;

    /// <summary>
    ///     Inicializa una nueva instancia de la clase <see cref="ProveedorMongoRepository" />.
    /// </summary>
    /// <param name="database">Base de datos de MongoDB.</param>
    public ProveedorMongoRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Models.Proveedor>("proveedores");
    }

    Task<Models.Categoria?> IProveedorRepository.GetByName(string name)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsByName(string name)
    {
        var count = await _collection.CountDocumentsAsync(c => c.Nombre == name);
        return count > 0;
    }

    public Task<Models.Proveedor> Create(Models.Proveedor item)
    {
        throw new NotImplementedException();
    }

    public Task<Models.Proveedor?> GetById(long key)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SoftDelete(long id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HardDelete(Models.Proveedor item)
    {
        throw new NotImplementedException();
    }

    public Task<Models.Proveedor> Update(Models.Proveedor item)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Models.Proveedor>> GetAll()
    {
        return await _collection
            .Find(_ => true)
            .SortBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Models.Proveedor?> GetById(int key)
    {
        return await _collection
            .Find(c => c.Id == key)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsById(int key)
    {
        var count = await _collection.CountDocumentsAsync(c => c.Id == key);
        return count > 0;
    }

    public async Task<Models.Proveedor?> GetByName(string name)
    {
        return await _collection
            .Find(f => f.Nombre == name)
            .FirstOrDefaultAsync();
    }
}
using MongoDB.Bson;
using MongoDB.Driver;

namespace FunkoApi.Back.Repositories.Proveedor;

/// <summary>
///     Repositorio de proveedores que implementa las operaciones de acceso a datos
///     utilizando el driver oficial de MongoDB para .NET.
/// </summary>
public class ProveedorMongoRepository : IProveedorRepository
{
    private readonly IMongoCollection<Models.Proveedor> _collection;

    /// <summary>
    ///     Inicializa una nueva instancia de <see cref="ProveedorMongoRepository"/>.
    /// </summary>
    /// <param name="database">Base de datos de MongoDB.</param>
    public ProveedorMongoRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Models.Proveedor>("proveedores");
    }

    public async Task<Models.Proveedor> Create(Models.Proveedor item)
    {
        await _collection.InsertOneAsync(item);
        return item;
    }

    public async Task<bool> SoftDelete(string id)
    {
        var objectId = ObjectId.Parse(id);
        var resultado = await _collection.UpdateOneAsync(c => c.Id == objectId,
            Builders<Models.Proveedor>.Update.Set(c => c.IsDeleted, true));

        return resultado.ModifiedCount > 0;
    }

    public async Task<bool> HardDelete(Models.Proveedor item)
    {
        var resultado = await _collection.DeleteOneAsync(c => c.Id == item.Id);
        return resultado.DeletedCount > 0;
    }

    public async Task<Models.Proveedor?> Update(Models.Proveedor item)
    {
        var result = await _collection.ReplaceOneAsync(
            p => p.Id == item.Id && !p.IsDeleted,
            item);
        return result.IsAcknowledged ? item : null;
    }

    public async Task<IEnumerable<Models.Proveedor>> GetAll()
    {
        return await _collection
            .Find(_ => true)
            .SortBy(c => c.Nombre)
            .ToListAsync();
    }
    
    public async Task<Models.Proveedor?> GetById(string key)
    {
        var objectId = ObjectId.Parse(key);
        return await _collection
            .Find(c => c.Id == objectId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsById(string key)
    {
        var objectId = ObjectId.Parse(key);
        return await _collection
            .Find(c => c.Id == objectId)
            .AnyAsync();
    }
}
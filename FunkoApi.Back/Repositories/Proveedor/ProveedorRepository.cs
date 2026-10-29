using FunkoApi.Back.Repositories.Get.Proveedor;
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

    public async Task<Models.Proveedor?> GetById(ObjectId key)
    {
        return await _collection
            .Find(c => c.Id == key)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SoftDelete(ObjectId id)
    {
        var resultado = await IMongoCollectionExtensions.UpdateOneAsync(_collection, c => c.Id == id,
            Builders<Models.Proveedor>.Update.Set(c => c.IsDeleted, true));

        return resultado.ModifiedCount > 0;
    }

    public async Task<bool> HardDelete(Models.Proveedor item)
    {
        var resultado = await _collection.DeleteOneAsync(c => c.Id == item.Id);
        return resultado.DeletedCount > 0;
    }

    public async Task<Models.Proveedor> Update(Models.Proveedor item)
    {
        var resultado = await _collection.ReplaceOneAsync(
            c => c.Id == item.Id,
            item,
            new ReplaceOptions { IsUpsert = false });

        if (resultado.MatchedCount == 0)
        {
            throw new InvalidOperationException($"No se encontró el Models.Proveedor con Id {item.Id}");
        }

        return item;
    }

    public async Task<IEnumerable<Models.Proveedor>> GetAll()
    {
        return await _collection
            .Find(_ => true)
            .SortBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExistsById(ObjectId key)
    {
        return await _collection
            .Find(c => c.Id == key)
            .AnyAsync();
    }
}
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FunkoApi.Back.Repositories.Proveedor;

/// <summary>
///     Repositorio de proveedores que implementa las operaciones de acceso a datos
///     utilizando el driver oficial de MongoDB para .NET.
/// </summary>
public class ProveedorMongoRepository(
    IMongoDatabase database,
    ILogger<ProveedorMongoRepository> logger) : IProveedorRepository
{
    private readonly IMongoCollection<Models.Proveedor> _collection = database.GetCollection<Models.Proveedor>("proveedores");

    /// <inheritdoc/>
    public async Task<Models.Proveedor> Create(Models.Proveedor item) {
        logger.LogInformation("Creando proveedor: {Nombre}", item.Nombre);
        await _collection.InsertOneAsync(item);
        return item;
    }

    /// <inheritdoc/>
    public async Task<bool> SoftDelete(string id) {
        logger.LogInformation("Realizando borrado lógico de proveedor ID: {Id}", id);
        var objectId = ObjectId.Parse(id);
        var resultado = await _collection.UpdateOneAsync(c => c.Id == objectId,
            Builders<Models.Proveedor>.Update.Set(c => c.IsDeleted, true));

        return resultado.ModifiedCount > 0;
    }
    
    /// <inheritdoc/>
    public async Task<bool> HardDelete(Models.Proveedor item) {
        logger.LogWarning("Realizando borrado físico de proveedor ID: {Id}", item.Id);
        var resultado = await _collection.DeleteOneAsync(c => c.Id == item.Id);
        return resultado.DeletedCount > 0;
    }

    /// <inheritdoc/>
    public async Task<Models.Proveedor?> Update(Models.Proveedor item) {
        logger.LogInformation("Actualizando proveedor ID: {Id}", item.Id);
        var result = await _collection.ReplaceOneAsync(
            p => p.Id == item.Id && !p.IsDeleted,
            item);
        return result.IsAcknowledged ? item : null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Models.Proveedor>> GetAll() {
        logger.LogDebug("Obteniendo todos los proveedores");
        return await _collection
            .Find(_ => true)
            .SortBy(c => c.Nombre)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<Models.Proveedor?> GetById(string key) {
        logger.LogDebug("Obteniendo proveedor por ID: {Key}", key);
        var objectId = ObjectId.Parse(key);
        return await _collection
            .Find(c => c.Id == objectId)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsById(string key) {
        logger.LogDebug("Verificando existencia de proveedor por ID: {Key}", key);
        var objectId = ObjectId.Parse(key);
        return await _collection
            .Find(c => c.Id == objectId)
            .AnyAsync();
    }
}
using MongoDB.Bson;
using MongoDB.Driver;

namespace FunkoApi.Back.Repositories.Proveedor;

/// <summary>
/// Repositorio de proveedores que implementa las operaciones de acceso a datos
/// utilizando el driver oficial de MongoDB para .NET.
/// </summary>
public class ProveedorMongoRepository : IProveedorRepository {
    private readonly IMongoCollection<Models.Proveedor> _collection;
    private readonly ILogger<ProveedorMongoRepository> logger;

    /// <summary>Obtiene la colección de proveedores y crea sus índices.</summary>
    public ProveedorMongoRepository(IMongoDatabase database, ILogger<ProveedorMongoRepository> logger) {
        _collection = database.GetCollection<Models.Proveedor>("proveedores");
        this.logger = logger;
        CreateIndexes();
    }

    /// <summary>Crea el índice no único utilizado para ordenar los proveedores por nombre.</summary>
    private void CreateIndexes() {
        var indexKeys = Builders<Models.Proveedor>.IndexKeys
            .Ascending(p => p.Nombre);

        var indexModel = new CreateIndexModel<Models.Proveedor>(indexKeys, new CreateIndexOptions { Name = "idx_proveedor_nombre" });
        _collection.Indexes.CreateOne(indexModel);
    }

    /// <inheritdoc cref="IProveedorRepository.Create"/>
    public async Task<Models.Proveedor> Create(Models.Proveedor item) {
        logger.LogInformation("Creando proveedor: {Nombre}", item.Nombre);
        await _collection.InsertOneAsync(item);
        return item;
    }

    /// <inheritdoc cref="IProveedorRepository.SoftDelete"/>
    public async Task<bool> SoftDelete(string id) {
        logger.LogInformation("Realizando borrado lógico de proveedor ID: {Id}", id);
        var objectId = ObjectId.Parse(id);
        var resultado = await _collection.UpdateOneAsync(c => c.Id == objectId,
            Builders<Models.Proveedor>.Update.Set(c => c.IsDeleted, true));

        return resultado.ModifiedCount > 0;
    }
    
    /// <inheritdoc cref="IProveedorRepository.HardDelete"/>
    public async Task<bool> HardDelete(Models.Proveedor item) {
        logger.LogWarning("Realizando borrado físico de proveedor ID: {Id}", item.Id);
        var resultado = await _collection.DeleteOneAsync(c => c.Id == item.Id);
        return resultado.DeletedCount > 0;
    }

    /// <inheritdoc cref="IProveedorRepository.Update"/>
    public async Task<Models.Proveedor?> Update(Models.Proveedor item) {
        logger.LogInformation("Actualizando proveedor ID: {Id}", item.Id);
        var result = await _collection.ReplaceOneAsync(
            p => p.Id == item.Id && !p.IsDeleted,
            item);
        return result.IsAcknowledged ? item : null;
    }

    /// <inheritdoc cref="IProveedorRepository.GetAll"/>
    public async Task<IEnumerable<Models.Proveedor>> GetAll() {
        logger.LogDebug("Obteniendo todos los proveedores");
        return await _collection
            .Find(_ => true)
            .SortBy(c => c.Nombre)
            .ToListAsync();
    }
    
    /// <summary>Recupera una página de proveedores y el total de documentos.</summary>
    public async Task<(IEnumerable<Models.Proveedor> Items, int TotalItems)> GetPaged(int page, int pageSize) {
        logger.LogDebug("Obteniendo página {Page} de proveedores con tamaño {PageSize}", page, pageSize);
        var total = await _collection.CountDocumentsAsync(_ => true);
        var items = await _collection.Find(_ => true)
            .SortBy(p => p.Nombre)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, (int)total);
    }

    /// <inheritdoc cref="IProveedorRepository.GetById"/>
    public async Task<Models.Proveedor?> GetById(string key) {
        logger.LogDebug("Obteniendo proveedor por ID: {Key}", key);
        var objectId = ObjectId.Parse(key);
        return await _collection
            .Find(c => c.Id == objectId)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc cref="IProveedorRepository.ExistsById"/>
    public async Task<bool> ExistsById(string key) {
        logger.LogDebug("Verificando existencia de proveedor por ID: {Key}", key);
        var objectId = ObjectId.Parse(key);
        return await _collection
            .Find(c => c.Id == objectId)
            .AnyAsync();
    }
}
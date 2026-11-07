using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Proveedor;
using MongoDB.Bson;

namespace FunkoApi.Back.Data.Seed.Mongo;

/// <summary>
/// Clase encargada de sembrar los datos iniciales de proveedores
/// en MongoDB utilizando el repositorio con driver nativo.
/// </summary>
public class MongoSeeder {
    private readonly IProveedorRepository _repositorio;
    private readonly ILogger<MongoSeeder> _logger;

    public MongoSeeder(IProveedorRepository repositorio, ILogger<MongoSeeder> logger) {
        _repositorio = repositorio;
        _logger = logger;
    }

    /// <summary>
    /// Inserta proveedores iniciales si la colección todavía
    /// no contiene ninguno.
    /// </summary>
    public async Task SeedAsync() {
        try {
            _logger.LogInformation("Sembrando datos iniciales de proveedores en MongoDB...");

            var yaExisten = await _repositorio.GetAll().ContinueWith(t => t.Result.Any());
            if (yaExisten) {
                _logger.LogInformation("Ya existen proveedores en MongoDB. Sembrado omitido.");
                return;
            }

            var proveedores = new[] {
                new Proveedor(
                    ObjectId.GenerateNewId(),
                    "Funko Corp",
                    false,
                    DateTime.UtcNow,
                    DateTime.UtcNow),

                new Proveedor(
                    ObjectId.GenerateNewId(),
                    "Toy Master",
                    false,
                    DateTime.UtcNow,
                    DateTime.UtcNow),

                new Proveedor(
                    ObjectId.GenerateNewId(),
                    "Geek Toys",
                    false,
                    DateTime.UtcNow,
                    DateTime.UtcNow)
            };

            foreach (var proveedor in proveedores) {
                await _repositorio.Create(proveedor);
            }

            _logger.LogInformation("Insertados {Count} proveedores de ejemplo en MongoDB.", proveedores.Length);
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error al sembrar proveedores en MongoDB");
            throw;
        }
    }
}
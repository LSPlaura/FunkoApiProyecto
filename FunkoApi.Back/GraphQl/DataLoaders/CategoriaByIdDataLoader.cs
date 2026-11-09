using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FunkoApi.Back.Data;
using FunkoApi.Back.Models;
using GreenDonut;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FunkoApi.Back.GraphQl.DataLoaders;

/// <summary>
/// DataLoader para cargar categorías por ID de forma optimizada.
/// Resuelve el problema N+1 agrupando las consultas en una sola llamada a la base de datos.
/// </summary>
public class CategoriaByIdDataLoader : BatchDataLoader<string, Categoria>
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<CategoriaByIdDataLoader> _logger;

    /// <summary>
    /// Inicializa una nueva instancia del DataLoader de categorías.
    /// </summary>
    public CategoriaByIdDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<CategoriaByIdDataLoader> logger)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Carga un lote de categorías basado en una lista de identificadores únicos.
    /// </summary>
    protected override async Task<IReadOnlyDictionary<string, Categoria>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cargando lote de categorías mediante DataLoader. Cantidad de IDs solicitados: {Count}", keys.Count);

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Categorias
            .Where(c => keys.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
    }
}
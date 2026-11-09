using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Repositories.Funko;
using FunkoApi.Back.Services.Funko;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Data;
using Microsoft.Extensions.Logging;

namespace FunkoApi.Back.GraphQl.Queries;

/// <summary>
/// Define las consultas (Queries) de GraphQL disponibles para la entidad Funko.
/// Proporciona puntos de entrada para obtener colecciones paginadas y elementos individuales.
/// </summary>
public class FunkoQuery(ILogger<FunkoQuery> logger)
{
    /// <summary>
    /// Obtiene una lista de todos los Funkos con soporte integrado para paginación, filtrado y ordenamiento.
    /// </summary>
    /// <param name="funkoRepository">El repositorio encargado de la persistencia y recuperación de los Funkos.</param>
    /// <returns>Una secuencia consultable (<see cref="IQueryable{Funko}"/>) de objetos Funko.</returns>
    [UsePaging(MaxPageSize = 50, DefaultPageSize = 10)]
    [UseFiltering]
    [UseSorting]
    public async Task<IQueryable<Funko>> GetFunkos([Service] IFunkoRepository funkoRepository)
    {
        logger.LogInformation("Iniciando la consulta para obtener todos los Funkos (con soporte de Paging/Filtering/Sorting).");

        var result = await funkoRepository.GetAll();

        logger.LogInformation("Se han recuperado los datos de los Funkos correctamente desde el repositorio.");
        return result.AsQueryable();
    }

    /// <summary>
    /// Busca y devuelve un Funko específico según su identificador único.
    /// </summary>
    /// <param name="id">El identificador único del Funko que se desea consultar.</param>
    /// <param name="funkoRepository">El repositorio encargado de la persistencia y recuperación de los Funkos.</param>
    /// <returns>
    /// El objeto <see cref="Funko"/> correspondiente si existe; de lo contrario, <see langword="null"/>.
    /// </returns>
    public async Task<Funko?> GetFunko(long id, [Service] IFunkoRepository funkoRepository)
    {
        logger.LogInformation("Consultando el Funko con el ID: {FunkoId}", id);

        var result = await funkoRepository.GetById(id);

        if (result == null) 
        {
            logger.LogWarning("No se encontró ningún Funko registrado con el ID: {FunkoId}", id);
            return null;
        }

        logger.LogInformation("Funko con ID: {FunkoId} encontrado exitosamente.", id);
        return result;
    }
}
using CSharpFunctionalExtensions;
using FunkoApi.Back.Dto.Funko;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Services.Common;

namespace FunkoApi.Back.Services.Funko;

/// <summary>
///     Interfaz que define las operaciones del servicio de Funkos.
///     Hereda las operaciones CRUD y añade la búsqueda por nombre.
/// </summary>
public interface IFunkoService : ICrudService<CreateFunkoDto, UpdateFunkoDto, FunkoDto, long> {
    /// <summary>
    /// Obtiene un Funko mediante su nombre.
    /// </summary>
    /// <param name="name">Nombre del Funko que se desea buscar.</param>
    /// <returns>Resultado con el DTO del Funko encontrado o un error de dominio.</returns>
    Task<Result<FunkoDto, DomainError>> GetByName(string name);

    /// <summary>
    /// Obtiene una colección paginada de productos junto con los metadatos de paginación necesarios.
    /// </summary>
    /// <param name="page">Número de la página actual que se desea consultar (indexada en base 1).</param>
    /// <param name="pageSize">Cantidad máxima de elementos por página que se van a recuperar.</param>
    /// <returns>
    /// Una tupla que contiene:
    /// <list type="bullet">
    /// <item><description><c>Items</c>: La colección de elementos de tipo <see cref="FunkoDto"/> correspondientes a la página solicitada.</description></item>
    /// <item><description><c>TotalPages</c>: El número total de páginas disponibles en función del total de registros y el tamaño de página.</description></item>
    /// <item><description><c>TotalItems</c>: El recuento total de elementos existentes en la base de datos para esta consulta.</description></item>
    /// </list>
    /// </returns>
    Task<(IEnumerable<FunkoDto> Items, int TotalPages, int TotalItems)> GetPaged(int page, int pageSize);
}
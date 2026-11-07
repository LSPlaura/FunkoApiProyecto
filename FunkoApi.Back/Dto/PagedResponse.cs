namespace FunkoApi.Back.Dto;

/// <summary>
/// Representa una respuesta paginada genérica para los endpoints de la API.
/// </summary>
/// <typeparam name="T">Tipo de los elementos contenidos en la página.</typeparam>
public sealed record PagedResponse<T> {
    /// <summary>
    /// Obtiene o inicializa la colección de elementos pertenecientes a la página actual.
    /// </summary>
    public IEnumerable<T> Data { get; init; } = Enumerable.Empty<T>();

    /// <summary>
    /// Obtiene o inicializa los metadatos estadísticos y de control de la paginación.
    /// </summary>
    public PageMetadata Pagination { get; init; } = new(0, 0, 0, 0);
}

/// <summary>
/// Contiene los metadatos informativos sobre el estado de la paginación de una respuesta <see cref="PagedResponse{T}"/>.
/// </summary>
/// <param name="Page">Número de la página actual solicitada (indexada en base 1).</param>
/// <param name="PageSize">Cantidad máxima de elementos por página que fueron solicitados.</param>
/// <param name="TotalPages">Número total de páginas disponibles en función del total de elementos y el tamaño de página.</param>
/// <param name="TotalItems">Recuento total de elementos existentes en la base de datos para esta consulta.</param>
public sealed record PageMetadata(int Page, int PageSize, int TotalPages, int TotalItems);
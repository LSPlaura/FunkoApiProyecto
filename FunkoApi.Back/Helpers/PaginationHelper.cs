namespace FunkoApi.Back.Helpers;

/// <summary>
/// Clase auxiliar para la construcción de cabeceras de paginación basadas en estándares web (RFC 5988 Link headers).
/// </summary>
public static class PaginationHelper {
    /// <summary>
    /// Crea una cadena de texto formateada para la cabecera HTTP <c>Link</c>, permitiendo al cliente 
    /// navegar entre las diferentes páginas de un recurso (primera, anterior, siguiente y última).
    /// </summary>
    /// <param name="page">Número de la página actual que se está consultando (indexada en base 1).</param>
    /// <param name="totalPages">Número total de páginas disponibles en la colección.</param>
    /// <param name="pageSize">Cantidad máxima de elementos por página solicitados.</param>
    /// <param name="baseUrl">URL base del endpoint actual (sin parámetros de consulta).</param>
    /// <returns>
    /// Una cadena de texto con los enlaces formateados según el estándar RFC 5988 separados por comas, 
    /// o una cadena vacía si no hay páginas adyacentes.
    /// </returns>
    public static string CreateLinkHeader(int page, int totalPages, int pageSize, string baseUrl) {
        var links = new List<string>();

        if (page > 1) {
            links.Add($"<{baseUrl}?page=1&pageSize={pageSize}>; rel=\"first\"");
            links.Add($"<{baseUrl}?page={page - 1}&pageSize={pageSize}>; rel=\"prev\"");
        }

        if (page < totalPages) {
            links.Add($"<{baseUrl}?page={page + 1}&pageSize={pageSize}>; rel=\"next\"");
            links.Add($"<{baseUrl}?page={totalPages}&pageSize={pageSize}>; rel=\"last\"");
        }

        return string.Join(", ", links);
    }
}
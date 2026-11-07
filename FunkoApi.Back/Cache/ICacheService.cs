namespace FunkoApi.Back.Cache;

/// <summary>
/// Define contratos para operaciones de caché distribuida, soportando serialización JSON,
/// tiempos de expiración configurables y control de concurrencia para evitar el problema de cache stampede.
/// </summary>
public interface ICacheService {
    /// <summary>
    /// Obtiene y deserializa un objeto almacenado en caché a partir de su clave.
    /// </summary>
    /// <typeparam name="T">Tipo de dato al que se deserializará el valor almacenado.</typeparam>
    /// <param name="key">Clave única asociada al elemento en caché.</param>
    /// <param name="ct">Token de cancelación opcional para la operación asíncrona.</param>
    /// <returns>
    /// Una tarea que representa la operación asíncrona, devolviendo el objeto deserializado de tipo <typeparamref name="T"/> 
    /// si existe; de lo contrario, <c>default</c> (o <c>null</c>).
    /// </returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Serializa y almacena un valor en la caché asociado a una clave específica, aplicando opciones de expiración.
    /// </summary>
    /// <typeparam name="T">Tipo del valor que se va a serializar y almacenar.</typeparam>
    /// <param name="key">Clave única que identificará al elemento en la caché.</param>
    /// <param name="value">Objeto o valor que se desea guardar.</param>
    /// <param name="expiration">Tiempo de vida opcional para el elemento. Si es nulo, se utiliza la expiración por defecto de la configuración.</param>
    /// <param name="ct">Token de cancelación opcional para la operación asíncrona.</param>
    /// <returns>Una tarea que representa la operación asíncrona de guardado.</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    /// <summary>
    /// Elimina un elemento de la caché utilizando su clave.
    /// </summary>
    /// <param name="key">Clave única del elemento que se desea eliminar.</param>
    /// <param name="ct">Token de cancelación opcional para la operación asíncrona.</param>
    /// <returns>Una tarea que representa la operación asíncrona de borrado.</returns>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Comprueba si existe un elemento asociado a una clave específica en la caché.
    /// </summary>
    /// <param name="key">Clave única a verificar.</param>
    /// <param name="ct">Token de cancelación opcional para la operación asíncrona.</param>
    /// <returns>
    /// Una tarea que devuelve <c>true</c> si el elemento existe en la caché; de lo contrario, <c>false</c>.
    /// </returns>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Intenta recuperar un elemento de la caché. Si no existe, ejecuta una función de fábrica (<paramref name="factory"/>) 
    /// para obtener el valor, lo almacena en caché y lo devuelve. Utiliza mecanismos de bloqueo distribuido y en memoria 
    /// para prevenir consultas concurrentes simultáneas (<c>cache stampede</c>).
    /// </summary>
    /// <typeparam name="T">Tipo de dato esperado como resultado.</typeparam>
    /// <param name="key">Clave única del elemento en caché.</param>
    /// <param name="factory">Función asíncrona encargada de obtener los datos originales si la caché falla (fallthrough).</param>
    /// <param name="expiration">Tiempo de expiración opcional para el nuevo valor si se llega a sembrar.</param>
    /// <param name="ct">Token de cancelación opcional para la operación asíncrona.</param>
    /// <returns>
    /// Una tarea que representa la operación asíncrona, devolviendo el objeto recuperado de la caché o generado por la fábrica.
    /// </returns>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken ct = default);
}
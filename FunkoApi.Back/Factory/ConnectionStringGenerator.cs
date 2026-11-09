namespace FunkoApi.Back.Factory;

/// <summary>
/// Factory estática para construir strings de conexión a partir de componentes individuales.
/// Evita tener el string completo hardcodeado en appsettings, docker-compose o código.
/// </summary>
public static class ConnectionStringGenerator {
    /// <summary>
    /// Construye el string de conexión de PostgreSQL para Entity Framework Core (Npgsql).
    /// </summary>
    /// <param name="host">Servidor de PostgreSQL.</param>
    /// <param name="port">Puerto del servidor.</param>
    /// <param name="database">Nombre de la base de datos.</param>
    /// <param name="username">Usuario de autenticación.</param>
    /// <param name="password">Contraseña de autenticación.</param>
    /// <returns>String de conexión lista para <c>UseNpgsql</c>.</returns>
    public static string BuildPostgres(
        string host,
        int port,
        string database,
        string username,
        string password)
    {

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    /// <summary>
    /// Construye el string de conexión de MongoDB para el driver nativo de .NET.
    /// </summary>
    /// <param name="host">Servidor de MongoDB.</param>
    /// <param name="port">Puerto del servidor.</param>
    /// <param name="database">Nombre de la base de datos.</param>
    /// <param name="username">Usuario de autenticación.</param>
    /// <param name="password">Contraseña de autenticación.</param>
    /// <param name="authSource">Base de datos de autenticación (por defecto <c>admin</c>).</param>
    /// <returns>String de conexión lista para <c>MongoClient</c>.</returns>
    public static string BuildMongoDb(
        string host,
        int port,
        string database,
        string username,
        string password,
        string authSource = "admin")
    {
        return $"mongodb://{username}:{password}@{host}:{port}/{database}?authSource={authSource}&retryWrites=true&retryReads=true";
    }

    /// <summary>
    /// Construye el string de conexión de Redis para StackExchange.Redis.
    /// </summary>
    /// <param name="host">Servidor de Redis.</param>
    /// <param name="port">Puerto del servidor.</param>
    /// <param name="password">Contraseña opcional de Redis.</param>
    /// <returns>String de conexión lista para <c>ConnectionMultiplexer.Connect</c> o <c>AddStackExchangeRedisCache</c>.</returns>
    public static string BuildRedis(
        string host,
        int port,
        string? password = null)
    {
        return string.IsNullOrWhiteSpace(password)
            ? $"{host}:{port}"
            : $"{host}:{port},password={password}";
    }
}
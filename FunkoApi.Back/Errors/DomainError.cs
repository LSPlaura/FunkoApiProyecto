namespace FunkoApi.Back.Errors;

/// <summary>
/// Clase base para errores del dominio.
/// </summary>
public abstract record DomainError(string Message) {
    /// <summary>Representación formateada del error.</summary>
    public override string ToString() {
        return $"{GetType().Name}: {Message}";
    }
}

/// <summary>
///     Recurso no encontrado (HTTP 404).
/// </summary>
public sealed record NotFoundError(string Message) : DomainError(Message) {
    /// <summary>Crea error "no encontrado" con formato estándar.</summary>
    /// <param name="id">ID del recurso que no existe.</param>
    /// <param name="resourceType">Tipo de recurso para el mensaje.</param>
    /// <returns>NotFoundError formateado.</returns>
    public static NotFoundError FromId(long id, string resourceType = "Unknown") {
        return new NotFoundError($"{resourceType} con ID {id} no encontrado");
    }

    /// <summary>Crea error "no encontrado" con formato estándar.</summary>
    /// <param name="name">Nombre del recurso que no existe.</param>
    /// <param name="resourceType">Tipo de recurso para el mensaje.</param>
    /// <returns>NotFoundError formateado.</returns>
    public static NotFoundError FromName(string name, string resourceType = "Unknown") {
        return new NotFoundError($"{resourceType} con nombre {name} no encontrado");
    }
    
    /// <summary>Crea error "no encontrado" con formato estándar.</summary>
    /// <param name="id">Identificador del recurso de MongoDB expresado como cadena.</param>
    /// <param name="resourceType">Tipo de recurso para el mensaje.</param>
    /// <returns>NotFoundError formateado.</returns>
    public static NotFoundError FromObjectId(string id, string resourceType = "Unknown") {
        return new NotFoundError($"{resourceType} con ID {id} no encontrado");
    }
}

/// <summary>
///     Error de validación de datos (HTTP 400).
/// </summary>
public record BadRequestError(string Message) : DomainError(Message) {
    /// <summary>
    /// Agrupa un mensaje de validación y, opcionalmente, los errores de cada campo.
    /// </summary>
    /// <param name="Message">Descripción general del fallo de validación.</param>
    /// <param name="ValidationErrors">Mensajes asociados al nombre de cada campo, si están disponibles.</param>
    public sealed record ValidationError(string Message, Dictionary<string, string[]>? ValidationErrors = null) : DomainError(Message) {
        /// <summary>Crea error de validación con errores por campo.</summary>
        /// <param name="fieldErrors">Diccionario de errores por campo.</param>
        /// <returns>ValidationError lista para retornar.</returns>
        public static ValidationError WithFieldErrors(Dictionary<string, string[]> fieldErrors) {
            return new ValidationError("Errores de validación", fieldErrors);
        }
    }
}

/// <summary>
///     Violación de regla de negocio (HTTP 400/422).
/// </summary>
public sealed record BusinessRuleError(string Message) : DomainError(Message)
{
}

/// <summary>
///     No autenticado (HTTP 401).
/// </summary>
public sealed record UnauthorizedError(string Message) : DomainError(Message) {
    /// <summary>Crea error para credenciales incorrectas.</summary>
    /// <returns>UnauthorizedError con mensaje estándar.</returns>
    public static UnauthorizedError InvalidCredentials() {
        return new UnauthorizedError("Credenciales inválidas");
    }

    /// <summary>Crea error para token expirado.</summary>
    /// <returns>UnauthorizedError para token expirado.</returns>
    public static UnauthorizedError TokenExpired() {
        return new UnauthorizedError("Token expirado o inválido");
    }
}

/// <summary>
/// Sin permisos (HTTP 403).
/// </summary>
public sealed record ForbiddenError(string Message) : DomainError(Message) {
    /// <summary>Crea error cuando no es propietario del recurso.</summary>
    /// <param name="resourceType">Tipo de recurso.</param>
    /// <param name="resourceId">ID del recurso.</param>
    /// <returns>ForbiddenError personalizado.</returns>
    public static ForbiddenError NotOwner(string resourceType, string resourceId) {
        return new ForbiddenError($"No tienes permisos para acceder a este {resourceType} (ID: {resourceId})");
    }
}

/// <summary>
/// Conflicto de recursos (HTTP 409).
/// </summary>
public sealed record ConflictError(string Message) : DomainError(Message) {
    /// <summary>Crea error para recurso duplicado.</summary>
    /// <param name="resourceType">Tipo de recurso.</param>
    /// <param name="value">Valor duplicado.</param>
    /// <returns>ConflictError personalizado.</returns>
    public static ConflictError Duplicate(string resourceType, string value) {
        return new ConflictError($"Ya existe un {resourceType} con el valor '{value}'");
    }
    
    /// <summary>Crea error para recurso que no se puede borrar por dependencias.</summary>
    /// <param name="resourceType">Tipo de recurso.</param>
    /// <param name="value">Id del recurso</param>
    /// <returns>ConflictError personalizado.</returns>
    public static ConflictError Dependency(string resourceType, string value) {
        return new ConflictError($"No se puede borrar {resourceType} con el valor '{value}'");
    }
}

/// <summary>
/// Error interno del servidor (HTTP 500).
/// </summary>
public sealed record InternalError(string Message) : DomainError(Message) { }
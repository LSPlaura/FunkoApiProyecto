namespace FunkoApi.Back.Dto.Documentation;

/// <summary>
/// Esquema documental del cuerpo que devuelve DomainErrorExtensions.
/// No sustituye ni modifica las respuestas actuales de los servicios.
/// </summary>
/// <param name="Message">Mensaje del error de dominio.</param>
public record MessageErrorResponse(string Message);

/// <summary>Esquema documental del error de validación de dominio, distinto de ValidationProblemDetails.</summary>
/// <param name="Message">Descripción general de la validación.</param>
/// <param name="Errors">Errores por campo, si el servicio los proporciona.</param>
public record DomainValidationErrorResponse(string Message, Dictionary<string, string[]>? Errors);

/// <summary>Esquema documental del JSON escrito por GlobalExceptionHandler.</summary>
/// <param name="ErrorId">Identificador de ocho caracteres para buscar el error en los logs.</param>
/// <param name="Status">Código de estado HTTP.</param>
/// <param name="Message">Descripción pública del error.</param>
/// <param name="Timestamp">Fecha UTC del error en formato ISO 8601.</param>
/// <param name="Path">Ruta de la petición.</param>
/// <param name="Method">Verbo HTTP de la petición.</param>
public record ExceptionErrorResponse(string ErrorId, int Status, string Message, string Timestamp, string Path, string Method);

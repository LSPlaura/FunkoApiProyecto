using FunkoApi.Back.Errors;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Back.Extensions;

/// <summary>
///     Proporciona métodos de extensión para mapear objetos de error del dominio (<see cref="DomainError" />)
///     a respuestas HTTP estandarizadas (<see cref="ActionResult{T}" />) en los controladores de la API.
/// </summary>
/// <remarks>
///     Centraliza la lógica de conversión de códigos de estado para evitar la duplicación de bloques
///     de código <c>switch</c> en los controladores y asegurar un formato de respuesta JSON consistente.
/// </remarks>
public static class DomainErrorExtensions
{
    /// <summary>
    ///     Convierte una instancia de <see cref="DomainError" /> en su correspondiente respuesta HTTP envuelta en un
    ///     <see cref="ActionResult{T}" />.
    /// </summary>
    /// <typeparam name="T">
    ///     El tipo de dato que se devolvería en caso de éxito, necesario para mantener la firma tipada y la
    ///     correcta documentación en OpenAPI/Swagger.
    /// </typeparam>
    /// <param name="error">El objeto <see cref="DomainError" /> proveniente de la capa de servicios o negocio.</param>
    /// <returns>
    ///     Un <see cref="ActionResult{T}" /> que incluye el código de estado HTTP adecuado
    ///     junto con un cuerpo JSON estructurado con el mensaje del error.
    /// </returns>
    public static ActionResult<T> ToHttpResult<T>(this DomainError error)
    {
        return error switch
        {
            NotFoundError => new NotFoundObjectResult(new { message = error.Message }),
            BadRequestError.ValidationError ve => new BadRequestObjectResult(new
                { message = ve.Message, errors = ve.ValidationErrors }),
            BadRequestError => new BadRequestObjectResult(new { message = error.Message }),
            ConflictError => new ConflictObjectResult(new { message = error.Message }),
            UnauthorizedError => new UnauthorizedObjectResult(new { message = error.Message }),
            _ => new ObjectResult(new { message = "Error interno del servidor" }) { StatusCode = 500 }
        };
    }

    /// <summary>
    ///     Convierte una instancia de <see cref="DomainError" /> en su correspondiente respuesta HTTP envuelta en un
    ///     <see cref="ActionResult{T}" />.
    /// </summary>
    /// <param name="error">El objeto <see cref="DomainError" /> proveniente de la capa de servicios o negocio.</param>
    /// <returns>
    ///     Un <see cref="ActionResult" /> que incluye el código de estado HTTP adecuado
    ///     junto con un cuerpo JSON estructurado con el mensaje del error.
    /// </returns>
    /// Sobrecarga para métodos que devuelvan un NOContent como el Delete
    public static ActionResult ToHttpResult(this DomainError error)
    {
        return error switch
        {
            NotFoundError => new NotFoundObjectResult(new { message = error.Message }),
            BadRequestError.ValidationError ve => new BadRequestObjectResult(new
                { message = ve.Message, errors = ve.ValidationErrors }),
            BadRequestError => new BadRequestObjectResult(new { message = error.Message }),
            ConflictError => new ConflictObjectResult(new { message = error.Message }),
            UnauthorizedError => new UnauthorizedObjectResult(new { message = error.Message }),
            _ => new ObjectResult(new { message = "Error interno del servidor" }) { StatusCode = 500 }
        };
    }
}
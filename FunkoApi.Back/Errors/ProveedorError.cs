namespace FunkoApi.Back.Errors;

/// <summary>
/// Proporciona métodos estáticos de fábrica para generar errores estandarizados 
/// y reutilizables relacionados con las operaciones del dominio de proveedores.
/// </summary>
public static class ProveedorError {
    /// <summary>
    /// Genera un error estandarizado de tipo <see cref="NotFoundError"/> indicando que 
    /// no se encontró un proveedor a partir de su identificador único.
    /// </summary>
    /// <param name="id">Identificador único del proveedor que no pudo ser localizado.</param>
    /// <returns>Una instancia de <see cref="NotFoundError"/> configurada para el recurso proveedor.</returns>
    public static NotFoundError ProveedoraNotFound(string id) {
        return NotFoundError.FromObjectId(id, "Proveedor");
    }

    /// <summary>
    /// Genera un error estandarizado de tipo <see cref="BadRequestError"/> para notificar 
    /// una solicitud incorrecta o inválida relacionada con un proveedor específico.
    /// </summary>
    /// <param name="id">Identificador del proveedor involucrado en la petición errónea.</param>
    /// <returns>Una instancia de <see cref="BadRequestError"/> con un mensaje descriptivo del fallo.</returns>
    public static BadRequestError ProveedorBadRequest(string id) {
        return new BadRequestError($"Proveedor con el ID {id} no encontrada");
    }
}
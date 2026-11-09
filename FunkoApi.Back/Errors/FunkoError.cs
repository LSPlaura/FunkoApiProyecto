namespace FunkoApi.Back.Errors;

/// <summary>
/// Clase que proporciona los errores relacionados con las operaciones de Funkos.
/// </summary>
public static class FunkoError {
    /// <summary>
    /// Genera un error cuando no se encuentra un Funko por su identificador.
    /// </summary>
    /// <param name="id">Identificador del Funko que no se ha encontrado.</param>
    /// <returns>Error de tipo NotFoundError.</returns>
    public static NotFoundError FunkoNotFound(long id) {
        return NotFoundError.FromId(id, "Funko");
    }

    /// <summary>
    /// Genera un error cuando no se encuentra un Funko por su nombre.
    /// </summary>
    /// <param name="name">Nombre del Funko que no se ha encontrado.</param>
    /// <returns>Error de tipo NotFoundError.</returns>
    public static NotFoundError FunkoNotFoundByName(string name) {
        return new NotFoundError($"No se encontró el Funko con el nombre '{name}'");
    }

    /// <summary>
    /// Genera un error de conflicto cuando ya existe un Funko con el mismo nombre.
    /// </summary>
    /// <param name="name">Nombre del Funko que ya está registrado.</param>
    /// <returns>Error de tipo ConflictError.</returns>
    public static ConflictError FunkoAlreadyExists(string name) {
        return new ConflictError($"Ya existe un Funko registrado con el nombre '{name}'");
    }

    /// <summary>
    /// Genera un error de regla de negocio cuando un Funko no tiene stock suficiente.
    /// </summary>
    /// <param name="funkoName">Nombre del Funko que no tiene stock suficiente.</param>
    /// <returns>Error de tipo BusinessRuleError.</returns>
    public static BusinessRuleError InsufficientStockWithName(string funkoName) {
        return new BusinessRuleError($"Stock insuficiente para el Funko '{funkoName}'");
    }

    /// <summary>
    /// Genera un error de regla de negocio cuando el precio de un Funko no es válido.
    /// </summary>
    /// <param name="price">Precio del Funko que se considera inválido.</param>
    /// <returns>Error de tipo BusinessRuleError.</returns>
    public static BusinessRuleError InvalidPrice(decimal price) {
        return new BusinessRuleError($"El precio {price} no es válido");
    }

    /// <summary>
    /// Genera un error de solicitud incorrecta cuando no se encuentra
    /// el proveedor asociado a un Funko.
    /// </summary>
    /// <param name="name">Nombre del Funko cuyo proveedor no se ha encontrado.</param>
    /// <returns>Error de tipo BadRequestError.</returns>
    public static BadRequestError FunkoBadRequestProveedor(string name) {
        return new BadRequestError($"Proveedor no encontrado para el Funko '{name}'");
    }
}

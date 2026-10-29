namespace FunkoApi.Back.Errors;

public static class ProveedorError
{
    public static NotFoundError ProveedoraNotFound(string id)
    {
        return NotFoundError.FromObjectId(id, "Proveedor");
    }

    public static NotFoundError CategoriaNotFound(string name)
    {
        return NotFoundError.FromName(name, "Proveedor");
    }

    public static BadRequestError ProveedorBadRequest(int id)
    {
        return new BadRequestError($"Proveedor con el ID {id} no encontrada");
    }
}
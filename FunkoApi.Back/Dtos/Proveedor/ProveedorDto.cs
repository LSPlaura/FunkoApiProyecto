using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace FunkoApi.Back.Dtos.Proveedor;

/// <summary>
///     DTO que representa los datos de un proveedor,
///     incluyendo su identificador, nombre, estado de eliminación
///     y fechas de creación y actualización.
/// </summary>
public record ProveedorDto(
    [Required(ErrorMessage = "El ID es obligatorio")]
    ObjectId Id,
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    string Nombre,
    bool IsDeleted,
    [DataType(DataType.DateTime)] DateTime CreatedAt,
    [DataType(DataType.DateTime)] DateTime UpdatedAt
);
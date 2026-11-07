using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace FunkoApi.Back.Dtos.Proveedor;

/// <summary>
/// DTO que representa los datos de un proveedor,
/// incluyendo su identificador, nombre, estado de eliminación
/// y fechas de creación y actualización.
/// </summary>
/// <param name="Id">Identificador de MongoDB expresado como cadena.</param>
/// <param name="Nombre">Nombre registrado del proveedor.</param>
/// <param name="IsDeleted">Marca de eliminación lógica del proveedor.</param>
/// <param name="CreatedAt">Fecha de creación del registro.</param>
/// <param name="UpdatedAt">Fecha de su última actualización.</param>
public record ProveedorDto(
    [Required(ErrorMessage = "El ID es obligatorio")]
    string Id,
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    string Nombre,
    bool IsDeleted,
    [DataType(DataType.DateTime)] DateTime CreatedAt,
    [DataType(DataType.DateTime)] DateTime UpdatedAt
);
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FunkoApi.Back.Models;

/// <summary>
///     Modelo que representa un proveedor de Funkos en la base de datos.
/// </summary>
public record Proveedor(
    [property: BsonId]
    [property: BsonRepresentation(BsonType.Int64)]
    ObjectId Id,

    [property: BsonElement("nombre")]
    string Nombre,

    [property: BsonElement("is_deleted")]
    bool IsDeleted,

    [property: BsonElement("created_at")]
    [property: BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    DateTime CreatedAt,

    [property: BsonElement("updated_at")]
    [property: BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    DateTime UpdatedAt
);
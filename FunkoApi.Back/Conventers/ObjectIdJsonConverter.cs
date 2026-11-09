using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace FunkoApi.Back.Conventers;

/// <summary>
/// Representa los identificadores ObjectId de MongoDB como cadenas al intercambiar JSON.
/// </summary>
public class ObjectIdJsonConverter : JsonConverter<ObjectId> {
    /// <summary>
    /// Recupera un ObjectId desde una cadena JSON; si es nula o vacía, devuelve ObjectId.Empty.
    /// </summary>
    /// <param name="reader">Lector situado en el valor que se va a convertir.</param>
    /// <param name="typeToConvert">Tipo solicitado por el serializador.</param>
    /// <param name="options">Opciones de la deserialización.</param>
    /// <returns>Identificador convertido a partir de la cadena recibida.</returns>
    public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var value = reader.GetString();
        return string.IsNullOrEmpty(value) ? ObjectId.Empty : ObjectId.Parse(value);
    }

    /// <summary>
    /// Escribe el identificador como una cadena JSON, conservando su representación textual.
    /// </summary>
    /// <param name="writer">Escritor que recibe el valor serializado.</param>
    /// <param name="value">Identificador que se quiere representar.</param>
    /// <param name="options">Opciones de la serialización.</param>
    public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options) {
        writer.WriteStringValue(value.ToString());
    }
}

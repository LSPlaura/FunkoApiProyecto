using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace FunkoApi.Back.Generators;

/// <summary>
/// Genera IDs de 11 caracteres estilo YouTube.
/// </summary>
public class YouTubeIdValueGenerator : ValueGenerator<string> {
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    
    /// <summary>
    /// Construye un identificador de once caracteres a partir del alfabeto definido en esta clase.
    /// </summary>
    /// <param name="entry">Entrada de Entity Framework que solicita el identificador.</param>
    /// <returns>Cadena generada para utilizarla como identificador de la entidad.</returns>
    public override string Next(EntityEntry entry) => new string(Enumerable.Range(0, 11).Select(_ => Chars[Random.Shared.Next(64)]).ToArray());

    /// <summary>
    /// Indica que Entity Framework debe conservar el valor generado, no tratarlo como provisional.
    /// </summary>
    public override bool GeneratesTemporaryValues => false;
}

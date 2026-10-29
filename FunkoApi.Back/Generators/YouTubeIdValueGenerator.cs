using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace FunkoApi.Back.Generators;

/// <summary>
/// Genera IDs de 11 caracteres estilo YouTube.
/// </summary>
public class YouTubeIdValueGenerator : ValueGenerator<string> {
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    
    public override string Next(EntityEntry entry) =>
        new string(Enumerable.Range(0, 11).Select(_ => Chars[Random.Shared.Next(64)]).ToArray());

    public override bool GeneratesTemporaryValues => false;
}
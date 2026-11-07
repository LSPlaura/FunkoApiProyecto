namespace FunkoApi.Back.Generators;

/// <summary>
/// Atributo personalizado diseñado para marcar propiedades que representan 
/// o almacenan un identificador (ID) de un video o recurso de YouTube.
/// </summary>
/// <remarks>
/// Este atributo está restringido exclusivamente para su uso en propiedades (<see cref="AttributeTargets.Property"/>).
/// Puede ser utilizado posteriormente por validadores personalizados, mapeadores o serializadores.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class YouTubeIdAttribute : Attribute { }
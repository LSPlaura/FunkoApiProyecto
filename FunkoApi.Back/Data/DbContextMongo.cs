using FunkoApi.Back.Models;
using MongoDB.Driver;

namespace FunkoApi.Back.Data;

/// <summary>
/// Contexto de base de datos para MongoDB, encargado de gestionar y exponer 
/// las colecciones disponibles para la persistencia de datos orientada a documentos.
/// </summary>
/// <param name="database">Instancia de la base de datos de MongoDB inyectada por el contenedor de dependencias.</param>
public class DbContextMongo(IMongoDatabase database)
{
    /// <summary>
    /// Obtiene la colección de documentos <see cref="Proveedor"/> mapeada a la colección de MongoDB llamada "proveedores".
    /// </summary>
    public IMongoCollection<Proveedor> Proveedores =>
        database.GetCollection<Proveedor>("proveedores");
}
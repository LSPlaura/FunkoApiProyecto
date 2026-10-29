using FunkoApi.Back.Models;
using MongoDB.Driver;

namespace FunkoApi.Back.Data;

public class DbContextMongo(IMongoDatabase database)
{
    public IMongoCollection<Proveedor> Proveedores =>
        database.GetCollection<Proveedor>("proveedores");
}
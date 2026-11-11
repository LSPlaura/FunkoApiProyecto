using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class DatabaseConfigTests
{
    [TestCase(null, 27017, 5432)]
    [TestCase("incorrecto", 27017, 5432)]
    [TestCase("12345", 12345, 12345)]
    public void AddDatabases_DistintosPuertos_RegistraBasesYContextos(string? puerto, int mongoPort, int postgresPort)
    {
        // Arrange
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MongoDb:Host"] = "localhost", ["MongoDb:Database"] = "pruebas",
            ["MongoDb:User"] = "test", ["MongoDb:Password"] = "test", ["MongoDb:Port"] = puerto,
            ["Postgres:Host"] = "localhost", ["Postgres:Database"] = "pruebas",
            ["Postgres:User"] = "test", ["Postgres:Password"] = "test", ["Postgres:Port"] = puerto
        }).Build();
        var servicios = new ServiceCollection();

        // Act
        var resultado = servicios.AddDatabases(configuracion);
        using var provider = servicios.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var cliente = provider.GetRequiredService<IMongoClient>();
        var database = provider.GetRequiredService<IMongoDatabase>();
        var contextoMongo = provider.GetRequiredService<DbContextMongo>();
        var contextoPostgres = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Assert
        resultado.Should().BeSameAs(servicios);
        cliente.Settings.Server.Port.Should().Be(mongoPort);
        database.DatabaseNamespace.DatabaseName.Should().Be("pruebas");
        contextoMongo.Should().NotBeNull();
        contextoPostgres.Database.GetConnectionString().Should().Contain($"Port={postgresPort}");
        contextoPostgres.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }
}

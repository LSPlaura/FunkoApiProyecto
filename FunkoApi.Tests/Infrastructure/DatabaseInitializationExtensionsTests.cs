using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.Config;
using FunkoApi.Back.Data.Seed.Efc;
using FunkoApi.Back.Data.Seed.Mongo;
using FunkoApi.Back.Infrastructure;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Proveedor;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class DatabaseInitializationExtensionsTests
{
    private PostgreSqlContainer _container = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder().WithImage("postgres:17-alpine")
            .WithDatabase("initialization_test").WithUsername("test").WithPassword("test").Build();
        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _container.DisposeAsync();

    [Test]
    public async Task InitializeDatabaseAsync_BaseExistente_RecreaYSiembraDatos()
    {
        // Arrange
        var fecha = DateTime.UtcNow;
        var proveedores = new[]
        {
            new Proveedor(ObjectId.GenerateNewId(), "Funko Corp", false, fecha, fecha),
            new Proveedor(ObjectId.GenerateNewId(), "Toy Master", false, fecha, fecha),
            new Proveedor(ObjectId.GenerateNewId(), "Geek Toys", false, fecha, fecha)
        };
        var repositorioMock = new Mock<IProveedorRepository>();
        repositorioMock.Setup(r => r.GetAll()).ReturnsAsync(proveedores);
        await using var contexto = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString()).Options);
        await contexto.Database.EnsureCreatedAsync();
        await contexto.Categorias.AddAsync(new Categoria("Antigua", false, fecha, fecha, "antigua0001"));
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        var mongoClientMock = new Mock<IMongoClient>();
        var mongoDatabaseMock = new Mock<IMongoDatabase>();
        mongoDatabaseMock.Setup(d => d.Client).Returns(mongoClientMock.Object);
        mongoDatabaseMock.Setup(d => d.DatabaseNamespace).Returns(new DatabaseNamespace("initialization_test"));
        builder.Services.AddSingleton(mongoDatabaseMock.Object);
        builder.Services.AddSingleton(contexto);
        builder.Services.AddIdentity<AppUser, IdentityRole<long>>().AddEntityFrameworkStores<AppDbContext>();
        builder.Services.AddSingleton<IOptions<StorageSettings>>(Options.Create(new StorageSettings
        {
            UploadPath = "uploads", DefaultImage = "default.png",
            Folders = new Dictionary<string, string> { ["Funkos"] = "funkos" }
        }));
        builder.Services.AddSingleton(repositorioMock.Object);
        builder.Services.AddSingleton<MongoSeeder>();
        builder.Services.AddSingleton<EfcSeeder>();
        await using var app = builder.Build();

        // Act
        await app.InitializeDatabaseAsync();

        // Assert
        (await contexto.Categorias.CountAsync()).Should().Be(3);
        (await contexto.Categorias.AnyAsync(c => c.Nombre == "Antigua")).Should().BeFalse();
        (await contexto.Funkos.CountAsync()).Should().Be(3);
        repositorioMock.Verify(r => r.GetAll(), Times.Exactly(2));
        repositorioMock.Verify(r => r.Create(It.IsAny<Proveedor>()), Times.Never);
        mongoClientMock.Verify(c => c.DropDatabaseAsync("initialization_test", default), Times.Once);
        (await contexto.Users.CountAsync()).Should().Be(2);
    }
}

[TestFixture]
public class DatabaseInitializationWithoutSeedingTests
{
    [TestCase("Production")]
    [TestCase("Staging")]
    public async Task InitializeDatabaseAsync_FueraDeDevelopment_ConservaDatosYNoEjecutaSeedersDestructivos(string entorno)
    {
        // Arrange: esta rama solo usa Identity; no ejecuta SQL ni transacciones.
        await using var contexto = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await contexto.Database.EnsureCreatedAsync();
        var fecha = DateTime.UtcNow;
        contexto.Categorias.Add(new Categoria("Conservar", false, fecha, fecha, "conservar01"));
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var repositorioMock = new Mock<IProveedorRepository>();
        var mongoClientMock = new Mock<IMongoClient>();
        var mongoDatabaseMock = new Mock<IMongoDatabase>();
        mongoDatabaseMock.Setup(d => d.Client).Returns(mongoClientMock.Object);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = entorno });
        builder.Services.AddSingleton(contexto);
        builder.Services.AddSingleton(mongoDatabaseMock.Object);
        builder.Services.AddSingleton(repositorioMock.Object);
        builder.Services.AddIdentity<AppUser, IdentityRole<long>>().AddEntityFrameworkStores<AppDbContext>();
        await using var app = builder.Build();

        // Act
        await app.InitializeDatabaseAsync();

        // Assert
        (await contexto.Categorias.Select(c => c.Nombre).ToListAsync()).Should().Equal("Conservar");
        (await contexto.Funkos.CountAsync()).Should().Be(0);
        (await contexto.Users.CountAsync()).Should().Be(2);
        repositorioMock.Verify(r => r.GetAll(), Times.Never);
        repositorioMock.Verify(r => r.Create(It.IsAny<Proveedor>()), Times.Never);
        mongoClientMock.Verify(c => c.DropDatabaseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

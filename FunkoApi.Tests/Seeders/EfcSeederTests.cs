using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.Data.Seed.Efc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Data.Seed.Efc;

[TestFixture]
public class EfcSeederTests
{
    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockLogger = new Mock<ILogger<EfcSeeder>>();

        _seeder = new EfcSeeder(_context, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private AppDbContext _context = null!;
    private Mock<ILogger<EfcSeeder>> _mockLogger = null!;
    private EfcSeeder _seeder = null!;

    [TestFixture]
    public class CasosPositivos : EfcSeederTests
    {
        [Test]
        public async Task SeedAsync_BaseDeDatosVacia_InsertaDatosCorrectamente()
        {
            // Act
            await _seeder.SeedAsync();

            // Assert
            _context.Categorias.Should().NotBeEmpty();
            _context.Funkos.Should().NotBeEmpty();
        }
    }

    [TestFixture]
    public class CasosNegativos : EfcSeederTests
    {
        [Test]
        public async Task SeedAsync_ConExcepcion_NoPropagaError()
        {
            // Arrange
            var seederConError = new EfcSeeder(null!, _mockLogger.Object);

            // Act & Assert
            await seederConError.Invoking(s => s.SeedAsync())
                .Should().NotThrowAsync();
        }
    }
}
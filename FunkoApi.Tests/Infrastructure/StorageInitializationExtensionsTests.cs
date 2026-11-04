using FluentAssertions;
using FunkoApi.Back.Config;
using FunkoApi.Back.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class StorageInitializationExtensionsTests
{
    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public async Task InitializeStorage_CreaCarpetasLimpiaYCopiaSemillas(bool existe, bool sinWebRoot, bool conSemillas)
    {
        // Arrange
        var raiz = Path.Combine(Path.GetTempPath(), "opencode", "funko-init-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(raiz);
        var uploads = Path.Combine(raiz, "uploads");
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{StorageSettings.SectionName}:Folders:Funkos"] = "funkos"
        }).Build();
        try
        {
            if (existe)
            {
                Directory.CreateDirectory(Path.Combine(uploads, "subcarpeta"));
                await File.WriteAllTextAsync(Path.Combine(uploads, "archivo.txt"), "anterior");
                await File.WriteAllTextAsync(Path.Combine(uploads, "subcarpeta", "otro.txt"), "anterior");
            }
            if (conSemillas)
            {
                var origen = Path.Combine(raiz, "SeedImages", "funkos");
                Directory.CreateDirectory(Path.Combine(origen, "anidada"));
                await File.WriteAllTextAsync(Path.Combine(origen, "default.png"), "semilla");
                await File.WriteAllTextAsync(Path.Combine(origen, "anidada", "otra.png"), "otra semilla");
            }
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = raiz });
            await using var app = builder.Build();
            app.Environment.WebRootPath = sinWebRoot ? null! : raiz;

            // Act
            app.InitializeStorage(configuracion);

            // Assert
            Directory.Exists(Path.Combine(uploads, "funkos")).Should().BeTrue();
            File.Exists(Path.Combine(uploads, "archivo.txt")).Should().BeFalse();
            Directory.Exists(Path.Combine(uploads, "subcarpeta")).Should().BeFalse();
            if (conSemillas)
            {
                File.ReadAllText(Path.Combine(uploads, "funkos", "default.png")).Should().Be("semilla");
                File.ReadAllText(Path.Combine(uploads, "funkos", "anidada", "otra.png")).Should().Be("otra semilla");
            }
            else Directory.GetFiles(uploads, "*", SearchOption.AllDirectories).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(raiz, true);
        }
    }

    [Test]
    public async Task InitializeStorage_RutaOcupadaPorArchivo_NoPropagaExcepcion()
    {
        // Arrange
        var raiz = Path.Combine(Path.GetTempPath(), "opencode", "funko-init-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(raiz);
        var uploads = Path.Combine(raiz, "uploads");
        await File.WriteAllTextAsync(uploads, "no soy una carpeta");
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{StorageSettings.SectionName}:Folders:Funkos"] = "funkos"
        }).Build();
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = raiz });
            await using var app = builder.Build();
            app.Environment.WebRootPath = raiz;
            Action accion = () => app.InitializeStorage(configuracion);

            // Act
            accion.Should().NotThrow();

            // Assert
            File.ReadAllText(uploads).Should().Be("no soy una carpeta");
        }
        finally
        {
            Directory.Delete(raiz, true);
        }
    }
}

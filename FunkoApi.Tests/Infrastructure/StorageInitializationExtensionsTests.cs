using FluentAssertions;
using FunkoApi.Back.Infrastructure;
using Microsoft.AspNetCore.Builder;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class StorageInitializationExtensionsTests
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task InitializeStorage_CarpetaNuevaOExistente_DejaCarpetaVacia(bool existe, bool sinWebRoot)
    {
        // Arrange
        var raiz = Path.Combine(Path.GetTempPath(), "opencode", "funko-init-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(raiz);
        var uploads = Path.Combine(raiz, "uploads");
        try
        {
            if (existe)
            {
                Directory.CreateDirectory(Path.Combine(uploads, "subcarpeta"));
                await File.WriteAllTextAsync(Path.Combine(uploads, "archivo.txt"), "prueba");
                await File.WriteAllTextAsync(Path.Combine(uploads, "subcarpeta", "otro.txt"), "prueba");
            }
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = raiz });
            await using var app = builder.Build();
            app.Environment.WebRootPath = sinWebRoot ? null! : raiz;

            // Act
            app.InitializeStorage();

            // Assert
            Directory.Exists(uploads).Should().BeTrue();
            Directory.GetFileSystemEntries(uploads).Should().BeEmpty();
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
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = raiz });
            await using var app = builder.Build();
            app.Environment.WebRootPath = raiz;
            Action accion = () => app.InitializeStorage();

            // Act
            var resultado = accion.Should().NotThrow();

            // Assert
            File.Exists(uploads).Should().BeTrue();
            resultado.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(raiz, true);
        }
    }
}

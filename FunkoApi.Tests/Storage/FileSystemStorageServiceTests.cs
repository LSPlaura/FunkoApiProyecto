using FluentAssertions;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Storage;

[TestFixture]
public class FileSystemStorageServiceTests
{
    private string _directorio = null!;
    private Mock<IWebHostEnvironment> _envMock = null!;
    private FileSystemStorageService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _directorio = Path.Combine(Path.GetTempPath(), "opencode", "funko-storage-tests", Guid.NewGuid().ToString());
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.WebRootPath).Returns(_directorio);
        _envMock.Setup(e => e.ContentRootPath).Returns(_directorio);
        _service = new FileSystemStorageService(new ConfigurationBuilder().Build(),
            new Mock<ILogger<FileSystemStorageService>>().Object, _envMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directorio)) Directory.Delete(_directorio, true);
    }

    [TestCase("imagen.JPG", "IMAGE/JPEG")]
    [TestCase("iron man-1.png", "image/png")]
    [TestCase("imagen.gif", "image/gif")]
    public async Task SaveFileAsync_ArchivoValido_GuardaContenidoConNombreGenerado(string nombre, string tipo)
    {
        // Arrange
        byte[] contenido = [1, 2, 3, 4];
        using var stream = new MemoryStream(contenido);
        var archivo = new FormFile(stream, 0, contenido.Length, "archivo", nombre) { Headers = new HeaderDictionary(), ContentType = tipo };

        // Act
        var resultado = await _service.SaveFileAsync(archivo, "funkos");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().StartWith("/uploads/funkos/").And.EndWith(Path.GetExtension(nombre).ToLowerInvariant());
        var ruta = Path.Combine(_directorio, "uploads", "funkos", Path.GetFileName(resultado.Value));
        File.ReadAllBytes(ruta).Should().Equal(contenido);
        Path.GetFileName(resultado.Value).Should().NotContain(" ").And.NotContain("-");
    }

    [TestCase(0, "imagen.jpg", "image/jpeg", "vacio")]
    [TestCase(5242881, "imagen.jpg", "image/jpeg", "grande")]
    [TestCase(1, "imagen.pdf", "image/jpeg", "extension")]
    [TestCase(1, "imagen.jpg", "application/pdf", "tipo")]
    [TestCase(1, "imagen.jpg", null, "tipo")]
    [TestCase(1, "imagen..jpg", "image/jpeg", "nombre")]
    public async Task SaveFileAsync_ArchivoInvalido_RetornaErrorSinEscribir(long longitud, string nombre, string? tipo, string motivo)
    {
        // Arrange
        var archivoMock = new Mock<IFormFile>();
        archivoMock.Setup(f => f.Length).Returns(longitud);
        archivoMock.Setup(f => f.FileName).Returns(nombre);
        archivoMock.Setup(f => f.ContentType).Returns(tipo!);
        var esperado = motivo switch
        {
            "vacio" => StorageError.ArchivoVacio(),
            "grande" => StorageError.ArchivoMuyGrande(),
            "extension" => StorageError.ExtensionNoPermitida(),
            "tipo" => StorageError.TipoContenidoNoPermitido(),
            _ => StorageError.NombreArchivoInvalido()
        };

        // Act
        var resultado = await _service.SaveFileAsync(archivoMock.Object, "funkos");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(esperado);
        archivoMock.Verify(f => f.CopyTo(It.IsAny<Stream>()), Times.Never);
        Directory.GetFiles(_directorio, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Test]
    public async Task SaveFileAsync_ArchivoNulo_RetornaError()
    {
        // Arrange
        IFormFile archivo = null!;

        // Act
        var resultado = await _service.SaveFileAsync(archivo, "funkos");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(StorageError.ArchivoVacio());
    }

    [Test]
    public async Task SaveFileAsync_ErrorCopiando_RetornaErrorGuardando()
    {
        // Arrange
        var archivoMock = new Mock<IFormFile>();
        archivoMock.Setup(f => f.Length).Returns(1);
        archivoMock.Setup(f => f.FileName).Returns("imagen.jpg");
        archivoMock.Setup(f => f.ContentType).Returns("image/jpeg");
        archivoMock.Setup(f => f.CopyTo(It.IsAny<Stream>())).Throws(new IOException("Error de escritura"));

        // Act
        var resultado = await _service.SaveFileAsync(archivoMock.Object, "funkos");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(StorageError.ErrorGuardando());
        archivoMock.Verify(f => f.CopyTo(It.IsAny<Stream>()), Times.Once);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("no-existe.png")]
    public async Task DeleteFileAsync_SinArchivo_RetornaExito(string? nombre)
    {
        // Arrange
        var ruta = nombre!;

        // Act
        var resultado = await _service.DeleteFileAsync(ruta);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeTrue();
    }

    [Test]
    public async Task DeleteFileAsync_ArchivoExistente_EliminaArchivo()
    {
        // Arrange
        var ruta = Path.Combine(_directorio, "uploads", "imagen.png");
        await File.WriteAllBytesAsync(ruta, new byte[] { 1, 2, 3 });

        // Act
        var resultado = await _service.DeleteFileAsync("imagen.png");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        File.Exists(ruta).Should().BeFalse();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("no-existe.png")]
    public void FileExists_SinArchivo_RetornaFalse(string? nombre)
    {
        // Arrange
        var ruta = nombre!;

        // Act
        var resultado = _service.FileExists(ruta);

        // Assert
        resultado.Should().BeFalse();
    }

    [Test]
    public void FileExists_ArchivoExistente_RetornaTrue()
    {
        // Arrange
        var ruta = Path.Combine(_directorio, "uploads", "imagen.png");
        File.WriteAllBytes(ruta, new byte[] { 1 });

        // Act
        var resultado = _service.FileExists("imagen.png");

        // Assert
        resultado.Should().BeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GetFullPath_RelativaOAbsoluta_ResuelveRuta(bool absoluta)
    {
        // Arrange
        var esperada = Path.Combine(_directorio, "uploads", "imagen.png");
        var entrada = absoluta ? esperada : "imagen.png";

        // Act
        var resultado = _service.GetFullPath(entrada);

        // Assert
        resultado.Should().Be(esperada);
    }

    [Test]
    public void GetRelativePath_SinCarpeta_UtilizaFunkos()
    {
        // Arrange
        const string nombre = "imagen.png";

        // Act
        var resultado = _service.GetRelativePath(nombre);

        // Assert
        resultado.Should().Be("/uploads/funkos/imagen.png");
    }

    [Test]
    public void Constructor_ConfiguracionPersonalizadaYWebRootNulo_CreaCarpetaEnContentRoot()
    {
        // Arrange
        _envMock.Setup(e => e.WebRootPath).Returns((string)null!);
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:UploadPath"] = "imagenes",
            ["Storage:MaxFileSize"] = "10",
            ["Storage:AllowedExtensions:0"] = ".png",
            ["Storage:AllowedContentTypes:0"] = "image/png"
        }).Build();

        // Act
        var servicio = new FileSystemStorageService(configuracion, new Mock<ILogger<FileSystemStorageService>>().Object, _envMock.Object);

        // Assert
        Directory.Exists(Path.Combine(_directorio, "imagenes")).Should().BeTrue();
        servicio.GetRelativePath("imagen.png", "categorias").Should().Be("/imagenes/categorias/imagen.png");
    }
}

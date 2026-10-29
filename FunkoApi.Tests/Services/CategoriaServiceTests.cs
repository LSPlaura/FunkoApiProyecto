using FluentAssertions;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Categoria;
using FunkoApi.Back.Services.Categoria;
using FunkoApi.Back.Services.Get.Categoria;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Services;

/// <summary>
///     Tests unitarios de CategoriaService con Moq para la dependencia del repositorio.
/// </summary>
[TestFixture]
public class CategoriaServiceTests
{
    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<ICategoriaRepository>();
        _service = new CategoriaService(_mockRepository.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _mockRepository.Reset();
    }

    private Mock<ICategoriaRepository> _mockRepository = null!;
    private CategoriaService _service = null!;

    [TestFixture]
    public class CasosPositivos : CategoriaServiceTests
    {
        [Test]
        public async Task GetById_Existente_DeberiaRetornarCategoria()
        {
            // Arrange
            var categoria = TestData.Categoria();
            _mockRepository.Setup(r => r.GetById(1)).ReturnsAsync(categoria);

            // Act
            var resultado = await _service.GetById(1);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Nombre.Should().Be("Marvel");
            _mockRepository.Verify(r => r.GetById(1), Times.Once);
        }

        [Test]
        public async Task GetAll_ConCategorias_DeberiaRetornarTodasLasCategorias()
        {
            // Arrange
            var categorias = new[]
            {
                TestData.Categoria(),
                TestData.Categoria(2, "Anime")
            };
            _mockRepository.Setup(r => r.GetAll()).ReturnsAsync(categorias);

            // Act
            var resultado = await _service.GetAll();

            // Assert
            resultado.Should().HaveCount(2);
            resultado.Select(c => c.Nombre).Should().ContainInOrder("Marvel", "Anime");
            _mockRepository.Verify(r => r.GetAll(), Times.Once);
        }

        [Test]
        public async Task GetByName_Existente_DeberiaRetornarCategoria()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByName("Marvel")).ReturnsAsync(TestData.Categoria());

            // Act
            var resultado = await _service.GetByName("Marvel");

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Nombre.Should().Be("Marvel");
            _mockRepository.Verify(r => r.GetByName("Marvel"), Times.Once);
        }
    }

    [TestFixture]
    public class CasosNegativos : CategoriaServiceTests
    {
        [Test]
        public async Task GetById_Inexistente_DeberiaRetornarNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetById(99))
                .ReturnsAsync((Categoria?)null);

            // Act
            var resultado = await _service.GetById(99);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
            _mockRepository.Verify(r => r.GetById(99), Times.Once);
        }

        [Test]
        public async Task GetByName_Inexistente_DeberiaRetornarNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByName("Missing"))
                .ReturnsAsync((Categoria?)null);

            // Act
            var resultado = await _service.GetByName("Missing");

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
            resultado.Error.Message.Should().Contain("Missing");
            _mockRepository.Verify(r => r.GetByName("Missing"), Times.Once);
        }
    }
}
using FluentAssertions;
using FunkoApi.Back.Dtos.Funko;
using FunkoApi.Back.Dtos.Proveedor;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Models;
using FunkoApi.Back.Repositories.Crud.Funko.Common;
using FunkoApi.Back.Repositories.Get.Categoria.Common;
using FunkoApi.Back.Services.Crud.Funko;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Services;

/// <summary>
///     Tests unitarios de FunkoServices con Moq y patrón AAA.
/// </summary>
[TestFixture]
public class FunkoServicesTests
{
    [SetUp]
    public void SetUp()
    {
        _mockFunkoRepository = new Mock<IFunkoRepository>();
        _mockCategoriaRepository = new Mock<ICategoriaRepository>();
        _service = new FunkoServices(
            _mockFunkoRepository.Object,
            _mockCategoriaRepository.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _mockFunkoRepository.Reset();
        _mockCategoriaRepository.Reset();
    }

    private Mock<IFunkoRepository> _mockFunkoRepository = null!;
    private Mock<ICategoriaRepository> _mockCategoriaRepository = null!;
    private FunkoServices _service = null!;

    private static CreateFunkoDto CreateRequest(string nombre = "Iron Man")
    {
        return new CreateFunkoDto
        {
            Nombre = nombre,
            Precio = 14.99m,
            Stock = 10,
            CategoriaId = 1,
            Proveedor = new ProveedorDto(
                1,
                "Funko Corp",
                false,
                DateTime.UtcNow,
                DateTime.UtcNow
            )
        };
    }

    private static UpdateFunkoDto UpdateRequest(string nombre = "Iron Man")
    {
        return new UpdateFunkoDto
        {
            Nombre = nombre,
            Precio = 20m,
            Stock = 5,
            CategoriaId = 1,
            Proveedor = new ProveedorDto(
                1,
                "Proveedor",
                false,
                DateTime.UtcNow,
                DateTime.UtcNow
            )
        };
    }

    [TestFixture]
    public class CasosPositivos : FunkoServicesTests
    {
        [Test]
        public async Task Create_DatosValidos_CreaYDevuelveDto()
        {
            // Arrange
            var categoria = TestData.Categoria();
            var funko = TestData.Funko(categoria: categoria);
            _mockCategoriaRepository.Setup(x => x.GetById(1)).ReturnsAsync(categoria);
            _mockFunkoRepository.Setup(x => x.ExitsName("Iron Man")).ReturnsAsync(false);
            _mockFunkoRepository.Setup(x => x.Create(It.IsAny<Funko>()))
                .ReturnsAsync(funko);

            // Act
            var resultado = await _service.Create(CreateRequest());

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Nombre.Should().Be("Iron Man");
            resultado.Value.CategoriaId.Should().Be(1);
            _mockFunkoRepository.Verify(r => r.Create(It.IsAny<Funko>()), Times.Once);
        }

        [Test]
        public async Task GetById_Existente_DevuelveDto()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Funko());

            // Act
            var resultado = await _service.GetById(1);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Nombre.Should().Be("Iron Man");
        }

        [Test]
        public async Task GetByName_Existente_DevuelveDto()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetByName("Iron Man")).ReturnsAsync(TestData.Funko());

            // Act
            var resultado = await _service.GetByName("Iron Man");

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Nombre.Should().Be("Iron Man");
        }

        [Test]
        public async Task SoftDelete_Existente_DevuelveResultadoDelRepositorio()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Funko());
            _mockFunkoRepository.Setup(x => x.SoftDelete(1)).ReturnsAsync(true);

            // Act
            var resultado = await _service.SoftDelete(1);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().BeTrue();
            _mockFunkoRepository.Verify(r => r.SoftDelete(1), Times.Once);
        }

        [Test]
        public async Task SoftDelete_ExistentePeroNoModificado_DevuelveFalse()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Funko());
            _mockFunkoRepository.Setup(x => x.SoftDelete(1)).ReturnsAsync(false);

            // Act
            var resultado = await _service.SoftDelete(1);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().BeFalse();
        }

        [Test]
        public async Task HardDelete_Existente_DevuelveResultadoDelRepositorio()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Funko());
            _mockFunkoRepository.Setup(x => x.HardDelete(It.IsAny<Funko>()))
                .ReturnsAsync(true);

            // Act
            var resultado = await _service.HardDelete(1);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Should().BeTrue();
            _mockFunkoRepository.Verify(r => r.HardDelete(It.IsAny<Funko>()), Times.Once);
        }

        [Test]
        public async Task Update_MismoNombre_NoCompruebaConflictoYActualiza()
        {
            // Arrange
            var oldFunko = TestData.Funko();
            _mockFunkoRepository.Setup(x => x.GetById(1)).ReturnsAsync(oldFunko);

            _mockCategoriaRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Categoria());

            _mockFunkoRepository.Setup(x => x.ExitsName("Iron Man")).ReturnsAsync(true);
            _mockFunkoRepository.Setup(x => x.Update(It.IsAny<Funko>()))
                .ReturnsAsync((Funko item) => item);

            // Act
            var resultado = await _service.Update(UpdateRequest(), 1);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Precio.Should().Be(20m);
            resultado.Value.Stock.Should().Be(5);
            _mockFunkoRepository.Verify(r => r.Update(It.IsAny<Funko>()), Times.Once);
        }

        [Test]
        public async Task Update_NombreNuevoNoDuplicado_Actualiza()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Funko());

            _mockCategoriaRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Categoria());

            _mockFunkoRepository.Setup(x => x.ExitsName("Updated")).ReturnsAsync(false);
            _mockFunkoRepository.Setup(x => x.Update(It.IsAny<Funko>()))
                .ReturnsAsync((Funko item) => item);

            // Act
            var resultado = await _service.Update(UpdateRequest("Updated"), 1);

            // Assert
            resultado.IsSuccess.Should().BeTrue();
            resultado.Value.Nombre.Should().Be("Updated");
        }

        [Test]
        public async Task GetAll_ConDatos_DevuelveDtos()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetAll())
                .ReturnsAsync(new[] { TestData.Funko(1, "Alpha"), TestData.Funko(2, "Beta") });

            // Act
            var resultado = await _service.GetAll();

            // Assert
            resultado.Should().HaveCount(2);
            resultado.Select(f => f.Nombre).Should().ContainInOrder("Alpha", "Beta");
        }

        [Test]
        public async Task GetAll_SinDatos_DevuelveColeccionVacia()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetAll())
                .ReturnsAsync(Array.Empty<Funko>());

            // Act
            var resultado = await _service.GetAll();

            // Assert
            resultado.Should().BeEmpty();
        }
    }

    [TestFixture]
    public class CasosNegativos : FunkoServicesTests
    {
        [Test]
        public async Task Create_CategoriaInexistente_DevuelveBadRequest()
        {
            // Arrange
            _mockCategoriaRepository.Setup(x => x.GetById(1))
                .ReturnsAsync((Categoria?)null);

            // Act
            var resultado = await _service.Create(CreateRequest());

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<BadRequestError>();
            _mockFunkoRepository.Verify(r => r.ExitsName(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task Create_NombreDuplicado_DevuelveConflict()
        {
            // Arrange
            _mockCategoriaRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Categoria());
            _mockFunkoRepository.Setup(x => x.ExitsName("Iron Man")).ReturnsAsync(true);

            // Act
            var resultado = await _service.Create(CreateRequest());

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<ConflictError>();
            _mockFunkoRepository.Verify(r => r.Create(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task GetById_Inexistente_DevuelveNotFound()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetById(99))
                .ReturnsAsync((Funko?)null);

            // Act
            var resultado = await _service.GetById(99);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
        }

        [Test]
        public async Task GetByName_Inexistente_DevuelveNotFoundPorNombre()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetByName("Missing"))
                .ReturnsAsync((Funko?)null);

            // Act
            var resultado = await _service.GetByName("Missing");

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Message.Should().Contain("Missing");
        }

        [Test]
        public async Task SoftDelete_Inexistente_DevuelveNotFound()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetById(99))
                .ReturnsAsync((Funko?)null);

            // Act
            var resultado = await _service.SoftDelete(99);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
            _mockFunkoRepository.Verify(r => r.SoftDelete(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task HardDelete_Inexistente_DevuelveNotFound()
        {
            // Arrange
            _mockFunkoRepository.Setup(x => x.GetById(99))
                .ReturnsAsync((Funko?)null);

            // Act
            var resultado = await _service.HardDelete(99);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
            _mockFunkoRepository.Verify(r => r.HardDelete(It.IsAny<Funko>()), Times.Never);
        }

        [Test]
        public async Task Update_Inexistente_DevuelveNotFound()
        {
            _mockFunkoRepository.Setup(x => x.GetById(99))
                .ReturnsAsync((Funko?)null);
            _mockCategoriaRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Categoria());

            // Act
            var resultado = await _service.Update(UpdateRequest("Updated"), 99);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<NotFoundError>();
            _mockFunkoRepository.Verify(r => r.ExitsName(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task Update_NombreNuevoDuplicado_DevuelveConflict()
        {
            _mockFunkoRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Funko());
            _mockFunkoRepository.Setup(x => x.ExitsName("Existing")).ReturnsAsync(true);
            _mockCategoriaRepository.Setup(x => x.GetById(1)).ReturnsAsync(TestData.Categoria());

            // Act
            var resultado = await _service.Update(UpdateRequest("Existing"), 1);

            // Assert
            resultado.IsFailure.Should().BeTrue();
            resultado.Error.Should().BeOfType<ConflictError>();
            _mockFunkoRepository.Verify(r => r.Update(It.IsAny<Funko>()), Times.Never);
        }
    }
}
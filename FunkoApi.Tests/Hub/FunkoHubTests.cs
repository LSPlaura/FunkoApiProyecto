using System.Security.Claims;
using FluentAssertions;
using FunkoApi.Back.Hub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Hub;

[TestFixture]
public class FunkoHubTests {
    private Mock<HubCallerContext> _contextMock = null!;
    private Mock<IGroupManager> _groupManagerMock = null!;
    private Mock<IHubCallerClients> _clientsMock = null!;
    private Mock<ISingleClientProxy> _clientProxyMock = null!;
    private FunkoHub _hub = null!;

    [SetUp]
    public void Setup()
    {
        _contextMock = new Mock<HubCallerContext>();
        _groupManagerMock = new Mock<IGroupManager>();
        _clientsMock = new Mock<IHubCallerClients>();
        _clientProxyMock = new Mock<ISingleClientProxy>();
        _contextMock.Setup(c => c.ConnectionId).Returns("test-connection-id");
        _groupManagerMock
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _clientProxyMock
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _hub = new FunkoHub
        {
            Context = _contextMock.Object,
            Groups = _groupManagerMock.Object,
            Clients = _clientsMock.Object
        };
    }

    [TearDown]
    public void TearDown()
    {
        _hub?.Dispose();
    }

    [Test]
    public void FunkoHub_TieneAuthorizeAttribute_Clase()
    {
        // Arrange
        var tipo = typeof(FunkoHub);

        // Act
        var atributos = tipo.GetCustomAttributes(typeof(AuthorizeAttribute), true);

        // Assert
        atributos.Should().ContainSingle();
        ((AuthorizeAttribute)atributos[0]).Roles.Should().Be("ADMIN");
    }

    [Test]
    public async Task OnConnectedAsync_AdminUser_SeAñadeAGruposUserYAdmin()
    {
        // Arrange
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "ADMIN")
        }, "TestAuthType"));
        _contextMock.Setup(c => c.User).Returns(usuario);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync("test-connection-id", "user-1",
            It.IsAny<CancellationToken>()), Times.Once);
        _groupManagerMock.Verify(g => g.AddToGroupAsync("test-connection-id", "admins",
            It.IsAny<CancellationToken>()), Times.Once);
        _groupManagerMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task OnConnectedAsync_RegularUser_SeAñadeSoloAGrupoUser()
    {
        // Arrange
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Role, "USER")
        }, "TestAuthType"));
        _contextMock.Setup(c => c.User).Returns(usuario);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync("test-connection-id", "user-2",
            It.IsAny<CancellationToken>()), Times.Once);
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), "admins",
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_UserSinId_NoSeAñadeAGrupo()
    {
        // Arrange
        _contextMock.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_UserNulo_NoSeAñadeAGrupo()
    {
        // Arrange
        _contextMock.Setup(c => c.User).Returns((ClaimsPrincipal?)null);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OnDisconnectedAsync_ConOSinError_NoLanzaExcepcion(bool conError)
    {
        // Arrange
        var excepcion = conError ? new Exception("Connection lost") : null;

        // Act
        Func<Task> act = () => _hub.OnDisconnectedAsync(excepcion);

        // Assert
        await act.Should().NotThrowAsync();
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SendMessage_MensajeValido_EnviaATodosLosClientes()
    {
        // Arrange
        _clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        var antes = DateTime.UtcNow;

        // Act
        await _hub.SendMessage("Lucia", "Hola");
        var despues = DateTime.UtcNow;

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync("ReceiveMessage",
            It.Is<object?[]>(args => args.Length == 3 && Equals(args[0], "Lucia") &&
                Equals(args[1], "Hola") && args[2] is DateTime &&
                (DateTime)args[2]! >= antes && (DateTime)args[2]! <= despues),
            It.IsAny<CancellationToken>()), Times.Once);
        _clientsMock.VerifyGet(c => c.All, Times.Once);
        _clientsMock.VerifyNoOtherCalls();
        _clientProxyMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task SendToCaller_MensajeValido_EnviaSoloAlEmisor()
    {
        // Arrange
        _clientsMock.Setup(c => c.Caller).Returns(_clientProxyMock.Object);
        var antes = DateTime.UtcNow;

        // Act
        await _hub.SendToCaller("Hola");
        var despues = DateTime.UtcNow;

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync("ReceiveMessage",
            It.Is<object?[]>(args => args.Length == 3 && Equals(args[0], "Sistema") &&
                Equals(args[1], "Hola") && args[2] is DateTime &&
                (DateTime)args[2]! >= antes && (DateTime)args[2]! <= despues),
            It.IsAny<CancellationToken>()), Times.Once);
        _clientsMock.VerifyGet(c => c.Caller, Times.Once);
        _clientsMock.VerifyNoOtherCalls();
        _clientProxyMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task SendToGroup_MensajeValido_EnviaSoloAlGrupoIndicado()
    {
        // Arrange
        _clientsMock.Setup(c => c.Group("admins")).Returns(_clientProxyMock.Object);
        var antes = DateTime.UtcNow;

        // Act
        await _hub.SendToGroup("admins", "Hola");
        var despues = DateTime.UtcNow;

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync("ReceiveMessage",
            It.Is<object?[]>(args => args.Length == 3 && Equals(args[0], "Sistema") &&
                Equals(args[1], "Hola") && args[2] is DateTime &&
                (DateTime)args[2]! >= antes && (DateTime)args[2]! <= despues),
            It.IsAny<CancellationToken>()), Times.Once);
        _clientsMock.Verify(c => c.Group("admins"), Times.Once);
        _clientsMock.VerifyNoOtherCalls();
        _clientProxyMock.VerifyNoOtherCalls();
    }
}

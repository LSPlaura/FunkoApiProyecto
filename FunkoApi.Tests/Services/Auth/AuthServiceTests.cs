using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using FunkoApi.Back.Config;
using FunkoApi.Back.Data;
using FunkoApi.Back.Dto.Auth;
using FunkoApi.Back.Errors;
using FunkoApi.Back.Models;
using FunkoApi.Back.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace FunkoApi.Tests.Services.Auth;

// Igual que IdentityAuthServiceTests del profesor: Identity real con EF InMemory.
// No se simula SignInManager ni se sustituye el algoritmo de contraseñas.
[TestFixture]
public class AuthServiceTests
{
    private ServiceProvider _provider = null!;
    // El contenedor crea y libera estas dependencias; el único recurso propio es _provider.
    private UserManager<AppUser> _users => _provider.GetRequiredService<UserManager<AppUser>>();
    private AppDbContext _context => _provider.GetRequiredService<AppDbContext>();
    private AuthService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<AppUser, IdentityRole<long>>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
        services.Configure<JwtSettings>(o =>
        {
            o.Key = new string('t', 64); // Clave sintética exclusiva de los tests.
            o.Issuer = "funko-tests";
            o.Audience = "funko-tests";
            o.ExpirationMinutes = 5;
        });
        services.AddScoped<AuthService>();
        _provider = services.BuildServiceProvider();
        _service = _provider.GetRequiredService<AuthService>();
        var roles = _provider.GetRequiredService<RoleManager<IdentityRole<long>>>();
        await roles.CreateAsync(new IdentityRole<long>("USER"));
        await roles.CreateAsync(new IdentityRole<long>("ADMIN"));
    }

    [TearDown]
    public void TearDown() => _provider?.Dispose();

    [Test]
    public async Task SignupAsync_DatosValidos_CreaUsuarioConRolYToken()
    {
        // Arrange
        var dto = new SignupDto("nueva@example.test", "Prueba123!", "Lucia", "Pruebas");

        // Act
        var resultado = await _service.SignupAsync(dto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Email.Should().Be(dto.Email);
        resultado.Value.Role.Should().Be("USER");
        resultado.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        var user = await _users.FindByEmailAsync(dto.Email);
        user.Should().NotBeNull();
        user!.Nombre.Should().Be(dto.Nombre);
        user.Apellidos.Should().Be(dto.Apellidos);
        (await _users.IsInRoleAsync(user, "USER")).Should().BeTrue();
        (await _users.CheckPasswordAsync(user, dto.Password)).Should().BeTrue();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(resultado.Value.Token);
        jwt.Issuer.Should().Be("funko-tests");
        jwt.Audiences.Should().Contain("funko-tests");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == dto.Email);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "USER");
    }

    [Test]
    public async Task SignupAsync_EmailDuplicado_RetornaConflictSinDuplicarUsuario()
    {
        // Arrange
        var dto = new SignupDto("duplicada@example.test", "Prueba123!", "Lucia", "Pruebas");
        await _service.SignupAsync(dto);

        // Act
        var resultado = await _service.SignupAsync(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ConflictError.Duplicate("usuario", dto.Email));
        (await _context.Users.CountAsync(u => u.Email == dto.Email)).Should().Be(1);
    }

    [TestCase("x")]
    [TestCase("prueba123!")]
    [TestCase("PRUEBA123!")]
    [TestCase("PruebaSinNumero!")]
    [TestCase("Prueba123SinSimbolo")]
    public async Task SignupAsync_PasswordInvalida_RetornaBusinessRuleErrorSinCrearUsuario(string password)
    {
        // Arrange
        var dto = new SignupDto("invalida@example.test", password, "Lucia", "Pruebas");

        // Act
        var resultado = await _service.SignupAsync(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<BusinessRuleError>().Which.Message.Should().StartWith("Error al crear el usuario:");
        (await _users.FindByEmailAsync(dto.Email)).Should().BeNull();
    }

    [TestCase(false, "Prueba123!")]
    [TestCase(true, "Incorrecta123!")]
    public async Task LoginAsync_UsuarioAusenteOPasswordIncorrecta_RetornaUnauthorized(bool existe, string password)
    {
        // Arrange
        const string email = "login@example.test";
        if (existe) await _users.CreateAsync(new AppUser { UserName = email, Email = email }, "Prueba123!");
        var dto = new LoginDto(email, password);

        // Act
        var resultado = await _service.LoginAsync(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UnauthorizedError.InvalidCredentials());
    }

    [TestCase(null, "USER")]
    [TestCase("ADMIN", "ADMIN")]
    public async Task LoginAsync_CredencialesValidas_RetornaTokenYRol(string? rol, string esperado)
    {
        // Arrange
        var user = new AppUser { UserName = "valida@example.test", Email = "valida@example.test", Nombre = "Lucia", Apellidos = "Pruebas" };
        await _users.CreateAsync(user, "Prueba123!");
        if (rol != null) await _users.AddToRoleAsync(user, rol);
        var dto = new LoginDto(user.Email, "Prueba123!");

        // Act
        var resultado = await _service.LoginAsync(dto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Role.Should().Be(esperado);
        resultado.Value.Token.Should().NotBeNullOrWhiteSpace();
        resultado.Value.Email.Should().Be(user.Email);
    }

    [Test]
    public async Task GetMeAsync_UsuarioSinEmail_RetornaCadenaVaciaEnPerfil()
    {
        // Arrange
        // Dato existente sin email: no se crea mediante signup ni se alteran sus validaciones.
        var user = new AppUser { UserName = "sin-email", Email = null, Nombre = "Lucia", Apellidos = "Pruebas" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var resultado = await _service.GetMeAsync(user.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeEquivalentTo(new UserDto(user.Id, "", user.Nombre, user.Apellidos, "USER"));
    }

    [Test]
    public async Task GetMeAsync_Inexistente_RetornaNotFound()
    {
        // Arrange
        const long id = 999;

        // Act
        var resultado = await _service.GetMeAsync(id);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(NotFoundError.FromId(id, "Usuario"));
    }

    [TestCase(null, "USER")]
    [TestCase("ADMIN", "ADMIN")]
    public async Task GetMeAsync_Existente_RetornaPerfilConRol(string? rol, string esperado)
    {
        // Arrange
        var user = new AppUser { UserName = "perfil@example.test", Email = "perfil@example.test", Nombre = "Lucia", Apellidos = "Pruebas" };
        await _users.CreateAsync(user, "Prueba123!");
        if (rol != null) await _users.AddToRoleAsync(user, rol);

        // Act
        var resultado = await _service.GetMeAsync(user.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().BeEquivalentTo(new UserDto(user.Id, user.Email, user.Nombre, user.Apellidos, esperado));
    }
}

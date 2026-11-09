using FluentAssertions;
using FunkoApi.Back.Config;
using FunkoApi.Back.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace FunkoApi.Tests.Infrastructure;

[TestFixture]
public class IdentityConfigTests
{
    [Test]
    public void AddIdentityConfiguration_ConfigValida_RegistraPoliticasYValidaJWT()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('t', 64), ["Jwt:Issuer"] = "tests", ["Jwt:Audience"] = "tests", ["Jwt:ExpirationMinutes"] = "5"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        var environment = new Mock<IWebHostEnvironment>().Object;

        // Act
        var resultado = services.AddIdentityConfiguration(configuration, environment);
        using var provider = services.BuildServiceProvider();
        var identity = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;
        var authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        var authorization = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var settings = provider.GetRequiredService<IOptions<JwtSettings>>().Value;

        // Assert
        resultado.Should().BeSameAs(services);
        identity.Password.RequiredLength.Should().Be(8);
        identity.Password.RequireDigit.Should().BeTrue();
        identity.Password.RequireLowercase.Should().BeTrue();
        identity.Password.RequireUppercase.Should().BeTrue();
        identity.Password.RequireNonAlphanumeric.Should().BeTrue();
        identity.User.RequireUniqueEmail.Should().BeTrue();
        identity.Lockout.MaxFailedAccessAttempts.Should().Be(5);
        identity.Lockout.DefaultLockoutTimeSpan.Should().Be(TimeSpan.FromMinutes(5));
        authentication.DefaultScheme.Should().Be("Bearer");
        authentication.DefaultChallengeScheme.Should().Be("Bearer");
        jwt.TokenValidationParameters.ValidIssuer.Should().Be("tests");
        jwt.TokenValidationParameters.ValidAudience.Should().Be("tests");
        jwt.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        jwt.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
        jwt.TokenValidationParameters.ValidateAudience.Should().BeTrue();
        jwt.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwt.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
        settings.ExpirationMinutes.Should().Be(5);
        authorization.GetPolicy("RequireAdmin")!.Requirements.OfType<RolesAuthorizationRequirement>()
            .Single().AllowedRoles.Should().Equal("ADMIN");
        authorization.GetPolicy("RequireUser")!.Requirements.OfType<RolesAuthorizationRequirement>()
            .Single().AllowedRoles.Should().BeEquivalentTo("USER", "ADMIN");
    }

    [TestCase("/hubs/funkos", "token-de-prueba", true)]
    [TestCase("/hubs/funkos/negotiate", "token-de-prueba", true)]
    [TestCase("/hubs/funkos", "", false)]
    [TestCase("/hubs/funkos", null, false)]
    [TestCase("/api/funkos", "token-de-prueba", false)]
    [TestCase("/hubs/otro", "token-de-prueba", false)]
    [TestCase("/hubs/funkos-otro", "token-de-prueba", false)]
    public async Task OnMessageReceived_TokenEnQuery_SoloLoLeeEnRutaDelHub(string ruta, string? token, bool esperado)
    {
        // Arrange: se prueba la extraccion, no se usa ni valida un JWT real.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('t', 64), ["Jwt:Issuer"] = "tests", ["Jwt:Audience"] = "tests"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityConfiguration(configuration, new Mock<IWebHostEnvironment>().Object);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = ruta;
        if (token is not null) httpContext.Request.QueryString = QueryString.Create("access_token", token);
        var context = new MessageReceivedContext(httpContext,
            new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), options);

        // Act
        await options.Events.MessageReceived(context);

        // Assert
        context.Token.Should().Be(esperado ? token : null);
    }
}

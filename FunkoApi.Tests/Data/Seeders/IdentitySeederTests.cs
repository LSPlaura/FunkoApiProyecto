using FluentAssertions;
using FunkoApi.Back.Data;
using FunkoApi.Back.Data.Seed.Identity;
using FunkoApi.Back.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace FunkoApi.Tests.Seeders;

[TestFixture]
public class IdentitySeederTests
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task SeedAsync_DatosNuevosExistentesOPasswordRechazada_CreaSoloLoNecesario(bool existen, bool fallaCreacion)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<AppUser, IdentityRole<long>>(o => o.Password.RequiredLength = fallaCreacion ? 128 : 8)
            .AddEntityFrameworkStores<AppDbContext>();
        await using var provider = services.BuildServiceProvider();
        var usuarios = provider.GetRequiredService<UserManager<AppUser>>();
        var roles = provider.GetRequiredService<RoleManager<IdentityRole<long>>>();
        if (existen)
        {
            await roles.CreateAsync(new IdentityRole<long>("ADMIN"));
            await roles.CreateAsync(new IdentityRole<long>("USER"));
            await usuarios.CreateAsync(new AppUser { UserName = "admin@admin.com", Email = "admin@admin.com", Nombre = "Previo" }, "Prueba123!");
            await usuarios.CreateAsync(new AppUser { UserName = "user@user.com", Email = "user@user.com", Nombre = "Previo" }, "Prueba123!");
        }

        // Act
        await IdentitySeeder.SeedAsync(provider);

        // Assert
        var context = provider.GetRequiredService<AppDbContext>();
        (await context.Roles.CountAsync()).Should().Be(2);
        (await context.Users.CountAsync()).Should().Be(fallaCreacion ? 0 : 2);
        if (!fallaCreacion)
        {
            var admin = await usuarios.FindByEmailAsync("admin@admin.com");
            var user = await usuarios.FindByEmailAsync("user@user.com");
            admin!.Nombre.Should().Be(existen ? "Previo" : "Admin");
            user!.Nombre.Should().Be(existen ? "Previo" : "Usuario");
            if (!existen)
            {
                (await usuarios.IsInRoleAsync(admin, "ADMIN")).Should().BeTrue();
                (await usuarios.IsInRoleAsync(user, "USER")).Should().BeTrue();
            }
        }
    }
}

using FluentAssertions;
using FunkoApi.Back.Models;
using NUnit.Framework;

namespace FunkoApi.Tests.Models;

[TestFixture]
public class AppUserTests
{
    [Test]
    public void AppUser_ValoresPorDefecto_IniciaVacioYConFechas()
    {
        // Arrange
        var antes = DateTime.UtcNow;

        // Act
        var usuario = new AppUser();

        // Assert
        usuario.Nombre.Should().BeEmpty();
        usuario.Apellidos.Should().BeEmpty();
        usuario.IsDeleted.Should().BeFalse();
        usuario.CreatedAt.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        usuario.UpdatedAt.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Test]
    public void AppUser_PropiedadesAsignadas_ConservanLosValores()
    {
        // Arrange
        var fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var usuario = new AppUser
        {
            UserName = "usuario@example.test",
            Email = "usuario@example.test",
            Nombre = "Lucia",
            Apellidos = "Pruebas",
            IsDeleted = true,
            CreatedAt = fecha,
            UpdatedAt = fecha
        };

        // Act
        var valores = usuario;

        // Assert
        valores.UserName.Should().Be("usuario@example.test");
        valores.Email.Should().Be("usuario@example.test");
        valores.Nombre.Should().Be("Lucia");
        valores.Apellidos.Should().Be("Pruebas");
        valores.IsDeleted.Should().BeTrue();
        valores.CreatedAt.Should().Be(fecha);
        valores.UpdatedAt.Should().Be(fecha);
    }
}

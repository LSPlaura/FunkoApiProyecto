using FunkoApi.Back.Generators;
using FunkoApi.Back.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace FunkoApi.Back.Data;

/// <summary>
/// Contexto de base de datos de Entity Framework Core.
/// Gestiona las entidades de Funkos, categorías y proveedores.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, IdentityRole<long>, long>(options) {
    public DbSet<Funko> Funkos => Set<Funko>();
    public DbSet<Categoria> Categorias => Set<Categoria>();

    /// <summary>
    /// Configura el modelo de datos y sus mapeos mediante Fluent API.
    /// Define restricciones de columna y conversiones personalizadas de tipos complejos (Address, Company)
    /// a cadenas JSON para compatibilidad con SQLite.
    /// </summary>
    /// <param name="modelBuilder">El constructor de modelos utilizado para configurar las entidades de la base de datos.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Funko>(entity => {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            entity.HasIndex(p => p.Nombre).IsUnique();
            entity.Property(p => p.Precio).IsRequired().HasPrecision(18, 2);
            entity.Property(p => p.Stock).IsRequired();

            entity.Property(f => f.ProveedorId)
                .HasConversion(
                    id => id.ToString(),
                    valor => ObjectId.Parse(valor)
                );

            entity.HasOne(e => e.Categoria)
                .WithMany()
                .HasForeignKey(p => p.CategoriaId);

            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.IsDeleted);
        });

        modelBuilder.Entity<Categoria>(entity => {
            entity.Property(p => p.Id)
                .HasValueGenerator<YouTubeIdValueGenerator>()
                .ValueGeneratedOnAdd();
            entity.HasIndex(p => p.Nombre).IsUnique();
            entity.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.IsDeleted);
        });
    }
}
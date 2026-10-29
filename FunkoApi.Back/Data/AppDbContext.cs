using FunkoApi.Back.Generators;
using FunkoApi.Back.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Back.Data;

/// <summary>
///     Contexto de base de datos de Entity Framework Core.
///     Gestiona las entidades de Funkos, categorías y proveedores.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
    public DbSet<Funko> Funkos => Set<Funko>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Proveedor> Proveedor => Set<Proveedor>();

    /// <summary>
    ///     Configura el modelo de datos y sus mapeos mediante Fluent API.
    ///     Define restricciones de columna y conversiones personalizadas de tipos complejos (Address, Company)
    ///     a cadenas JSON para compatibilidad con SQLite.
    /// </summary>
    /// <param name="modelBuilder">El constructor de modelos utilizado para configurar las entidades de la base de datos.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Funko>(entity => {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            entity.HasIndex(p => p.Nombre).IsUnique();
            entity.Property(p => p.Precio).IsRequired().HasPrecision(18, 2);
            entity.Property(p => p.Stock).IsRequired();

            entity.HasOne(e => e.Proveedor)
                .WithMany()
                .HasForeignKey(p => p.ProveedorId);

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

        modelBuilder.Entity<Proveedor>(entity => {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Nombre).IsUnique();
            entity.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(p => p.IsDeleted);
            entity.Property(p => p.CreatedAt);
            entity.Property(p => p.UpdatedAt);
        });
    }
}
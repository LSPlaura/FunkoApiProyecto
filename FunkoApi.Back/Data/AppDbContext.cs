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
    /// <summary>Conjunto de funkos almacenados en la base de datos relacional.</summary>
    public DbSet<Funko> Funkos => Set<Funko>();
    /// <summary>Conjunto de categorías asociables a los funkos.</summary>
    public DbSet<Categoria> Categorias => Set<Categoria>();

    /// <summary>
    /// Configura el modelo de datos y sus mapeos mediante Fluent API.
    /// Establece índices, restricciones y la relación con categorías en PostgreSQL.
    /// Convierte el ObjectId del proveedor a una cadena para persistir su referencia.
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
            entity.HasIndex(p => p.ProveedorId);

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

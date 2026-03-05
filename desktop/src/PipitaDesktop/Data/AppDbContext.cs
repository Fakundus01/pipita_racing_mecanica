using Microsoft.EntityFrameworkCore;
using PipitaDesktop.Models;

namespace PipitaDesktop.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Parte> Partes => Set<Parte>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Reporte> Reportes => Set<Reporte>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("clientes");
            entity.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Telefono).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(120);
            entity.Property(x => x.Estado).HasMaxLength(40).HasDefaultValue("activo");
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.ToTable("vehiculos");
            entity.Property(x => x.Marca).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Modelo).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Version).HasMaxLength(80);
            entity.Property(x => x.Patente).HasMaxLength(20);
            entity.Property(x => x.Estado).HasMaxLength(40).HasDefaultValue("disponible");
            entity.HasIndex(x => x.Patente).IsUnique();
            entity
                .HasOne(x => x.Cliente)
                .WithMany(x => x.Vehiculos)
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Parte>(entity =>
        {
            entity.ToTable("partes");
            entity.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Costo).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.ToTable("servicios");
            entity.Property(x => x.Descripcion).IsRequired();
            entity.Property(x => x.Costo).HasPrecision(12, 2);
            entity
                .HasOne(x => x.Vehiculo)
                .WithMany(x => x.Servicios)
                .HasForeignKey(x => x.VehiculoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reporte>(entity =>
        {
            entity.ToTable("reportes");
            entity.Property(x => x.Titulo).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Periodo).HasMaxLength(40);
        });

        ConfigureBaseEntity<Cliente>(modelBuilder);
        ConfigureBaseEntity<Vehiculo>(modelBuilder);
        ConfigureBaseEntity<Parte>(modelBuilder);
        ConfigureBaseEntity<Servicio>(modelBuilder);
        ConfigureBaseEntity<Reporte>(modelBuilder);
    }

    private static void ConfigureBaseEntity<TEntity>(ModelBuilder modelBuilder)
        where TEntity : BaseEntity
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });
    }
}

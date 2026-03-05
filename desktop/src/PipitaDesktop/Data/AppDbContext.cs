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
    public DbSet<SolicitudCliente> SolicitudesCliente => Set<SolicitudCliente>();
    public DbSet<Distribuidora> Distribuidoras => Set<Distribuidora>();
    public DbSet<TrabajoDistribuidora> TrabajosDistribuidora => Set<TrabajoDistribuidora>();

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

        modelBuilder.Entity<SolicitudCliente>(entity =>
        {
            entity.ToTable("solicitudes_cliente");
            entity.Property(x => x.Descripcion).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Estado).HasMaxLength(30).HasDefaultValue("pendiente");
            entity.Property(x => x.Prioridad).HasMaxLength(20).HasDefaultValue("media");
            entity.Property(x => x.Canal).HasMaxLength(40);
            entity.Property(x => x.Notas).HasMaxLength(4000);
            entity.HasIndex(x => x.FechaSolicitud);
            entity.HasIndex(x => x.Estado);

            entity
                .HasOne(x => x.Cliente)
                .WithMany(x => x.Solicitudes)
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.Vehiculo)
                .WithMany(x => x.Solicitudes)
                .HasForeignKey(x => x.VehiculoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Distribuidora>(entity =>
        {
            entity.ToTable("distribuidoras");
            entity.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Rubro).HasMaxLength(80);
            entity.Property(x => x.Telefono).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(120);
            entity.Property(x => x.Estado).HasMaxLength(30).HasDefaultValue("activa");
            entity.Property(x => x.Notas).HasMaxLength(4000);
            entity.HasIndex(x => x.Nombre);
        });

        modelBuilder.Entity<TrabajoDistribuidora>(entity =>
        {
            entity.ToTable("trabajos_distribuidora");
            entity.Property(x => x.Descripcion).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Costo).HasPrecision(12, 2);
            entity.Property(x => x.EstadoPago).HasMaxLength(30).HasDefaultValue("pagado");
            entity.Property(x => x.Notas).HasMaxLength(4000);
            entity.HasIndex(x => x.Fecha);
            entity.HasIndex(x => x.DistribuidoraId);

            entity
                .HasOne(x => x.Distribuidora)
                .WithMany(x => x.Trabajos)
                .HasForeignKey(x => x.DistribuidoraId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Cliente)
                .WithMany(x => x.TrabajosDistribuidora)
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.Vehiculo)
                .WithMany(x => x.TrabajosDistribuidora)
                .HasForeignKey(x => x.VehiculoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        ConfigureBaseEntity<Cliente>(modelBuilder);
        ConfigureBaseEntity<Vehiculo>(modelBuilder);
        ConfigureBaseEntity<Parte>(modelBuilder);
        ConfigureBaseEntity<Servicio>(modelBuilder);
        ConfigureBaseEntity<Reporte>(modelBuilder);
        ConfigureBaseEntity<SolicitudCliente>(modelBuilder);
        ConfigureBaseEntity<Distribuidora>(modelBuilder);
        ConfigureBaseEntity<TrabajoDistribuidora>(modelBuilder);
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

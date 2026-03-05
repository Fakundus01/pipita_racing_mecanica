using System.IO;
using Microsoft.EntityFrameworkCore;

namespace PipitaDesktop.Data;

public static class DatabaseInitializer
{
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DatabasePathProvider.DatabaseDirectory);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={DatabasePathProvider.DatabasePath}")
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        EnsureSchemaUpgrades(db);
    }

    private static void EnsureSchemaUpgrades(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");

        db.Database.ExecuteSqlRaw(
            @"CREATE TABLE IF NOT EXISTS solicitudes_cliente (
                Id INTEGER NOT NULL CONSTRAINT PK_solicitudes_cliente PRIMARY KEY AUTOINCREMENT,
                ClienteId INTEGER NULL,
                VehiculoId INTEGER NULL,
                Descripcion TEXT NOT NULL,
                FechaSolicitud TEXT NOT NULL,
                Estado TEXT NOT NULL,
                Prioridad TEXT NOT NULL,
                Canal TEXT NULL,
                Notas TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                CONSTRAINT FK_solicitudes_cliente_clientes_ClienteId FOREIGN KEY (ClienteId) REFERENCES clientes (Id) ON DELETE SET NULL,
                CONSTRAINT FK_solicitudes_cliente_vehiculos_VehiculoId FOREIGN KEY (VehiculoId) REFERENCES vehiculos (Id) ON DELETE SET NULL
            );");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_solicitudes_cliente_ClienteId ON solicitudes_cliente (ClienteId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_solicitudes_cliente_VehiculoId ON solicitudes_cliente (VehiculoId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_solicitudes_cliente_Estado ON solicitudes_cliente (Estado);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_solicitudes_cliente_FechaSolicitud ON solicitudes_cliente (FechaSolicitud);");

        db.Database.ExecuteSqlRaw(
            @"CREATE TABLE IF NOT EXISTS distribuidoras (
                Id INTEGER NOT NULL CONSTRAINT PK_distribuidoras PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Rubro TEXT NULL,
                Telefono TEXT NULL,
                Email TEXT NULL,
                Estado TEXT NOT NULL,
                Notas TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_distribuidoras_Nombre ON distribuidoras (Nombre);");

        db.Database.ExecuteSqlRaw(
            @"CREATE TABLE IF NOT EXISTS trabajos_distribuidora (
                Id INTEGER NOT NULL CONSTRAINT PK_trabajos_distribuidora PRIMARY KEY AUTOINCREMENT,
                DistribuidoraId INTEGER NOT NULL,
                ClienteId INTEGER NULL,
                VehiculoId INTEGER NULL,
                Descripcion TEXT NOT NULL,
                Fecha TEXT NOT NULL,
                Costo TEXT NOT NULL,
                EstadoPago TEXT NOT NULL,
                Notas TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                CONSTRAINT FK_trabajos_distribuidora_distribuidoras_DistribuidoraId FOREIGN KEY (DistribuidoraId) REFERENCES distribuidoras (Id) ON DELETE CASCADE,
                CONSTRAINT FK_trabajos_distribuidora_clientes_ClienteId FOREIGN KEY (ClienteId) REFERENCES clientes (Id) ON DELETE SET NULL,
                CONSTRAINT FK_trabajos_distribuidora_vehiculos_VehiculoId FOREIGN KEY (VehiculoId) REFERENCES vehiculos (Id) ON DELETE SET NULL
            );");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_trabajos_distribuidora_DistribuidoraId ON trabajos_distribuidora (DistribuidoraId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_trabajos_distribuidora_ClienteId ON trabajos_distribuidora (ClienteId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_trabajos_distribuidora_VehiculoId ON trabajos_distribuidora (VehiculoId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_trabajos_distribuidora_Fecha ON trabajos_distribuidora (Fecha);");
        db.Database.ExecuteSqlRaw(
            @"CREATE TABLE IF NOT EXISTS citas (
                Id INTEGER NOT NULL CONSTRAINT PK_citas PRIMARY KEY AUTOINCREMENT,
                ClienteId INTEGER NULL,
                VehiculoId INTEGER NULL,
                FechaHoraInicio TEXT NOT NULL,
                DuracionMinutos INTEGER NOT NULL,
                Estado TEXT NOT NULL,
                Motivo TEXT NOT NULL,
                Notas TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                CONSTRAINT FK_citas_clientes_ClienteId FOREIGN KEY (ClienteId) REFERENCES clientes (Id) ON DELETE SET NULL,
                CONSTRAINT FK_citas_vehiculos_VehiculoId FOREIGN KEY (VehiculoId) REFERENCES vehiculos (Id) ON DELETE SET NULL
            );");

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_citas_ClienteId ON citas (ClienteId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_citas_VehiculoId ON citas (VehiculoId);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_citas_FechaHoraInicio ON citas (FechaHoraInicio);");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_citas_Estado ON citas (Estado);");
    }
}


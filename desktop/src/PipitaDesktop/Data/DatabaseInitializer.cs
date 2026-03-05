using System;
using System.Data;
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
                FechaHoraCita TEXT NULL,
                DuracionMinutos INTEGER NOT NULL DEFAULT 60,
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
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_solicitudes_cliente_FechaHoraCita ON solicitudes_cliente (FechaHoraCita);");

        EnsureColumnExists(db, "solicitudes_cliente", "FechaHoraCita", "TEXT NULL");
        EnsureColumnExists(db, "solicitudes_cliente", "DuracionMinutos", "INTEGER NOT NULL DEFAULT 60");

        db.Database.ExecuteSqlRaw("UPDATE solicitudes_cliente SET FechaHoraCita = COALESCE(FechaHoraCita, FechaSolicitud);");
        db.Database.ExecuteSqlRaw("UPDATE solicitudes_cliente SET DuracionMinutos = COALESCE(DuracionMinutos, 60) WHERE DuracionMinutos IS NULL OR DuracionMinutos <= 0;");

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

        db.Database.ExecuteSqlRaw(
            @"INSERT INTO solicitudes_cliente (
                ClienteId,
                VehiculoId,
                Descripcion,
                FechaSolicitud,
                FechaHoraCita,
                DuracionMinutos,
                Estado,
                Prioridad,
                Canal,
                Notas,
                CreatedAt,
                UpdatedAt)
              SELECT
                c.ClienteId,
                c.VehiculoId,
                c.Motivo,
                date(c.FechaHoraInicio),
                c.FechaHoraInicio,
                CASE
                    WHEN c.DuracionMinutos IS NULL OR c.DuracionMinutos <= 0 THEN 60
                    ELSE c.DuracionMinutos
                END,
                COALESCE(c.Estado, 'pendiente'),
                'media',
                NULL,
                c.Notas,
                COALESCE(c.CreatedAt, CURRENT_TIMESTAMP),
                COALESCE(c.UpdatedAt, CURRENT_TIMESTAMP)
              FROM citas c
              WHERE NOT EXISTS (
                SELECT 1
                FROM solicitudes_cliente s
                WHERE IFNULL(s.ClienteId, -1) = IFNULL(c.ClienteId, -1)
                  AND IFNULL(s.VehiculoId, -1) = IFNULL(c.VehiculoId, -1)
                  AND s.FechaHoraCita = c.FechaHoraInicio
                  AND s.Descripcion = c.Motivo
              );");
    }

    private static void EnsureColumnExists(AppDbContext db, string tableName, string columnName, string definition)
    {
        using var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var existingColumn = reader[1]?.ToString();
            if (string.Equals(existingColumn, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        alterCommand.ExecuteNonQuery();
    }
}

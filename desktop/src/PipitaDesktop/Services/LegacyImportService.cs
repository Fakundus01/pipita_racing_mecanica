using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PipitaDesktop.Data;
using PipitaDesktop.Models;
namespace PipitaDesktop.Services;

public static class LegacyImportService
{
    public sealed record ImportResult(
        int Clientes,
        int Vehiculos,
        int Partes,
        int Servicios,
        int Reportes);

    public static async Task<ImportResult> ImportAsync(
        AppDbContext db,
        string legacyDbPath,
        bool replaceExistingData)
    {
        if (string.IsNullOrWhiteSpace(legacyDbPath) || !File.Exists(legacyDbPath))
        {
            throw new FileNotFoundException("No se encontro la base legacy para importar.", legacyDbPath);
        }

        var data = await ReadLegacyDataAsync(legacyDbPath);

        using var transaction = await db.Database.BeginTransactionAsync();
        var autoDetectChanges = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            if (replaceExistingData)
            {
                await ClearCurrentDataAsync(db);
            }

            await db.Clientes.AddRangeAsync(data.Clientes);
            await db.Vehiculos.AddRangeAsync(data.Vehiculos);
            await db.Partes.AddRangeAsync(data.Partes);
            await db.Servicios.AddRangeAsync(data.Servicios);
            await db.Reportes.AddRangeAsync(data.Reportes);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }

        return new ImportResult(
            data.Clientes.Count,
            data.Vehiculos.Count,
            data.Partes.Count,
            data.Servicios.Count,
            data.Reportes.Count);
    }

    private static async Task ClearCurrentDataAsync(AppDbContext db)
    {
        await db.Servicios.ExecuteDeleteAsync();
        await db.Reportes.ExecuteDeleteAsync();
        await db.Partes.ExecuteDeleteAsync();
        await db.Vehiculos.ExecuteDeleteAsync();
        await db.Clientes.ExecuteDeleteAsync();
    }

    private static async Task<LegacyData> ReadLegacyDataAsync(string dbPath)
    {
        var data = new LegacyData();

        await using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        if (await TableExistsAsync(connection, "clientes"))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, nombre, telefono, email, estado, created_at, updated_at FROM clientes ORDER BY id";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var createdAt = ParseDateTime(reader, 5) ?? DateTime.UtcNow;
                var updatedAt = ParseDateTime(reader, 6) ?? createdAt;

                data.Clientes.Add(
                    new Cliente
                    {
                        Id = id,
                        Nombre = NonEmpty(reader.GetStringOrNull(1), $"Cliente {id}"),
                        Telefono = reader.GetStringOrNull(2),
                        Email = reader.GetStringOrNull(3),
                        Estado = NonEmpty(reader.GetStringOrNull(4), "activo"),
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt,
                    });
            }
        }

        if (await TableExistsAsync(connection, "vehiculos"))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, marca, modelo, version, anio, patente, estado, cliente_id, created_at, updated_at FROM vehiculos ORDER BY id";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var createdAt = ParseDateTime(reader, 8) ?? DateTime.UtcNow;
                var updatedAt = ParseDateTime(reader, 9) ?? createdAt;

                data.Vehiculos.Add(
                    new Vehiculo
                    {
                        Id = id,
                        Marca = NonEmpty(reader.GetStringOrNull(1), "Marca N/D"),
                        Modelo = NonEmpty(reader.GetStringOrNull(2), "Modelo N/D"),
                        Version = reader.GetStringOrNull(3),
                        Anio = reader.GetIntOrNull(4),
                        Patente = NormalizeNullable(reader.GetStringOrNull(5))?.ToUpperInvariant(),
                        Estado = NonEmpty(reader.GetStringOrNull(6), "disponible"),
                        ClienteId = reader.GetIntOrNull(7),
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt,
                    });
            }
        }

        if (await TableExistsAsync(connection, "partes"))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, nombre, stock, costo, created_at, updated_at FROM partes ORDER BY id";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var createdAt = ParseDateTime(reader, 4) ?? DateTime.UtcNow;
                var updatedAt = ParseDateTime(reader, 5) ?? createdAt;

                data.Partes.Add(
                    new Parte
                    {
                        Id = id,
                        Nombre = NonEmpty(reader.GetStringOrNull(1), $"Parte {id}"),
                        Stock = reader.GetIntOrNull(2) ?? 0,
                        Costo = reader.GetDecimalOrDefault(3),
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt,
                    });
            }
        }

        if (await TableExistsAsync(connection, "servicios"))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, vehiculo_id, descripcion, fecha, kilometraje, costo, notas, created_at, updated_at FROM servicios ORDER BY id";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var createdAt = ParseDateTime(reader, 7) ?? DateTime.UtcNow;
                var updatedAt = ParseDateTime(reader, 8) ?? createdAt;

                data.Servicios.Add(
                    new Servicio
                    {
                        Id = id,
                        VehiculoId = reader.GetIntOrNull(1) ?? 0,
                        Descripcion = NonEmpty(reader.GetStringOrNull(2), $"Servicio {id}"),
                        Fecha = ParseDateTime(reader, 3)?.Date ?? DateTime.Today,
                        Kilometraje = reader.GetIntOrNull(4),
                        Costo = reader.GetDecimalOrDefault(5),
                        Notas = reader.GetStringOrNull(6),
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt,
                    });
            }

            data.Servicios.RemoveAll(x => x.VehiculoId <= 0);
        }

        if (await TableExistsAsync(connection, "reportes"))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, titulo, periodo, generado_el, created_at, updated_at FROM reportes ORDER BY id";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var createdAt = ParseDateTime(reader, 4) ?? DateTime.UtcNow;
                var updatedAt = ParseDateTime(reader, 5) ?? createdAt;

                data.Reportes.Add(
                    new Reporte
                    {
                        Id = id,
                        Titulo = NonEmpty(reader.GetStringOrNull(1), $"Reporte {id}"),
                        Periodo = reader.GetStringOrNull(2),
                        GeneradoEl = ParseDateTime(reader, 3)?.Date ?? DateTime.Today,
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt,
                    });
            }
        }

        var clienteIds = data.Clientes.Select(x => x.Id).ToHashSet();
        foreach (var vehiculo in data.Vehiculos)
        {
            if (vehiculo.ClienteId.HasValue && !clienteIds.Contains(vehiculo.ClienteId.Value))
            {
                vehiculo.ClienteId = null;
            }
        }

        var seenPatentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var vehiculo in data.Vehiculos)
        {
            if (string.IsNullOrWhiteSpace(vehiculo.Patente))
            {
                continue;
            }

            if (!seenPatentes.Add(vehiculo.Patente))
            {
                vehiculo.Patente = null;
            }
        }

        var vehiculoIds = data.Vehiculos.Select(x => x.Id).ToHashSet();
        data.Servicios.RemoveAll(x => !vehiculoIds.Contains(x.VehiculoId));

        return data;
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1";
        command.Parameters.AddWithValue("$name", tableName);
        var result = await command.ExecuteScalarAsync();
        return result is not null;
    }

    private static DateTime? ParseDateTime(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var raw = reader.GetValue(ordinal);
        return raw switch
        {
            DateTime dateTime => dateTime,
            string text when DateTime.TryParse(text, out var parsed) => parsed,
            _ => null,
        };
    }

    private static string NonEmpty(string? value, string fallback)
    {
        var normalized = NormalizeNullable(value);
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed class LegacyData
    {
        public List<Cliente> Clientes { get; } = new();
        public List<Vehiculo> Vehiculos { get; } = new();
        public List<Parte> Partes { get; } = new();
        public List<Servicio> Servicios { get; } = new();
        public List<Reporte> Reportes { get; } = new();
    }
}

internal static class SqliteDataReaderExtensions
{
    public static string? GetStringOrNull(this SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static int? GetIntOrNull(this SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var raw = reader.GetValue(ordinal);
        return raw switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => null,
        };
    }

    public static decimal GetDecimalOrDefault(this SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        var raw = reader.GetValue(ordinal);
        return raw switch
        {
            decimal decimalValue => decimalValue,
            double doubleValue => Convert.ToDecimal(doubleValue),
            float floatValue => Convert.ToDecimal(floatValue),
            long longValue => longValue,
            int intValue => intValue,
            string text when decimal.TryParse(text, out var parsed) => parsed,
            _ => 0,
        };
    }
}




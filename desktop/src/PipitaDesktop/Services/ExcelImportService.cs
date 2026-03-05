using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PipitaDesktop.Data;
using PipitaDesktop.Models;

namespace PipitaDesktop.Services;

public static class ExcelImportService
{
    private const int HeaderRow = 4;
    private const int DataRow = 5;

    public sealed record ImportSectionResult(int Created, int Updated, int Skipped);

    public sealed record ImportResult(
        ImportSectionResult Clientes,
        ImportSectionResult Vehiculos,
        ImportSectionResult Partes,
        ImportSectionResult Servicios,
        ImportSectionResult Reportes,
        ImportSectionResult Solicitudes,
        ImportSectionResult Distribuidoras,
        ImportSectionResult TrabajosDistribuidora)
    {
        public int TotalCreated => Clientes.Created + Vehiculos.Created + Partes.Created + Servicios.Created + Reportes.Created + Solicitudes.Created + Distribuidoras.Created + TrabajosDistribuidora.Created;
        public int TotalUpdated => Clientes.Updated + Vehiculos.Updated + Partes.Updated + Servicios.Updated + Reportes.Updated + Solicitudes.Updated + Distribuidoras.Updated + TrabajosDistribuidora.Updated;
        public int TotalSkipped => Clientes.Skipped + Vehiculos.Skipped + Partes.Skipped + Servicios.Skipped + Reportes.Skipped + Solicitudes.Skipped + Distribuidoras.Skipped + TrabajosDistribuidora.Skipped;

        public string BuildSummary()
        {
            return string.Join(
                Environment.NewLine,
                "Importacion Excel finalizada.",
                string.Empty,
                $"Creados: {TotalCreated}",
                $"Actualizados: {TotalUpdated}",
                $"Saltados: {TotalSkipped}",
                string.Empty,
                $"Clientes -> +{Clientes.Created} / ~{Clientes.Updated} / !{Clientes.Skipped}",
                $"Vehiculos -> +{Vehiculos.Created} / ~{Vehiculos.Updated} / !{Vehiculos.Skipped}",
                $"Partes -> +{Partes.Created} / ~{Partes.Updated} / !{Partes.Skipped}",
                $"Servicios -> +{Servicios.Created} / ~{Servicios.Updated} / !{Servicios.Skipped}",
                $"Reportes -> +{Reportes.Created} / ~{Reportes.Updated} / !{Reportes.Skipped}",
                $"Solicitudes -> +{Solicitudes.Created} / ~{Solicitudes.Updated} / !{Solicitudes.Skipped}",
                $"Distribuidoras -> +{Distribuidoras.Created} / ~{Distribuidoras.Updated} / !{Distribuidoras.Skipped}",
                $"Trabajos dist. -> +{TrabajosDistribuidora.Created} / ~{TrabajosDistribuidora.Updated} / !{TrabajosDistribuidora.Skipped}");
        }
    }

    public static async Task<ImportResult> ImportAsync(AppDbContext db, string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("No se encontro el archivo Excel a importar.", filePath);
        }

        using var workbook = new XLWorkbook(filePath);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;

        var clientes = await db.Clientes.ToListAsync();
        var clientesResult = ImportClientes(workbook, db, clientes, now);
        await db.SaveChangesAsync();

        clientes = await db.Clientes.ToListAsync();
        var vehiculos = await db.Vehiculos.Include(x => x.Cliente).ToListAsync();
        var vehiculosResult = ImportVehiculos(workbook, db, vehiculos, clientes, now);
        await db.SaveChangesAsync();

        var partes = await db.Partes.ToListAsync();
        var partesResult = ImportPartes(workbook, db, partes, now);
        await db.SaveChangesAsync();

        var distribuidoras = await db.Distribuidoras.ToListAsync();
        var distribuidorasResult = ImportDistribuidoras(workbook, db, distribuidoras, now);
        await db.SaveChangesAsync();

        var reportes = await db.Reportes.ToListAsync();
        var reportesResult = ImportReportes(workbook, db, reportes, now);
        await db.SaveChangesAsync();

        vehiculos = await db.Vehiculos.Include(x => x.Cliente).ToListAsync();
        var servicios = await db.Servicios.Include(x => x.Vehiculo).ToListAsync();
        var serviciosResult = ImportServicios(workbook, db, servicios, vehiculos, now);
        await db.SaveChangesAsync();

        clientes = await db.Clientes.ToListAsync();
        vehiculos = await db.Vehiculos.Include(x => x.Cliente).ToListAsync();
        var solicitudes = await db.SolicitudesCliente.Include(x => x.Cliente).Include(x => x.Vehiculo).ToListAsync();
        var solicitudesResult = ImportSolicitudes(workbook, db, solicitudes, clientes, vehiculos, now);
        await db.SaveChangesAsync();

        distribuidoras = await db.Distribuidoras.ToListAsync();
        clientes = await db.Clientes.ToListAsync();
        vehiculos = await db.Vehiculos.Include(x => x.Cliente).ToListAsync();
        var trabajos = await db.TrabajosDistribuidora
            .Include(x => x.Distribuidora)
            .Include(x => x.Cliente)
            .Include(x => x.Vehiculo)
            .ToListAsync();
        var trabajosResult = ImportTrabajosDistribuidora(workbook, db, trabajos, distribuidoras, clientes, vehiculos, now);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        return new ImportResult(
            clientesResult,
            vehiculosResult,
            partesResult,
            serviciosResult,
            reportesResult,
            solicitudesResult,
            distribuidorasResult,
            trabajosResult);
    }

    private static ImportSectionResult ImportClientes(XLWorkbook workbook, AppDbContext db, List<Cliente> existing, DateTime now)
    {
        var reader = SheetReader.TryCreate(workbook, "Clientes");
        if (reader is null)
        {
            return new ImportSectionResult(0, 0, 0);
        }

        var byId = existing.ToDictionary(x => x.Id);
        var byName = BuildLookup(existing, x => x.Nombre);
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in reader.GetDataRows())
        {
            var nombre = reader.GetText(row, "Nombre");
            if (string.IsNullOrWhiteSpace(nombre))
            {
                skipped++;
                continue;
            }

            var id = reader.GetNullableInt(row, "ID");
            var cliente = FindEntity(byId, id) ?? FindEntity(byName, nombre);
            var isNew = cliente is null;
            if (isNew)
            {
                cliente = new Cliente
                {
                    Id = id.GetValueOrDefault(),
                    CreatedAt = reader.GetNullableDateTime(row, "Creado") ?? now,
                };
                db.Clientes.Add(cliente);
                existing.Add(cliente);
            }

            cliente!.Nombre = nombre;
            cliente.Telefono = EmptyToNull(reader.GetText(row, "Telefono"));
            cliente.Email = EmptyToNull(reader.GetText(row, "Email"));
            cliente.Estado = EmptyToNull(reader.GetText(row, "Estado")) ?? "activo";
            cliente.UpdatedAt = reader.GetNullableDateTime(row, "Actualizado") ?? now;

            if (isNew)
            {
                created++;
                if (cliente.Id > 0)
                {
                    byId[cliente.Id] = cliente;
                }
            }
            else
            {
                updated++;
            }

            byName[cliente.Nombre] = cliente;
        }

        return new ImportSectionResult(created, updated, skipped);
    }

    private static ImportSectionResult ImportVehiculos(XLWorkbook workbook, AppDbContext db, List<Vehiculo> existing, List<Cliente> clientes, DateTime now)
    {
        var reader = SheetReader.TryCreate(workbook, "Vehiculos");
        if (reader is null)
        {
            return new ImportSectionResult(0, 0, 0);
        }

        var byId = existing.ToDictionary(x => x.Id);
        var byPatente = BuildLookup(existing, x => NormalizePatente(x.Patente));
        var clientesByNombre = BuildLookup(clientes, x => x.Nombre);
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in reader.GetDataRows())
        {
            var marca = reader.GetText(row, "Marca");
            var modelo = reader.GetText(row, "Modelo");
            if (string.IsNullOrWhiteSpace(marca) || string.IsNullOrWhiteSpace(modelo))
            {
                skipped++;
                continue;
            }

            var patente = NormalizePatente(reader.GetText(row, "Patente"));
            var id = reader.GetNullableInt(row, "ID");
            var vehiculo = FindEntity(byId, id) ?? FindEntity(byPatente, patente);
            var isNew = vehiculo is null;
            if (isNew)
            {
                vehiculo = new Vehiculo
                {
                    Id = id.GetValueOrDefault(),
                    CreatedAt = reader.GetNullableDateTime(row, "Creado") ?? now,
                };
                db.Vehiculos.Add(vehiculo);
                existing.Add(vehiculo);
            }

            var clienteNombre = reader.GetText(row, "Cliente");
            vehiculo!.Patente = EmptyToNull(patente);
            vehiculo.Marca = marca;
            vehiculo.Modelo = modelo;
            vehiculo.Version = EmptyToNull(reader.GetText(row, "Version"));
            vehiculo.Anio = reader.GetNullableInt(row, "Anio");
            vehiculo.Estado = EmptyToNull(reader.GetText(row, "Estado")) ?? "disponible";
            vehiculo.ClienteId = FindEntity(clientesByNombre, clienteNombre)?.Id;
            vehiculo.UpdatedAt = now;

            if (isNew)
            {
                created++;
                if (vehiculo.Id > 0)
                {
                    byId[vehiculo.Id] = vehiculo;
                }
            }
            else
            {
                updated++;
            }

            if (!string.IsNullOrWhiteSpace(vehiculo.Patente))
            {
                byPatente[vehiculo.Patente] = vehiculo;
            }
        }

        return new ImportSectionResult(created, updated, skipped);
    }

    private static ImportSectionResult ImportPartes(XLWorkbook workbook, AppDbContext db, List<Parte> existing, DateTime now)
    {
        var reader = SheetReader.TryCreate(workbook, "Partes");
        if (reader is null)
        {
            return new ImportSectionResult(0, 0, 0);
        }

        var byId = existing.ToDictionary(x => x.Id);
        var byName = BuildLookup(existing, x => x.Nombre);
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in reader.GetDataRows())
        {
            var nombre = reader.GetText(row, "Nombre");
            if (string.IsNullOrWhiteSpace(nombre))
            {
                skipped++;
                continue;
            }

            var id = reader.GetNullableInt(row, "ID");
            var parte = FindEntity(byId, id) ?? FindEntity(byName, nombre);
            var isNew = parte is null;
            if (isNew)
            {
                parte = new Parte
                {
                    Id = id.GetValueOrDefault(),
                    CreatedAt = reader.GetNullableDateTime(row, "Creado") ?? now,
                };
                db.Partes.Add(parte);
                existing.Add(parte);
            }

            parte!.Nombre = nombre;
            parte.Stock = reader.GetNullableInt(row, "Stock") ?? 0;
            parte.Costo = reader.GetNullableDecimal(row, "Precio c/u") ?? 0m;
            parte.UpdatedAt = now;

            if (isNew)
            {
                created++;
                if (parte.Id > 0)
                {
                    byId[parte.Id] = parte;
                }
            }
            else
            {
                updated++;
            }

            byName[parte.Nombre] = parte;
        }

        return new ImportSectionResult(created, updated, skipped);
    }

    private static ImportSectionResult ImportDistribuidoras(XLWorkbook workbook, AppDbContext db, List<Distribuidora> existing, DateTime now)
    {
        var reader = SheetReader.TryCreate(workbook, "Distribuidoras");
        if (reader is null)
        {
            return new ImportSectionResult(0, 0, 0);
        }

        var byId = existing.ToDictionary(x => x.Id);
        var byName = BuildLookup(existing, x => x.Nombre);
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in reader.GetDataRows())
        {
            var nombre = reader.GetText(row, "Nombre");
            if (string.IsNullOrWhiteSpace(nombre))
            {
                skipped++;
                continue;
            }

            var id = reader.GetNullableInt(row, "ID");
            var distribuidora = FindEntity(byId, id) ?? FindEntity(byName, nombre);
            var isNew = distribuidora is null;
            if (isNew)
            {
                distribuidora = new Distribuidora
                {
                    Id = id.GetValueOrDefault(),
                    CreatedAt = reader.GetNullableDateTime(row, "Creado") ?? now,
                };
                db.Distribuidoras.Add(distribuidora);
                existing.Add(distribuidora);
            }

            distribuidora!.Nombre = nombre;
            distribuidora.Rubro = EmptyToNull(reader.GetText(row, "Rubro"));
            distribuidora.Telefono = EmptyToNull(reader.GetText(row, "Telefono"));
            distribuidora.Email = EmptyToNull(reader.GetText(row, "Email"));
            distribuidora.Estado = EmptyToNull(reader.GetText(row, "Estado")) ?? "activa";
            distribuidora.UpdatedAt = now;

            if (isNew)
            {
                created++;
                if (distribuidora.Id > 0)
                {
                    byId[distribuidora.Id] = distribuidora;
                }
            }
            else
            {
                updated++;
            }

            byName[distribuidora.Nombre] = distribuidora;
        }

        return new ImportSectionResult(created, updated, skipped);
    }

    private static ImportSectionResult ImportReportes(XLWorkbook workbook, AppDbContext db, List<Reporte> existing, DateTime now)
    {
        var reader = SheetReader.TryCreate(workbook, "Reportes");
        if (reader is null)
        {
            return new ImportSectionResult(0, 0, 0);
        }

        var byId = existing.ToDictionary(x => x.Id);
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in reader.GetDataRows())
        {
            var titulo = reader.GetText(row, "Titulo");
            if (string.IsNullOrWhiteSpace(titulo))
            {
                skipped++;
                continue;
            }

            var periodo = reader.GetText(row, "Periodo");
            var id = reader.GetNullableInt(row, "ID");
            var reporte = FindEntity(byId, id) ?? existing.FirstOrDefault(x => string.Equals(x.Titulo, titulo, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Periodo, periodo, StringComparison.OrdinalIgnoreCase));
            var isNew = reporte is null;
            if (isNew)
            {
                reporte = new Reporte
                {
                    Id = id.GetValueOrDefault(),
                    CreatedAt = reader.GetNullableDateTime(row, "Creado") ?? now,
                };
                db.Reportes.Add(reporte);
                existing.Add(reporte);
            }

            reporte!.Titulo = titulo;
            reporte.Periodo = EmptyToNull(periodo);
            reporte.GeneradoEl = reader.GetNullableDateTime(row, "Generado")?.Date ?? DateTime.Today;
            reporte.UpdatedAt = now;

            if (isNew)
            {
                created++;
                if (reporte.Id > 0)
                {
                    byId[reporte.Id] = reporte;
                }
            }
            else
            {
                updated++;
            }
        }

        return new ImportSectionResult(created, updated, skipped);
    }

    private static ImportSectionResult ImportServicios(XLWorkbook workbook, AppDbContext db, List<Servicio> existing, List<Vehiculo> vehiculos, DateTime now)
    {
        var reader = SheetReader.TryCreate(workbook, "Servicios");
        if (reader is null)
        {
            return new ImportSectionResult(0, 0, 0);
        }

        var byId = existing.ToDictionary(x => x.Id);
        var vehiculosByPatente = BuildLookup(vehiculos, x => NormalizePatente(x.Patente));
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in reader.GetDataRows())
        {
            var descripcion = reader.GetText(row, "Descripcion");
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                skipped++;
                continue;
            }

            var patente = NormalizePatente(reader.GetText(row, "Patente"));
            var vehiculo = FindEntity(vehiculosByPatente, patente);
            var id = reader.GetNullableInt(row, "ID");
            var servicio = FindEntity(byId, id);
            var isNew = servicio is null;
            if (servicio is null && vehiculo is null)
            {
                skipped++;
                continue;
            }

            if (isNew)
            {
                servicio = new Servicio
                {
                    Id = id.GetValueOrDefault(),
                    CreatedAt = now,
                };
                db.Servicios.Add(servicio);
                existing.Add(servicio);
            }

            servicio!.VehiculoId = vehiculo?.Id ?? servicio.VehiculoId;
            servicio.Descripcion = descripcion;
            servicio.Fecha = reader.GetNullableDateTime(row, "Fecha")?.Date ?? DateTime.Today;
            servicio.Kilometraje = reader.GetNullableInt(row, "Kilometraje");
            servicio.Costo = reader.GetNullableDecimal(row, "Costo") ?? 0m;
            servicio.Notas = EmptyToNull(reader.GetText(row, "Notas"));
            servicio.UpdatedAt = now;

            if (isNew)
            {
                created++;
                if (servicio.Id > 0)
                {
                    byId[servicio.Id] = servicio;
                }
            }
            else
            {
                updated++;
            }
        }

        return new ImportSectionResult(created, updated, skipped);
    }

    private static ImportSectionResult ImportSolicitudes(XLWorkbook workbook, AppDbContext db, List<SolicitudCliente> existing, List<Cliente> clientes, List<Vehiculo> vehiculos, DateTime now)
    {
        var reader = SheetReader.TryCreate(workbook, "Solicitudes");
        if (reader is null)
        {
            return new ImportSectionResult(0, 0, 0);
        }

        var byId = existing.ToDictionary(x => x.Id);
        var clientesByNombre = BuildLookup(clientes, x => x.Nombre);
        var vehiculosByPatente = BuildLookup(vehiculos, x => NormalizePatente(x.Patente));
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in reader.GetDataRows())
        {
            var descripcion = reader.GetText(row, "Descripcion");
            var inicio = reader.GetNullableDateTime(row, "Inicio");
            if (string.IsNullOrWhiteSpace(descripcion) || !inicio.HasValue)
            {
                skipped++;
                continue;
            }

            var fin = reader.GetNullableDateTime(row, "Fin");
            var duracion = reader.GetNullableInt(row, "Duracion (min)") ?? (fin.HasValue ? Math.Max(1, (int)(fin.Value - inicio.Value).TotalMinutes) : 60);
            var id = reader.GetNullableInt(row, "ID");
            var solicitud = FindEntity(byId, id);
            var isNew = solicitud is null;
            if (isNew)
            {
                solicitud = new SolicitudCliente
                {
                    Id = id.GetValueOrDefault(),
                    CreatedAt = reader.GetNullableDateTime(row, "Creado") ?? now,
                };
                db.SolicitudesCliente.Add(solicitud);
                existing.Add(solicitud);
            }

            solicitud!.ClienteId = FindEntity(clientesByNombre, reader.GetText(row, "Cliente"))?.Id;
            solicitud.VehiculoId = FindEntity(vehiculosByPatente, NormalizePatente(reader.GetText(row, "Patente")))?.Id;
            solicitud.Descripcion = descripcion;
            solicitud.FechaSolicitud = solicitud.FechaSolicitud == default ? inicio.Value.Date : solicitud.FechaSolicitud;
            solicitud.FechaHoraCita = inicio.Value;
            solicitud.DuracionMinutos = Math.Max(1, duracion);
            solicitud.Estado = EmptyToNull(reader.GetText(row, "Estado")) ?? "pendiente";
            solicitud.Prioridad = EmptyToNull(reader.GetText(row, "Prioridad")) ?? "media";
            solicitud.Canal = EmptyToNull(reader.GetText(row, "Canal"));
            solicitud.Notas = EmptyToNull(reader.GetText(row, "Notas"));
            solicitud.UpdatedAt = now;

            if (isNew)
            {
                created++;
                if (solicitud.Id > 0)
                {
                    byId[solicitud.Id] = solicitud;
                }
            }
            else
            {
                updated++;
            }
        }

        return new ImportSectionResult(created, updated, skipped);
    }

    private static ImportSectionResult ImportTrabajosDistribuidora(
        XLWorkbook workbook,
        AppDbContext db,
        List<TrabajoDistribuidora> existing,
        List<Distribuidora> distribuidoras,
        List<Cliente> clientes,
        List<Vehiculo> vehiculos,
        DateTime now)
    {
        var reader = SheetReader.TryCreate(workbook, "Trabajos Dist.");
        if (reader is null)
        {
            return new ImportSectionResult(0, 0, 0);
        }

        var byId = existing.ToDictionary(x => x.Id);
        var distribuidorasByNombre = BuildLookup(distribuidoras, x => x.Nombre);
        var clientesByNombre = BuildLookup(clientes, x => x.Nombre);
        var vehiculosByPatente = BuildLookup(vehiculos, x => NormalizePatente(x.Patente));
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in reader.GetDataRows())
        {
            var descripcion = reader.GetText(row, "Descripcion");
            var distribuidora = FindEntity(distribuidorasByNombre, reader.GetText(row, "Distribuidora"));
            if (string.IsNullOrWhiteSpace(descripcion) || distribuidora is null)
            {
                skipped++;
                continue;
            }

            var id = reader.GetNullableInt(row, "ID");
            var trabajo = FindEntity(byId, id);
            var isNew = trabajo is null;
            if (isNew)
            {
                trabajo = new TrabajoDistribuidora
                {
                    Id = id.GetValueOrDefault(),
                    CreatedAt = reader.GetNullableDateTime(row, "Creado") ?? now,
                };
                db.TrabajosDistribuidora.Add(trabajo);
                existing.Add(trabajo);
            }

            trabajo!.DistribuidoraId = distribuidora.Id;
            trabajo.ClienteId = FindEntity(clientesByNombre, reader.GetText(row, "Cliente"))?.Id;
            trabajo.VehiculoId = FindEntity(vehiculosByPatente, NormalizePatente(reader.GetText(row, "Patente")))?.Id;
            trabajo.Descripcion = descripcion;
            trabajo.Fecha = reader.GetNullableDateTime(row, "Fecha")?.Date ?? DateTime.Today;
            trabajo.Costo = reader.GetNullableDecimal(row, "Costo") ?? 0m;
            trabajo.EstadoPago = EmptyToNull(reader.GetText(row, "Estado pago")) ?? "pagado";
            trabajo.Notas = EmptyToNull(reader.GetText(row, "Notas"));
            trabajo.UpdatedAt = now;

            if (isNew)
            {
                created++;
                if (trabajo.Id > 0)
                {
                    byId[trabajo.Id] = trabajo;
                }
            }
            else
            {
                updated++;
            }
        }

        return new ImportSectionResult(created, updated, skipped);
    }

    private static Dictionary<string, TEntity> BuildLookup<TEntity>(IEnumerable<TEntity> source, Func<TEntity, string?> keySelector)
        where TEntity : class
    {
        var lookup = new Dictionary<string, TEntity>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source)
        {
            var key = keySelector(item)?.Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                lookup[key] = item;
            }
        }

        return lookup;
    }

    private static TEntity? FindEntity<TEntity>(Dictionary<int, TEntity> lookup, int? id)
        where TEntity : class
    {
        if (!id.HasValue || id.Value <= 0)
        {
            return null;
        }

        lookup.TryGetValue(id.Value, out var entity);
        return entity;
    }

    private static TEntity? FindEntity<TEntity>(Dictionary<string, TEntity> lookup, string? key)
        where TEntity : class
    {
        var normalized = key?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        lookup.TryGetValue(normalized, out var entity);
        return entity;
    }

    private static string? EmptyToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string NormalizePatente(string? patente)
    {
        return patente?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    private sealed class SheetReader
    {
        private readonly IXLWorksheet _worksheet;
        private readonly Dictionary<string, int> _columns;
        private readonly int _lastRow;

        private SheetReader(IXLWorksheet worksheet)
        {
            _worksheet = worksheet;
            _columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (var col = 1; col <= lastColumn; col++)
            {
                var header = worksheet.Cell(HeaderRow, col).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    _columns[header] = col;
                }
            }

            _lastRow = worksheet.LastRowUsed()?.RowNumber() ?? HeaderRow;
        }

        public static SheetReader? TryCreate(XLWorkbook workbook, string sheetName)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault(x => string.Equals(x.Name, sheetName, StringComparison.OrdinalIgnoreCase));
            return worksheet is null ? null : new SheetReader(worksheet);
        }

        public IEnumerable<int> GetDataRows()
        {
            for (var row = DataRow; row <= _lastRow; row++)
            {
                if (IsRowEmpty(row))
                {
                    continue;
                }

                if (string.Equals(_worksheet.Cell(row, 1).GetString().Trim(), "Sin datos para exportar.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return row;
            }
        }

        public string GetText(int row, string header)
        {
            return TryGetCell(row, header)?.GetString().Trim() ?? string.Empty;
        }

        public int? GetNullableInt(int row, string header)
        {
            var cell = TryGetCell(row, header);
            if (cell is null || cell.IsEmpty())
            {
                return null;
            }

            if (cell.TryGetValue<double>(out var number))
            {
                return Convert.ToInt32(Math.Round(number, MidpointRounding.AwayFromZero));
            }

            var text = cell.GetString().Trim();
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ||
                int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
            {
                return parsed;
            }

            return null;
        }

        public decimal? GetNullableDecimal(int row, string header)
        {
            var cell = TryGetCell(row, header);
            if (cell is null || cell.IsEmpty())
            {
                return null;
            }

            if (cell.TryGetValue<double>(out var number))
            {
                return Convert.ToDecimal(number, CultureInfo.InvariantCulture);
            }

            var text = cell.GetString().Trim().Replace("$", string.Empty);
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ||
                decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
            {
                return parsed;
            }

            return null;
        }

        public DateTime? GetNullableDateTime(int row, string header)
        {
            var cell = TryGetCell(row, header);
            if (cell is null || cell.IsEmpty())
            {
                return null;
            }

            if (cell.TryGetValue<DateTime>(out var dateTime))
            {
                return dateTime;
            }

            var text = cell.GetString().Trim();
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed) ||
                DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
            {
                return parsed;
            }

            return null;
        }

        private IXLCell? TryGetCell(int row, string header)
        {
            return _columns.TryGetValue(header, out var column) ? _worksheet.Cell(row, column) : null;
        }

        private bool IsRowEmpty(int row)
        {
            foreach (var column in _columns.Values)
            {
                if (!_worksheet.Cell(row, column).IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }
    }
}

using System.IO;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PipitaDesktop.Data;
using PipitaDesktop.Models;

namespace PipitaDesktop.Services;

public static class ExcelExportService
{
    public sealed record ExportData(
        IReadOnlyList<Cliente> Clientes,
        IReadOnlyList<Vehiculo> Vehiculos,
        IReadOnlyList<Parte> Partes,
        IReadOnlyList<Servicio> Servicios,
        IReadOnlyList<Reporte> Reportes,
        IReadOnlyList<SolicitudCliente> Solicitudes,
        IReadOnlyList<Distribuidora> Distribuidoras,
        IReadOnlyList<TrabajoDistribuidora> TrabajosDistribuidora);

    public static async Task<ExportData> LoadAsync(AppDbContext db)
    {
        var clientes = await db.Clientes
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        var vehiculos = await db.Vehiculos
            .AsNoTracking()
            .Include(x => x.Cliente)
            .OrderBy(x => x.Marca)
            .ThenBy(x => x.Modelo)
            .ToListAsync();

        var partes = await db.Partes
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        var servicios = await db.Servicios
            .AsNoTracking()
            .Include(x => x.Vehiculo)
            .ThenInclude(x => x!.Cliente)
            .OrderByDescending(x => x.Fecha)
            .ToListAsync();

        var reportes = await db.Reportes
            .AsNoTracking()
            .OrderByDescending(x => x.GeneradoEl)
            .ToListAsync();

        var solicitudes = await db.SolicitudesCliente
            .AsNoTracking()
            .Include(x => x.Cliente)
            .Include(x => x.Vehiculo)
            .OrderByDescending(x => x.FechaHoraCita)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        var distribuidoras = await db.Distribuidoras
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        var trabajosDistribuidora = await db.TrabajosDistribuidora
            .AsNoTracking()
            .Include(x => x.Distribuidora)
            .Include(x => x.Cliente)
            .Include(x => x.Vehiculo)
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return new ExportData(
            clientes,
            vehiculos,
            partes,
            servicios,
            reportes,
            solicitudes,
            distribuidoras,
            trabajosDistribuidora);
    }

    public static void ExportToFile(ExportData data, string filePath)
    {
        using var workbook = File.Exists(filePath)
            ? new XLWorkbook(filePath)
            : new XLWorkbook();

        BuildSummarySheet(workbook, data);
        BuildClientesSheet(workbook, data.Clientes);
        BuildVehiculosSheet(workbook, data.Vehiculos);
        BuildPartesSheet(workbook, data.Partes);
        BuildServiciosSheet(workbook, data.Servicios);
        BuildReportesSheet(workbook, data.Reportes);
        BuildSolicitudesSheet(workbook, data.Solicitudes);
        BuildDistribuidorasSheet(workbook, data.Distribuidoras, data.TrabajosDistribuidora);
        BuildTrabajosDistribuidoraSheet(workbook, data.TrabajosDistribuidora);

        workbook.SaveAs(filePath);
    }

    private static IXLWorksheet ReplaceWorksheet(XLWorkbook workbook, string sheetName)
    {
        var existing = workbook.Worksheets.FirstOrDefault(x => string.Equals(x.Name, sheetName, StringComparison.OrdinalIgnoreCase));
        existing?.Delete();
        return workbook.Worksheets.Add(sheetName);
    }

    private static void BuildSummarySheet(XLWorkbook workbook, ExportData data)
    {
        var ws = ReplaceWorksheet(workbook, "Resumen");

        ws.Cell(1, 1).Value = "Pipita Garage - Reporte General";
        ws.Range(1, 1, 1, 4).Merge();
        ws.Range(1, 1, 1, 4).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 4).Style.Font.FontSize = 16;
        ws.Range(1, 1, 1, 4).Style.Font.FontColor = XLColor.White;
        ws.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#0F6FDE");
        ws.Row(1).Height = 26;

        ws.Cell(2, 1).Value = "Generado";
        ws.Cell(2, 2).Value = DateTime.Now;
        ws.Cell(2, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

        var valorInventario = data.Partes.Sum(x => x.Stock * x.Costo);
        var gastoDistribuidoras = data.TrabajosDistribuidora.Sum(x => x.Costo);

        var metrics = new List<object[]>
        {
            new object[] { "Clientes", data.Clientes.Count },
            new object[] { "Vehiculos", data.Vehiculos.Count },
            new object[] { "Partes", data.Partes.Count },
            new object[] { "Servicios", data.Servicios.Count },
            new object[] { "Reportes", data.Reportes.Count },
            new object[] { "Solicitudes de clientes", data.Solicitudes.Count },
            new object[] { "Distribuidoras", data.Distribuidoras.Count },
            new object[] { "Trabajos tercerizados", data.TrabajosDistribuidora.Count },
            new object[] { "Stock total de partes", data.Partes.Sum(x => x.Stock) },
            new object[] { "Precio total de partes", data.Partes.Sum(x => x.Costo) },
            new object[] { "Valor inventario (stock x precio c/u)", valorInventario },
            new object[] { "Costo total de servicios", data.Servicios.Sum(x => x.Costo) },
            new object[] { "Gasto total distribuidoras", gastoDistribuidoras },
        };

        const int headerRow = 4;
        ws.Cell(headerRow, 1).Value = "Indicador";
        ws.Cell(headerRow, 2).Value = "Valor";
        StyleHeader(ws.Range(headerRow, 1, headerRow, 2));

        var row = headerRow + 1;
        foreach (var metric in metrics)
        {
            ws.Cell(row, 1).Value = metric[0]?.ToString() ?? string.Empty;
            SetCellValue(ws.Cell(row, 2), metric[1]);
            row++;
        }

        if (row > headerRow + 1)
        {
            var range = ws.Range(headerRow, 1, row - 1, 2);
            var table = range.CreateTable("TblResumen");
            table.Theme = XLTableTheme.TableStyleMedium9;
            ws.Range(headerRow + 1, 2, row - 1, 2).Style.NumberFormat.Format = "#,##0.00";
        }

        ws.Columns(1, 4).AdjustToContents();
        ws.SheetView.FreezeRows(headerRow);
    }

    private static void BuildClientesSheet(XLWorkbook workbook, IReadOnlyList<Cliente> clientes)
    {
        var rows = clientes
            .Select(x => new object?[]
            {
                x.Id,
                x.Nombre,
                x.Telefono,
                x.Email,
                x.Estado,
                x.CreatedAt.ToLocalTime(),
                x.UpdatedAt.ToLocalTime(),
            })
            .ToList();

        BuildDataSheet(
            workbook,
            "Clientes",
            "Clientes cargados",
            new[] { "ID", "Nombre", "Telefono", "Email", "Estado", "Creado", "Actualizado" },
            rows,
            new Dictionary<int, string>
            {
                [6] = "dd/MM/yyyy HH:mm",
                [7] = "dd/MM/yyyy HH:mm",
            });
    }

    private static void BuildVehiculosSheet(XLWorkbook workbook, IReadOnlyList<Vehiculo> vehiculos)
    {
        var rows = vehiculos
            .Select(x => new object?[]
            {
                x.Id,
                x.Patente,
                x.Marca,
                x.Modelo,
                x.Version,
                x.Anio,
                x.Estado,
                x.Cliente?.Nombre,
                x.CreatedAt.ToLocalTime(),
            })
            .ToList();

        BuildDataSheet(
            workbook,
            "Vehiculos",
            "Inventario de vehiculos",
            new[] { "ID", "Patente", "Marca", "Modelo", "Version", "Anio", "Estado", "Cliente", "Creado" },
            rows,
            new Dictionary<int, string>
            {
                [9] = "dd/MM/yyyy HH:mm",
            });
    }

    private static void BuildPartesSheet(XLWorkbook workbook, IReadOnlyList<Parte> partes)
    {
        var rows = partes
            .Select(x => new object?[]
            {
                x.Id,
                x.Nombre,
                x.Stock,
                x.Costo,
                x.Stock * x.Costo,
                x.CreatedAt.ToLocalTime(),
            })
            .ToList();

        BuildDataSheet(
            workbook,
            "Partes",
            "Partes y accesorios",
            new[] { "ID", "Nombre", "Stock", "Precio c/u", "Total estimado", "Creado" },
            rows,
            new Dictionary<int, string>
            {
                [3] = "#,##0",
                [4] = "$ #,##0.00",
                [5] = "$ #,##0.00",
                [6] = "dd/MM/yyyy HH:mm",
            });
    }

    private static void BuildServiciosSheet(XLWorkbook workbook, IReadOnlyList<Servicio> servicios)
    {
        var rows = servicios
            .Select(x => new object?[]
            {
                x.Id,
                x.Vehiculo?.Patente,
                x.Vehiculo is null ? null : $"{x.Vehiculo.Marca} {x.Vehiculo.Modelo}",
                x.Vehiculo?.Cliente?.Nombre,
                x.Descripcion,
                x.Fecha,
                x.Kilometraje,
                x.Costo,
                x.Notas,
            })
            .ToList();

        BuildDataSheet(
            workbook,
            "Servicios",
            "Servicios y tareas realizadas",
            new[] { "ID", "Patente", "Vehiculo", "Cliente", "Descripcion", "Fecha", "Kilometraje", "Costo", "Notas" },
            rows,
            new Dictionary<int, string>
            {
                [6] = "dd/MM/yyyy",
                [7] = "#,##0",
                [8] = "$ #,##0.00",
            });
    }

    private static void BuildReportesSheet(XLWorkbook workbook, IReadOnlyList<Reporte> reportes)
    {
        var rows = reportes
            .Select(x => new object?[]
            {
                x.Id,
                x.Titulo,
                x.Periodo,
                x.GeneradoEl,
                x.CreatedAt.ToLocalTime(),
            })
            .ToList();

        BuildDataSheet(
            workbook,
            "Reportes",
            "Historial de reportes",
            new[] { "ID", "Titulo", "Periodo", "Generado", "Creado" },
            rows,
            new Dictionary<int, string>
            {
                [4] = "dd/MM/yyyy",
                [5] = "dd/MM/yyyy HH:mm",
            });
    }

    private static void BuildSolicitudesSheet(XLWorkbook workbook, IReadOnlyList<SolicitudCliente> solicitudes)
    {
        var rows = solicitudes
            .Select(x => new object?[]
            {
                x.Id,
                x.FechaHoraCita,
                x.FechaHoraCita.AddMinutes(Math.Max(1, x.DuracionMinutos)),
                x.DuracionMinutos,
                x.Estado,
                x.Prioridad,
                x.Canal,
                x.Cliente?.Nombre,
                x.Vehiculo?.Patente,
                x.Descripcion,
                x.Notas,
                x.CreatedAt.ToLocalTime(),
            })
            .ToList();

        BuildDataSheet(
            workbook,
            "Solicitudes",
            "Historial de solicitudes/citas de clientes",
            new[] { "ID", "Inicio", "Fin", "Duracion (min)", "Estado", "Prioridad", "Canal", "Cliente", "Patente", "Descripcion", "Notas", "Creado" },
            rows,
            new Dictionary<int, string>
            {
                [2] = "dd/MM/yyyy HH:mm",
                [3] = "dd/MM/yyyy HH:mm",
                [4] = "#,##0",
                [12] = "dd/MM/yyyy HH:mm",
            });
    }

    private static void BuildDistribuidorasSheet(
        XLWorkbook workbook,
        IReadOnlyList<Distribuidora> distribuidoras,
        IReadOnlyList<TrabajoDistribuidora> trabajos)
    {
        var gastoPorDistribuidora = trabajos
            .GroupBy(x => x.DistribuidoraId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Costo));

        var trabajosPorDistribuidora = trabajos
            .GroupBy(x => x.DistribuidoraId)
            .ToDictionary(x => x.Key, x => x.Count());

        var rows = distribuidoras
            .Select(x => new object?[]
            {
                x.Id,
                x.Nombre,
                x.Rubro,
                x.Telefono,
                x.Email,
                x.Estado,
                gastoPorDistribuidora.GetValueOrDefault(x.Id, 0m),
                trabajosPorDistribuidora.GetValueOrDefault(x.Id, 0),
                x.CreatedAt.ToLocalTime(),
            })
            .ToList();

        BuildDataSheet(
            workbook,
            "Distribuidoras",
            "Proveedores externos y gasto acumulado",
            new[] { "ID", "Nombre", "Rubro", "Telefono", "Email", "Estado", "Gasto total", "Trabajos", "Creado" },
            rows,
            new Dictionary<int, string>
            {
                [7] = "$ #,##0.00",
                [8] = "#,##0",
                [9] = "dd/MM/yyyy HH:mm",
            });
    }

    private static void BuildTrabajosDistribuidoraSheet(XLWorkbook workbook, IReadOnlyList<TrabajoDistribuidora> trabajos)
    {
        var rows = trabajos
            .Select(x => new object?[]
            {
                x.Id,
                x.Fecha,
                x.Distribuidora?.Nombre,
                x.Cliente?.Nombre,
                x.Vehiculo?.Patente,
                x.Descripcion,
                x.Costo,
                x.EstadoPago,
                x.Notas,
                x.CreatedAt.ToLocalTime(),
            })
            .ToList();

        BuildDataSheet(
            workbook,
            "Trabajos Dist.",
            "Historial de trabajos tercerizados",
            new[] { "ID", "Fecha", "Distribuidora", "Cliente", "Patente", "Descripcion", "Costo", "Estado pago", "Notas", "Creado" },
            rows,
            new Dictionary<int, string>
            {
                [2] = "dd/MM/yyyy",
                [7] = "$ #,##0.00",
                [10] = "dd/MM/yyyy HH:mm",
            });
    }

    private static void BuildDataSheet(
        XLWorkbook workbook,
        string sheetName,
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<object?[]> rows,
        IReadOnlyDictionary<int, string>? numberFormats = null)
    {
        var ws = ReplaceWorksheet(workbook, sheetName);
        var colCount = headers.Count;

        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, colCount).Merge();
        ws.Range(1, 1, 1, colCount).Style.Font.Bold = true;
        ws.Range(1, 1, 1, colCount).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, colCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF1FF");
        ws.Range(1, 1, 1, colCount).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Cell(2, 1).Value = "Generado";
        ws.Cell(2, 2).Value = DateTime.Now;
        ws.Cell(2, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

        const int headerRow = 4;
        for (var i = 0; i < colCount; i++)
        {
            ws.Cell(headerRow, i + 1).Value = headers[i];
        }

        StyleHeader(ws.Range(headerRow, 1, headerRow, colCount));

        var dataRow = headerRow + 1;
        foreach (var row in rows)
        {
            for (var col = 0; col < colCount; col++)
            {
                SetCellValue(ws.Cell(dataRow, col + 1), row[col]);
            }

            dataRow++;
        }

        var hasData = rows.Count > 0;
        if (hasData)
        {
            var range = ws.Range(headerRow, 1, dataRow - 1, colCount);
            var tableName = $"Tbl{new string(sheetName.Where(char.IsLetterOrDigit).ToArray())}";
            var table = range.CreateTable(tableName);
            table.Theme = XLTableTheme.TableStyleMedium2;
        }
        else
        {
            ws.Cell(dataRow, 1).Value = "Sin datos para exportar.";
            ws.Range(dataRow, 1, dataRow, colCount).Merge();
            ws.Cell(dataRow, 1).Style.Font.Italic = true;
            ws.Cell(dataRow, 1).Style.Font.FontColor = XLColor.FromHtml("#4A6075");
            dataRow++;
        }

        if (numberFormats is not null && hasData)
        {
            var startRow = headerRow + 1;
            var endRow = dataRow - 1;
            foreach (var format in numberFormats)
            {
                ws.Range(startRow, format.Key, endRow, format.Key).Style.NumberFormat.Format = format.Value;
            }
        }

        ws.Columns(1, colCount).AdjustToContents();
        ws.SheetView.FreezeRows(headerRow);
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Clear(XLClearOptions.Contents);
                return;
            case string text:
                cell.Value = text;
                return;
            case bool boolValue:
                cell.Value = boolValue;
                return;
            case int intValue:
                cell.Value = intValue;
                return;
            case long longValue:
                cell.Value = longValue;
                return;
            case short shortValue:
                cell.Value = shortValue;
                return;
            case decimal decimalValue:
                cell.Value = decimalValue;
                return;
            case double doubleValue:
                cell.Value = doubleValue;
                return;
            case float floatValue:
                cell.Value = floatValue;
                return;
            case DateTime dateTime:
                cell.Value = dateTime;
                return;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.DateTime;
                return;
            default:
                cell.Value = value.ToString();
                return;
        }
    }

    private static void StyleHeader(IXLRange headerRange)
    {
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F6FDE");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
}

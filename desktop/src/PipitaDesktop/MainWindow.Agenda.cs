using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using PipitaDesktop.Models;

namespace PipitaDesktop;

public partial class MainWindow
{
    private int? _editingCitaId;
    private DateTime _agendaMesActual = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _agendaFechaSeleccionada = DateTime.Today;
    private List<CitaGridRow> _allCitas = new();

    private string _agendaMesTitulo = string.Empty;
    private string _agendaResumenDia = "Sin citas para el dia seleccionado.";
    private string _agendaSemanaRango = string.Empty;

    public ObservableCollection<CitaGridRow> CitasDiaSeleccionado { get; } = new();
    public ObservableCollection<AgendaDayCell> AgendaDiasMes { get; } = new();
    public ObservableCollection<AgendaWeekSlotRow> AgendaSemanaSlots { get; } = new();

    public ObservableCollection<string> EstadoCitas { get; } = new(new[] { "pendiente", "confirmada", "en_proceso", "completada", "cancelada" });
    public ObservableCollection<string> EstadoCitasConTodos { get; } = new(new[] { "Todos", "pendiente", "confirmada", "en_proceso", "completada", "cancelada" });

    public string AgendaMesTitulo
    {
        get => _agendaMesTitulo;
        private set => SetField(ref _agendaMesTitulo, value);
    }

    public string AgendaResumenDia
    {
        get => _agendaResumenDia;
        private set => SetField(ref _agendaResumenDia, value);
    }

    public string AgendaSemanaRango
    {
        get => _agendaSemanaRango;
        private set => SetField(ref _agendaSemanaRango, value);
    }

    private async Task LoadAgendaAsync()
    {
        using var db = CreateDbContext();
        var citas = await db.Citas
            .AsNoTracking()
            .Include(x => x.Cliente)
            .Include(x => x.Vehiculo)
            .OrderBy(x => x.FechaHoraInicio)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync();

        _allCitas = citas
            .Select(
                cita =>
                {
                    var badge = BuildStatusBadge(cita.Estado);
                    return new CitaGridRow
                    {
                        Id = cita.Id,
                        ClienteId = cita.ClienteId,
                        VehiculoId = cita.VehiculoId,
                        ClienteNombre = cita.Cliente?.Nombre ?? "Sin cliente",
                        Patente = cita.Vehiculo?.Patente,
                        VehiculoNombre = cita.Vehiculo is null
                            ? "Sin vehiculo"
                            : $"{cita.Vehiculo.Marca} {cita.Vehiculo.Modelo}",
                        FechaHoraInicio = cita.FechaHoraInicio,
                        DuracionMinutos = cita.DuracionMinutos,
                        Estado = cita.Estado,
                        Motivo = cita.Motivo,
                        Notas = cita.Notas,
                        EstadoBadgeText = badge.Text,
                        EstadoBadgeBackground = badge.Background,
                        EstadoBadgeForeground = badge.Foreground,
                    };
                })
            .ToList();

        _agendaMesActual = new DateTime(_agendaFechaSeleccionada.Year, _agendaFechaSeleccionada.Month, 1);
        RefreshAgendaCalendar();
    }

    private void ApplyAgendaFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        var filtered = GetAgendaFilteredCitas();
        ApplyAgendaFilters(filtered);
    }

    private void ApplyAgendaFilters(List<CitaGridRow> filtered)
    {
        var selectedDate = _agendaFechaSeleccionada.Date;

        var dayItems = filtered
            .Where(x => x.FechaHoraInicio.Date == selectedDate)
            .OrderBy(x => x.FechaHoraInicio)
            .ToList();

        ReplaceCollection(CitasDiaSeleccionado, dayItems);

        var culture = CultureInfo.GetCultureInfo("es-AR");
        AgendaResumenDia = $"{selectedDate.ToString("dddd dd 'de' MMMM yyyy", culture)} - {dayItems.Count} cita(s)";

        RefreshAgendaWeekView(filtered);
    }

    private void RefreshAgendaCalendar()
    {
        var firstDayOfMonth = new DateTime(_agendaMesActual.Year, _agendaMesActual.Month, 1);
        _agendaMesActual = firstDayOfMonth;

        var filtered = GetAgendaFilteredCitas();
        var culture = CultureInfo.GetCultureInfo("es-AR");
        var title = firstDayOfMonth.ToString("MMMM yyyy", culture);
        AgendaMesTitulo = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title);

        var mondayBasedOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
        var gridStart = firstDayOfMonth.AddDays(-mondayBasedOffset);

        var citasPorDia = filtered
            .GroupBy(x => x.FechaHoraInicio.Date)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.FechaHoraInicio).ToList());

        var cells = new List<AgendaDayCell>(42);
        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i).Date;
            citasPorDia.TryGetValue(date, out var citasDia);
            citasDia ??= new List<CitaGridRow>();

            var hasCitas = citasDia.Count > 0;
            var dayBadge = hasCitas ? BuildStatusBadge(citasDia[0].Estado) : null;

            cells.Add(
                new AgendaDayCell
                {
                    Date = date,
                    DayNumber = date.Day,
                    IsCurrentMonth = date.Month == firstDayOfMonth.Month && date.Year == firstDayOfMonth.Year,
                    IsToday = date == DateTime.Today,
                    IsSelected = date == _agendaFechaSeleccionada.Date,
                    HasCitas = hasCitas,
                    DayNumberBackground = hasCitas ? dayBadge!.Background : CreateBrush("#FFF7FAFF"),
                    DayNumberForeground = hasCitas ? dayBadge!.Foreground : CreateBrush("#FF344861"),
                    TotalCitas = citasDia.Count,
                });
        }

        ReplaceCollection(AgendaDiasMes, cells);
        ApplyAgendaFilters(filtered);
    }

    private void RefreshAgendaWeekView(List<CitaGridRow> filtered)
    {
        var weekStart = GetWeekStartMonday(_agendaFechaSeleccionada);
        var weekEnd = weekStart.AddDays(6);
        AgendaSemanaRango = $"Semana {weekStart:dd/MM} - {weekEnd:dd/MM}";

        var citasPorDia = filtered
            .GroupBy(x => x.FechaHoraInicio.Date)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.FechaHoraInicio).ToList());

        var rows = new List<AgendaWeekSlotRow>();
        for (var hour = 7; hour <= 21; hour++)
        {
            rows.Add(
                new AgendaWeekSlotRow
                {
                    Hora = $"{hour:00}:00",
                    Lunes = BuildWeekCell(citasPorDia, weekStart, hour),
                    Martes = BuildWeekCell(citasPorDia, weekStart.AddDays(1), hour),
                    Miercoles = BuildWeekCell(citasPorDia, weekStart.AddDays(2), hour),
                    Jueves = BuildWeekCell(citasPorDia, weekStart.AddDays(3), hour),
                    Viernes = BuildWeekCell(citasPorDia, weekStart.AddDays(4), hour),
                    Sabado = BuildWeekCell(citasPorDia, weekStart.AddDays(5), hour),
                    Domingo = BuildWeekCell(citasPorDia, weekStart.AddDays(6), hour),
                });
        }

        ReplaceCollection(AgendaSemanaSlots, rows);
    }

    private static DateTime GetWeekStartMonday(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-offset);
    }

    private static AgendaWeekCell BuildWeekCell(Dictionary<DateTime, List<CitaGridRow>> citasPorDia, DateTime day, int hour)
    {
        if (!citasPorDia.TryGetValue(day.Date, out var dayCitas))
        {
            return AgendaWeekCell.Empty;
        }

        var slotStart = day.Date.AddHours(hour);
        var slotEnd = slotStart.AddHours(1);

        var inSlot = dayCitas
            .Where(cita => cita.FechaHoraInicio < slotEnd && cita.FechaHoraFin > slotStart)
            .OrderBy(cita => cita.FechaHoraInicio)
            .ToList();

        if (inSlot.Count == 0)
        {
            return AgendaWeekCell.Empty;
        }

        var first = inSlot[0];
        var badge = BuildStatusBadge(first.Estado);
        var shortMotivo = Shorten(first.Motivo, 12);
        var text = inSlot.Count == 1
            ? $"{first.FechaHoraInicio:HH:mm} {shortMotivo}"
            : $"{first.FechaHoraInicio:HH:mm} {shortMotivo} +{inSlot.Count - 1}";

        return new AgendaWeekCell
        {
            Text = text,
            Background = badge.Background,
            Foreground = badge.Foreground,
        };
    }

    private List<CitaGridRow> GetAgendaFilteredCitas()
    {
        var estado = AgendaFiltroEstadoCombo.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(estado) || estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            return _allCitas.ToList();
        }

        return _allCitas
            .Where(x => string.Equals(x.Estado, estado, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static AgendaBadge BuildStatusBadge(string? estado)
    {
        var key = estado?.Trim().ToLowerInvariant();
        return key switch
        {
            "pendiente" => new AgendaBadge
            {
                Text = "Pendiente",
                Background = CreateBrush("#FFFDF0C4"),
                Foreground = CreateBrush("#FF7A4F00"),
            },
            "confirmada" => new AgendaBadge
            {
                Text = "Confirmada",
                Background = CreateBrush("#FFD9F1FF"),
                Foreground = CreateBrush("#FF0B5276"),
            },
            "en_proceso" => new AgendaBadge
            {
                Text = "En proceso",
                Background = CreateBrush("#FFFEDFCF"),
                Foreground = CreateBrush("#FF8A3F00"),
            },
            "completada" => new AgendaBadge
            {
                Text = "Completada",
                Background = CreateBrush("#FFDDF7E5"),
                Foreground = CreateBrush("#FF166B2F"),
            },
            "cancelada" => new AgendaBadge
            {
                Text = "Cancelada",
                Background = CreateBrush("#FFF1D9DC"),
                Foreground = CreateBrush("#FF8E2430"),
            },
            _ => new AgendaBadge
            {
                Text = "Sin estado",
                Background = CreateBrush("#FFE9EDF3"),
                Foreground = CreateBrush("#FF37485D"),
            },
        };
    }

    private static SolidColorBrush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }

    private static string Shorten(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Length <= max ? text : $"{text[..max]}...";
    }

    private async void GuardarCitaButton_Click(object sender, RoutedEventArgs e)
    {
        var motivo = CitaMotivoInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            MessageBox.Show("El motivo es obligatorio.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseHora(CitaHoraInput.Text, out var hora))
        {
            MessageBox.Show("La hora debe estar en formato HH:mm.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseNonNegativeInt(CitaDuracionInput.Text, out var duracion) || duracion <= 0)
        {
            MessageBox.Show("La duracion debe ser un numero mayor a cero.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fecha = (CitaFechaInput.SelectedDate ?? _agendaFechaSeleccionada).Date;
        var inicio = fecha.Add(hora);
        var fin = inicio.AddMinutes(duracion);

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

            var dayStart = inicio.Date;
            var dayEnd = dayStart.AddDays(1);
            var currentId = _editingCitaId ?? -1;

            var citasMismoDia = await db.Citas
                .AsNoTracking()
                .Where(x => x.Id != currentId && x.FechaHoraInicio >= dayStart && x.FechaHoraInicio < dayEnd)
                .OrderBy(x => x.FechaHoraInicio)
                .ToListAsync();

            var exacta = citasMismoDia.FirstOrDefault(x => x.FechaHoraInicio == inicio);
            if (exacta is not null)
            {
                MessageBox.Show(
                    $"No se puede cargar otra cita el mismo dia a la misma hora ({inicio:dd/MM HH:mm}).",
                    "Conflicto de horario",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var solapada = citasMismoDia
                .Select(x => new
                {
                    Cita = x,
                    Inicio = x.FechaHoraInicio,
                    Fin = x.FechaHoraInicio.AddMinutes(Math.Max(1, x.DuracionMinutos)),
                })
                .FirstOrDefault(x => inicio < x.Fin && fin > x.Inicio);

            if (solapada is not null)
            {
                var texto = string.Equals(solapada.Cita.Estado, "en_proceso", StringComparison.OrdinalIgnoreCase)
                    ? $"Tenes en proceso una actividad ese dia de {solapada.Inicio:HH:mm} a {solapada.Fin:HH:mm} y esta cita entra en ese horario. Desea aplicar la cita?"
                    : $"Ya hay una cita ese dia de {solapada.Inicio:HH:mm} a {solapada.Fin:HH:mm} y esta cita entra en ese horario. Desea aplicar la cita?";

                var confirm = MessageBox.Show(
                    texto,
                    "Conflicto de horario",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            Cita cita;
            if (_editingCitaId.HasValue)
            {
                cita = await db.Citas.FirstOrDefaultAsync(x => x.Id == _editingCitaId.Value)
                    ?? throw new InvalidOperationException("Cita no encontrada.");
            }
            else
            {
                cita = new Cita { CreatedAt = DateTime.UtcNow };
                await db.Citas.AddAsync(cita);
            }

            cita.ClienteId = ParseNullableInt(CitaClienteCombo.SelectedValue);
            cita.VehiculoId = ParseNullableInt(CitaVehiculoCombo.SelectedValue);
            cita.FechaHoraInicio = inicio;
            cita.DuracionMinutos = duracion;
            cita.Estado = CitaEstadoCombo.SelectedItem as string ?? "pendiente";
            cita.Motivo = motivo;
            cita.Notas = ToNullable(CitaNotasInput.Text);
            cita.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            _agendaFechaSeleccionada = inicio.Date;
            _agendaMesActual = new DateTime(inicio.Year, inicio.Month, 1);
            ClearCitaForm();
            await RefreshAllAsync("Cita guardada.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar la cita.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void EliminarCitaButton_Click(object sender, RoutedEventArgs e)
    {
        if (CitasDiaGrid.SelectedItem is not CitaGridRow selected)
        {
            MessageBox.Show("Selecciona una cita para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Se eliminara la cita seleccionada. Queres continuar?",
            "Confirmar eliminacion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();
            var cita = await db.Citas.FirstOrDefaultAsync(x => x.Id == selected.Id);
            if (cita is null)
            {
                MessageBox.Show("La cita ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
                return;
            }

            db.Citas.Remove(cita);
            await db.SaveChangesAsync();

            ClearCitaForm();
            await RefreshAllAsync("Cita eliminada.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar la cita.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NuevoCitaButton_Click(object sender, RoutedEventArgs e)
    {
        ClearCitaForm();
    }

    private void CitasDiaGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CitasDiaGrid.SelectedItem is not CitaGridRow selected)
        {
            return;
        }

        _editingCitaId = selected.Id;
        CitaClienteCombo.SelectedValue = selected.ClienteId;
        CitaVehiculoCombo.SelectedValue = selected.VehiculoId;
        CitaFechaInput.SelectedDate = selected.FechaHoraInicio.Date;
        CitaHoraInput.Text = selected.FechaHoraInicio.ToString("HH:mm");
        CitaDuracionInput.Text = selected.DuracionMinutos.ToString(CultureInfo.InvariantCulture);
        CitaEstadoCombo.SelectedItem = EstadoCitas.Contains(selected.Estado) ? selected.Estado : "pendiente";
        CitaMotivoInput.Text = selected.Motivo;
        CitaNotasInput.Text = selected.Notas ?? string.Empty;

        StatusMessage = $"Editando cita: {selected.Motivo}";
    }

    private void AgendaMesAnteriorButton_Click(object sender, RoutedEventArgs e)
    {
        _agendaMesActual = _agendaMesActual.AddMonths(-1);
        RefreshAgendaCalendar();
    }

    private void AgendaMesSiguienteButton_Click(object sender, RoutedEventArgs e)
    {
        _agendaMesActual = _agendaMesActual.AddMonths(1);
        RefreshAgendaCalendar();
    }

    private void AgendaHoyButton_Click(object sender, RoutedEventArgs e)
    {
        _agendaFechaSeleccionada = DateTime.Today;
        _agendaMesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        CitaFechaInput.SelectedDate = _agendaFechaSeleccionada;
        RefreshAgendaCalendar();
    }

    private void AgendaDiaButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not AgendaDayCell day)
        {
            return;
        }

        _agendaFechaSeleccionada = day.Date;
        _agendaMesActual = new DateTime(day.Date.Year, day.Date.Month, 1);
        CitaFechaInput.SelectedDate = day.Date;
        RefreshAgendaCalendar();
    }

    private void AgendaFiltroEstadoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshAgendaCalendar();
    }

    private void CitaFechaInput_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || !CitaFechaInput.SelectedDate.HasValue)
        {
            return;
        }

        var date = CitaFechaInput.SelectedDate.Value.Date;
        _agendaFechaSeleccionada = date;
        _agendaMesActual = new DateTime(date.Year, date.Month, 1);
        RefreshAgendaCalendar();
    }

    private void ClearCitaForm()
    {
        _editingCitaId = null;
        CitaClienteCombo.SelectedValue = null;
        CitaVehiculoCombo.SelectedValue = null;
        CitaFechaInput.SelectedDate = _agendaFechaSeleccionada;
        CitaHoraInput.Text = "09:00";
        CitaDuracionInput.Text = "60";
        CitaEstadoCombo.SelectedItem = "pendiente";
        CitaMotivoInput.Text = string.Empty;
        CitaNotasInput.Text = string.Empty;
        CitasDiaGrid.SelectedItem = null;
    }

    private static bool TryParseHora(string rawValue, out TimeSpan time)
    {
        var value = rawValue.Trim();
        if (TimeSpan.TryParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture, out var parsed) ||
            TimeSpan.TryParseExact(value, "h\\:mm", CultureInfo.InvariantCulture, out parsed))
        {
            time = parsed;
            return true;
        }

        if (DateTime.TryParseExact(value, new[] { "HH:mm", "H:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var asDate))
        {
            time = asDate.TimeOfDay;
            return true;
        }

        time = default;
        return false;
    }
}

public sealed class CitaGridRow
{
    public int Id { get; init; }
    public int? ClienteId { get; init; }
    public int? VehiculoId { get; init; }
    public string ClienteNombre { get; init; } = "Sin cliente";
    public string? Patente { get; init; }
    public string VehiculoNombre { get; init; } = "Sin vehiculo";
    public DateTime FechaHoraInicio { get; init; }
    public int DuracionMinutos { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Motivo { get; init; } = string.Empty;
    public string? Notas { get; init; }
    public string EstadoBadgeText { get; init; } = string.Empty;
    public Brush EstadoBadgeBackground { get; init; } = Brushes.Transparent;
    public Brush EstadoBadgeForeground { get; init; } = Brushes.Black;
    public DateTime FechaHoraFin => FechaHoraInicio.AddMinutes(Math.Max(1, DuracionMinutos));
}

public sealed class AgendaDayCell
{
    public DateTime Date { get; init; }
    public int DayNumber { get; init; }
    public bool IsCurrentMonth { get; init; }
    public bool IsToday { get; init; }
    public bool IsSelected { get; init; }
    public bool HasCitas { get; init; }
    public Brush DayNumberBackground { get; init; } = Brushes.Transparent;
    public Brush DayNumberForeground { get; init; } = Brushes.Black;
    public int TotalCitas { get; init; }
}

public sealed class AgendaBadge
{
    public static AgendaBadge Empty { get; } = new();

    public string Text { get; init; } = string.Empty;
    public Brush Background { get; init; } = Brushes.Transparent;
    public Brush Foreground { get; init; } = Brushes.Transparent;
    public bool HasContent => !string.IsNullOrWhiteSpace(Text);
}

public sealed class AgendaWeekSlotRow
{
    public string Hora { get; init; } = string.Empty;
    public AgendaWeekCell Lunes { get; init; } = AgendaWeekCell.Empty;
    public AgendaWeekCell Martes { get; init; } = AgendaWeekCell.Empty;
    public AgendaWeekCell Miercoles { get; init; } = AgendaWeekCell.Empty;
    public AgendaWeekCell Jueves { get; init; } = AgendaWeekCell.Empty;
    public AgendaWeekCell Viernes { get; init; } = AgendaWeekCell.Empty;
    public AgendaWeekCell Sabado { get; init; } = AgendaWeekCell.Empty;
    public AgendaWeekCell Domingo { get; init; } = AgendaWeekCell.Empty;
}

public sealed class AgendaWeekCell
{
    public static AgendaWeekCell Empty { get; } = new();

    public string Text { get; init; } = string.Empty;
    public Brush Background { get; init; } = Brushes.Transparent;
    public Brush Foreground { get; init; } = Brushes.Transparent;
}




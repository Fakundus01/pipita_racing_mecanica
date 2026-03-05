using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PipitaDesktop;

public partial class MainWindow
{
    private DateTime _agendaMesActual = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _agendaFechaSeleccionada = DateTime.Today;
    private List<CitaGridRow> _allCitas = new();

    private string _agendaMesTitulo = string.Empty;
    private string _agendaResumenDia = "Sin citas para el dia seleccionado.";
    private string _agendaSemanaRango = string.Empty;
    private string _agendaDiaTotalCitas = "0";
    private string _agendaDiaPrimerTurno = "--";
    private string _agendaDiaHorasAsignadas = "0 h";
    private string _agendaSemanaTotalCitas = "0";
    private string _agendaMesTotalCitas = "0";
    private string _agendaMesDiasConCitas = "0";
    private string _agendaMesDiaMasCargado = "--";

    public ObservableCollection<CitaGridRow> CitasDiaSeleccionado { get; } = new();
    public ObservableCollection<AgendaDayCell> AgendaDiasMes { get; } = new();
    public ObservableCollection<AgendaWeekSlotRow> AgendaSemanaSlots { get; } = new();

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

    public string AgendaDiaTotalCitas
    {
        get => _agendaDiaTotalCitas;
        private set => SetField(ref _agendaDiaTotalCitas, value);
    }

    public string AgendaDiaPrimerTurno
    {
        get => _agendaDiaPrimerTurno;
        private set => SetField(ref _agendaDiaPrimerTurno, value);
    }

    public string AgendaDiaHorasAsignadas
    {
        get => _agendaDiaHorasAsignadas;
        private set => SetField(ref _agendaDiaHorasAsignadas, value);
    }

    public string AgendaSemanaTotalCitas
    {
        get => _agendaSemanaTotalCitas;
        private set => SetField(ref _agendaSemanaTotalCitas, value);
    }

    public string AgendaMesTotalCitas
    {
        get => _agendaMesTotalCitas;
        private set => SetField(ref _agendaMesTotalCitas, value);
    }

    public string AgendaMesDiasConCitas
    {
        get => _agendaMesDiasConCitas;
        private set => SetField(ref _agendaMesDiasConCitas, value);
    }

    public string AgendaMesDiaMasCargado
    {
        get => _agendaMesDiaMasCargado;
        private set => SetField(ref _agendaMesDiaMasCargado, value);
    }

    private Task LoadAgendaAsync()
    {
        _allCitas = _allSolicitudes
            .OrderBy(x => x.FechaHoraCita)
            .ThenBy(x => x.Id)
            .Select(
                solicitud =>
                {
                    var badge = BuildStatusBadge(solicitud.Estado);
                    return new CitaGridRow
                    {
                        Id = solicitud.Id,
                        ClienteId = solicitud.ClienteId,
                        VehiculoId = solicitud.VehiculoId,
                        ClienteNombre = solicitud.ClienteNombre,
                        Patente = solicitud.Patente,
                        VehiculoNombre = solicitud.VehiculoNombre,
                        FechaHoraInicio = solicitud.FechaHoraCita,
                        DuracionMinutos = solicitud.DuracionMinutos,
                        Estado = solicitud.Estado,
                        Motivo = solicitud.Descripcion,
                        Notas = solicitud.Notas,
                        EstadoBadgeText = badge.Text,
                        EstadoBadgeBackground = badge.Background,
                        EstadoBadgeForeground = badge.Foreground,
                    };
                })
            .ToList();

        _agendaMesActual = new DateTime(_agendaFechaSeleccionada.Year, _agendaFechaSeleccionada.Month, 1);
        RefreshAgendaCalendar();
        return Task.CompletedTask;
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
        AgendaDiaTotalCitas = dayItems.Count.ToString(CultureInfo.InvariantCulture);
        AgendaDiaPrimerTurno = dayItems.Count == 0
            ? "--"
            : dayItems.Min(x => x.FechaHoraInicio).ToString("HH:mm", CultureInfo.InvariantCulture);
        AgendaDiaHorasAsignadas = $"{dayItems.Sum(x => x.DuracionMinutos) / 60.0:0.#} h";

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

        var sundayBasedOffset = (int)firstDayOfMonth.DayOfWeek;
        var gridStart = firstDayOfMonth.AddDays(-sundayBasedOffset);

        var citasPorDia = filtered
            .GroupBy(x => x.FechaHoraInicio.Date)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.FechaHoraInicio).ToList());

        var citasDelMes = filtered
            .Where(x => x.FechaHoraInicio.Year == firstDayOfMonth.Year && x.FechaHoraInicio.Month == firstDayOfMonth.Month)
            .ToList();
        AgendaMesTotalCitas = citasDelMes.Count.ToString(CultureInfo.InvariantCulture);
        var diasConCitas = citasDelMes
            .Select(x => x.FechaHoraInicio.Date)
            .Distinct()
            .Count();
        AgendaMesDiasConCitas = diasConCitas.ToString(CultureInfo.InvariantCulture);
        var diaMasCargado = citasDelMes
            .GroupBy(x => x.FechaHoraInicio.Date)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key)
            .FirstOrDefault();
        AgendaMesDiaMasCargado = diaMasCargado is null
            ? "--"
            : $"{diaMasCargado.Key:dd/MM} ({diaMasCargado.Count()})";

        var cells = new List<AgendaDayCell>(42);
        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i).Date;
            citasPorDia.TryGetValue(date, out var citasDia);
            citasDia ??= new List<CitaGridRow>();

            var hasCitas = citasDia.Count > 0;
            var firstCita = hasCitas ? citasDia[0] : null;
            var dayBadge = hasCitas ? BuildStatusBadge(firstCita!.Estado) : null;

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
                    PreviewPrimary = firstCita is null ? string.Empty : $"{firstCita.FechaHoraInicio:HH:mm} {Shorten(firstCita.ClienteNombre, 12)}",
                    PreviewSecondary = citasDia.Count > 1
                        ? $"+{citasDia.Count - 1} mas"
                        : dayBadge?.Text ?? string.Empty,
                    PreviewBadgeBackground = hasCitas ? dayBadge!.Background : Brushes.Transparent,
                    PreviewBadgeForeground = hasCitas ? dayBadge!.Foreground : Brushes.Transparent,
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
        AgendaSemanaTotalCitas = filtered.Count(x => x.FechaHoraInicio.Date >= weekStart && x.FechaHoraInicio.Date <= weekEnd).ToString(CultureInfo.InvariantCulture);

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
        RefreshAgendaCalendar();
    }

    private void AgendaFiltroEstadoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshAgendaCalendar();
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
    public string PreviewPrimary { get; init; } = string.Empty;
    public string PreviewSecondary { get; init; } = string.Empty;
    public Brush PreviewBadgeBackground { get; init; } = Brushes.Transparent;
    public Brush PreviewBadgeForeground { get; init; } = Brushes.Black;
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

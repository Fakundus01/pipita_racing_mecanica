using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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

    public ObservableCollection<CitaGridRow> CitasDiaSeleccionado { get; } = new();
    public ObservableCollection<AgendaDayCell> AgendaDiasMes { get; } = new();

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
                cita => new CitaGridRow
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

        var estado = AgendaFiltroEstadoCombo.SelectedItem as string;
        var selectedDate = _agendaFechaSeleccionada.Date;

        var query = _allCitas
            .Where(x => x.FechaHoraInicio.Date == selectedDate)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => string.Equals(x.Estado, estado, StringComparison.OrdinalIgnoreCase));
        }

        var dayItems = query.OrderBy(x => x.FechaHoraInicio).ToList();
        ReplaceCollection(CitasDiaSeleccionado, dayItems);

        var culture = CultureInfo.GetCultureInfo("es-AR");
        AgendaResumenDia = $"{selectedDate.ToString("dddd dd 'de' MMMM yyyy", culture)} - {dayItems.Count} cita(s)";
    }

    private void RefreshAgendaCalendar()
    {
        var firstDayOfMonth = new DateTime(_agendaMesActual.Year, _agendaMesActual.Month, 1);
        _agendaMesActual = firstDayOfMonth;

        var culture = CultureInfo.GetCultureInfo("es-AR");
        var title = firstDayOfMonth.ToString("MMMM yyyy", culture);
        AgendaMesTitulo = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title);

        var mondayBasedOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
        var gridStart = firstDayOfMonth.AddDays(-mondayBasedOffset);

        var citasPorDia = _allCitas
            .GroupBy(x => x.FechaHoraInicio.Date)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.FechaHoraInicio).ToList());

        var cells = new List<AgendaDayCell>(42);
        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i).Date;
            citasPorDia.TryGetValue(date, out var citasDia);
            citasDia ??= new List<CitaGridRow>();

            var preview1 = citasDia.Count > 0 ? BuildAgendaPreview(citasDia[0]) : null;
            var preview2 = citasDia.Count > 1 ? BuildAgendaPreview(citasDia[1]) : null;
            var extra = citasDia.Count > 2 ? $"+{citasDia.Count - 2} mas" : null;

            cells.Add(
                new AgendaDayCell
                {
                    Date = date,
                    DayNumber = date.Day,
                    IsCurrentMonth = date.Month == firstDayOfMonth.Month && date.Year == firstDayOfMonth.Year,
                    IsToday = date == DateTime.Today,
                    IsSelected = date == _agendaFechaSeleccionada.Date,
                    Preview1 = preview1,
                    Preview2 = preview2,
                    ExtraLabel = extra,
                    TotalCitas = citasDia.Count,
                });
        }

        ReplaceCollection(AgendaDiasMes, cells);
        ApplyAgendaFilters();
    }

    private static string BuildAgendaPreview(CitaGridRow cita)
    {
        var raw = $"{cita.FechaHoraInicio:HH:mm} {cita.Motivo}";
        return raw.Length <= 22 ? raw : $"{raw[..22]}...";
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

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

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
        ApplyAgendaFilters();
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
    public DateTime FechaHoraFin => FechaHoraInicio.AddMinutes(DuracionMinutos);
}

public sealed class AgendaDayCell
{
    public DateTime Date { get; init; }
    public int DayNumber { get; init; }
    public bool IsCurrentMonth { get; init; }
    public bool IsToday { get; init; }
    public bool IsSelected { get; init; }
    public string? Preview1 { get; init; }
    public string? Preview2 { get; init; }
    public string? ExtraLabel { get; init; }
    public int TotalCitas { get; init; }
}

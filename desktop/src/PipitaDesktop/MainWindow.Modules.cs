using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PipitaDesktop.Models;

namespace PipitaDesktop;

public partial class MainWindow
{
    private int? _editingSolicitudId;
    private int? _editingDistribuidoraId;
    private int? _editingTrabajoDistribuidoraId;

    private List<SolicitudGridRow> _allSolicitudes = new();
    private List<DistribuidoraGridRow> _allDistribuidoras = new();
    private List<TrabajoDistribuidoraGridRow> _allTrabajosDistribuidora = new();

    private string _resumenGastoDistribuidoras = "$0.00";

    public ObservableCollection<SolicitudGridRow> Solicitudes { get; } = new();
    public ObservableCollection<DistribuidoraGridRow> Distribuidoras { get; } = new();
    public ObservableCollection<TrabajoDistribuidoraGridRow> TrabajosDistribuidora { get; } = new();

    public ObservableCollection<string> EstadoSolicitudes { get; } = new(new[] { "pendiente", "en_proceso", "completada", "cancelada" });
    public ObservableCollection<string> EstadoSolicitudesConTodos { get; } = new(new[] { "Todos", "pendiente", "en_proceso", "completada", "cancelada" });
    public ObservableCollection<string> PrioridadSolicitudes { get; } = new(new[] { "baja", "media", "alta" });
    public ObservableCollection<string> CanalSolicitudes { get; } = new(new[] { "telefono", "whatsapp", "presencial", "web" });

    public ObservableCollection<string> EstadoDistribuidoras { get; } = new(new[] { "activa", "inactiva" });
    public ObservableCollection<string> EstadoDistribuidorasConTodos { get; } = new(new[] { "Todos", "activa", "inactiva" });
    public ObservableCollection<string> EstadoPagoTrabajoDistribuidora { get; } = new(new[] { "pagado", "pendiente" });

    public ObservableCollection<DistribuidoraLookupItem> DistribuidoraOptions { get; } = new();
    public ObservableCollection<DistribuidoraLookupItem> DistribuidoraFilterOptions { get; } = new();

    public string ResumenGastoDistribuidoras
    {
        get => _resumenGastoDistribuidoras;
        private set => SetField(ref _resumenGastoDistribuidoras, value);
    }

    private async Task LoadSolicitudesAsync()
    {
        using var db = CreateDbContext();
        var solicitudes = await db.SolicitudesCliente
            .AsNoTracking()
            .Include(x => x.Cliente)
            .Include(x => x.Vehiculo)
            .OrderByDescending(x => x.FechaSolicitud)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        _allSolicitudes = solicitudes
            .Select(
                solicitud => new SolicitudGridRow
                {
                    Id = solicitud.Id,
                    ClienteId = solicitud.ClienteId,
                    VehiculoId = solicitud.VehiculoId,
                    ClienteNombre = solicitud.Cliente?.Nombre ?? "Sin cliente",
                    Patente = solicitud.Vehiculo?.Patente,
                    VehiculoNombre = solicitud.Vehiculo is null
                        ? "Sin vehiculo"
                        : $"{solicitud.Vehiculo.Marca} {solicitud.Vehiculo.Modelo}",
                    Descripcion = solicitud.Descripcion,
                    FechaSolicitud = solicitud.FechaSolicitud,
                    Estado = solicitud.Estado,
                    Prioridad = solicitud.Prioridad,
                    Canal = solicitud.Canal,
                    Notas = solicitud.Notas,
                })
            .ToList();

        ApplySolicitudesFilters();
    }

    private async Task LoadDistribuidorasModuleAsync()
    {
        using var db = CreateDbContext();
        var distribuidoras = await db.Distribuidoras
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        var trabajos = await db.TrabajosDistribuidora
            .AsNoTracking()
            .Include(x => x.Distribuidora)
            .Include(x => x.Cliente)
            .Include(x => x.Vehiculo)
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        _allTrabajosDistribuidora = trabajos
            .Select(
                trabajo => new TrabajoDistribuidoraGridRow
                {
                    Id = trabajo.Id,
                    DistribuidoraId = trabajo.DistribuidoraId,
                    ClienteId = trabajo.ClienteId,
                    VehiculoId = trabajo.VehiculoId,
                    DistribuidoraNombre = trabajo.Distribuidora?.Nombre ?? "Sin distribuidora",
                    ClienteNombre = trabajo.Cliente?.Nombre ?? "Sin cliente",
                    Patente = trabajo.Vehiculo?.Patente,
                    VehiculoNombre = trabajo.Vehiculo is null
                        ? "Sin vehiculo"
                        : $"{trabajo.Vehiculo.Marca} {trabajo.Vehiculo.Modelo}",
                    Descripcion = trabajo.Descripcion,
                    Fecha = trabajo.Fecha,
                    Costo = trabajo.Costo,
                    EstadoPago = trabajo.EstadoPago,
                    Notas = trabajo.Notas,
                })
            .ToList();

        var gastoPorDistribuidora = _allTrabajosDistribuidora
            .GroupBy(x => x.DistribuidoraId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Costo));

        var cantidadPorDistribuidora = _allTrabajosDistribuidora
            .GroupBy(x => x.DistribuidoraId)
            .ToDictionary(x => x.Key, x => x.Count());

        _allDistribuidoras = distribuidoras
            .Select(
                distribuidora => new DistribuidoraGridRow
                {
                    Id = distribuidora.Id,
                    Nombre = distribuidora.Nombre,
                    Rubro = distribuidora.Rubro,
                    Telefono = distribuidora.Telefono,
                    Email = distribuidora.Email,
                    Estado = distribuidora.Estado,
                    Notas = distribuidora.Notas,
                    GastoTotal = gastoPorDistribuidora.GetValueOrDefault(distribuidora.Id, 0m),
                    CantidadTrabajos = cantidadPorDistribuidora.GetValueOrDefault(distribuidora.Id, 0),
                })
            .ToList();

        DistribuidoraOptions.Clear();
        DistribuidoraOptions.Add(new DistribuidoraLookupItem { Id = null, Display = "Seleccionar distribuidora" });

        DistribuidoraFilterOptions.Clear();
        DistribuidoraFilterOptions.Add(new DistribuidoraLookupItem { Id = null, Display = "Todas las distribuidoras" });

        foreach (var distribuidora in distribuidoras)
        {
            var option = new DistribuidoraLookupItem { Id = distribuidora.Id, Display = distribuidora.Nombre };
            DistribuidoraOptions.Add(option);
            DistribuidoraFilterOptions.Add(option);
        }

        ResumenGastoDistribuidoras = _allTrabajosDistribuidora.Sum(x => x.Costo).ToString("C2");

        ApplyDistribuidorasFilters();
        ApplyTrabajosDistribuidoraFilters();
    }

    private void ApplySolicitudesFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        var query = _allSolicitudes.AsEnumerable();
        var text = ToNullable(SolicitudesFiltroTextoInput.Text);
        var estado = SolicitudesFiltroEstadoCombo.SelectedItem as string;

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(
                x => ContainsIgnoreCase(x.ClienteNombre, text)
                    || ContainsIgnoreCase(x.Patente, text)
                    || ContainsIgnoreCase(x.Descripcion, text)
                    || ContainsIgnoreCase(x.Canal, text));
        }

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => string.Equals(x.Estado, estado, StringComparison.OrdinalIgnoreCase));
        }

        ReplaceCollection(Solicitudes, query.OrderByDescending(x => x.FechaSolicitud).ThenByDescending(x => x.Id));
    }

    private void ApplyDistribuidorasFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        var query = _allDistribuidoras.AsEnumerable();
        var text = ToNullable(DistribuidorasFiltroTextoInput.Text);
        var estado = DistribuidorasFiltroEstadoCombo.SelectedItem as string;

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(x => ContainsIgnoreCase(x.Nombre, text) || ContainsIgnoreCase(x.Rubro, text));
        }

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => string.Equals(x.Estado, estado, StringComparison.OrdinalIgnoreCase));
        }

        ReplaceCollection(Distribuidoras, query.OrderBy(x => x.Nombre));
    }

    private void ApplyTrabajosDistribuidoraFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        var query = _allTrabajosDistribuidora.AsEnumerable();
        var text = ToNullable(TrabajosDistribuidoraFiltroTextoInput.Text);
        var distribuidoraId = ParseNullableInt(TrabajosDistribuidoraFiltroDistribuidoraCombo.SelectedValue);

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(
                x => ContainsIgnoreCase(x.DistribuidoraNombre, text)
                    || ContainsIgnoreCase(x.ClienteNombre, text)
                    || ContainsIgnoreCase(x.Patente, text)
                    || ContainsIgnoreCase(x.Descripcion, text));
        }

        if (distribuidoraId.HasValue)
        {
            query = query.Where(x => x.DistribuidoraId == distribuidoraId.Value);
        }

        ReplaceCollection(TrabajosDistribuidora, query.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id));
    }

    private void LimpiarFiltroSolicitudesButton_Click(object sender, RoutedEventArgs e)
    {
        SolicitudesFiltroTextoInput.Text = string.Empty;
        SolicitudesFiltroEstadoCombo.SelectedItem = "Todos";
        ApplySolicitudesFilters();
    }

    private void LimpiarFiltroDistribuidorasButton_Click(object sender, RoutedEventArgs e)
    {
        DistribuidorasFiltroTextoInput.Text = string.Empty;
        DistribuidorasFiltroEstadoCombo.SelectedItem = "Todos";
        ApplyDistribuidorasFilters();
    }

    private void LimpiarFiltroTrabajosDistribuidoraButton_Click(object sender, RoutedEventArgs e)
    {
        TrabajosDistribuidoraFiltroTextoInput.Text = string.Empty;
        TrabajosDistribuidoraFiltroDistribuidoraCombo.SelectedValue = null;
        ApplyTrabajosDistribuidoraFilters();
    }

    private async void GuardarSolicitudButton_Click(object sender, RoutedEventArgs e)
    {
        var descripcion = SolicitudDescripcionInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            MessageBox.Show("La descripcion es obligatoria.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

            SolicitudCliente solicitud;
            if (_editingSolicitudId.HasValue)
            {
                solicitud = await db.SolicitudesCliente.FirstOrDefaultAsync(x => x.Id == _editingSolicitudId.Value)
                    ?? throw new InvalidOperationException("Request no encontrado.");
            }
            else
            {
                solicitud = new SolicitudCliente { CreatedAt = DateTime.UtcNow };
                await db.SolicitudesCliente.AddAsync(solicitud);
            }

            solicitud.ClienteId = ParseNullableInt(SolicitudClienteCombo.SelectedValue);
            solicitud.VehiculoId = ParseNullableInt(SolicitudVehiculoCombo.SelectedValue);
            solicitud.Descripcion = descripcion;
            solicitud.FechaSolicitud = (SolicitudFechaInput.SelectedDate ?? DateTime.Today).Date;
            solicitud.Estado = SolicitudEstadoCombo.SelectedItem as string ?? "pendiente";
            solicitud.Prioridad = SolicitudPrioridadCombo.SelectedItem as string ?? "media";
            solicitud.Canal = ToNullable(SolicitudCanalCombo.SelectedItem as string);
            solicitud.Notas = ToNullable(SolicitudNotasInput.Text);
            solicitud.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            ClearSolicitudForm();
            await RefreshAllAsync("Request de cliente guardado.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar el request de cliente.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void EliminarSolicitudButton_Click(object sender, RoutedEventArgs e)
    {
        if (SolicitudesGrid.SelectedItem is not SolicitudGridRow selected)
        {
            MessageBox.Show("Selecciona un request para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Se eliminara el request seleccionado. Queres continuar?",
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
            var solicitud = await db.SolicitudesCliente.FirstOrDefaultAsync(x => x.Id == selected.Id);
            if (solicitud is null)
            {
                MessageBox.Show("El request ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
                return;
            }

            db.SolicitudesCliente.Remove(solicitud);
            await db.SaveChangesAsync();

            ClearSolicitudForm();
            await RefreshAllAsync("Request eliminado.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar el request.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NuevoSolicitudButton_Click(object sender, RoutedEventArgs e)
    {
        ClearSolicitudForm();
    }

    private void SolicitudesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SolicitudesGrid.SelectedItem is not SolicitudGridRow selected)
        {
            return;
        }

        _editingSolicitudId = selected.Id;
        SolicitudClienteCombo.SelectedValue = selected.ClienteId;
        SolicitudVehiculoCombo.SelectedValue = selected.VehiculoId;
        SolicitudFechaInput.SelectedDate = selected.FechaSolicitud;
        SolicitudEstadoCombo.SelectedItem = EstadoSolicitudes.Contains(selected.Estado) ? selected.Estado : "pendiente";
        SolicitudPrioridadCombo.SelectedItem = PrioridadSolicitudes.Contains(selected.Prioridad) ? selected.Prioridad : "media";
        SolicitudCanalCombo.SelectedItem = CanalSolicitudes.Contains(selected.Canal ?? string.Empty) ? selected.Canal : null;
        SolicitudDescripcionInput.Text = selected.Descripcion;
        SolicitudNotasInput.Text = selected.Notas ?? string.Empty;

        StatusMessage = $"Editando request: {selected.Descripcion}";
    }

    private async void GuardarDistribuidoraButton_Click(object sender, RoutedEventArgs e)
    {
        var nombre = DistribuidoraNombreInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.Show("El nombre es obligatorio.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

            Distribuidora distribuidora;
            if (_editingDistribuidoraId.HasValue)
            {
                distribuidora = await db.Distribuidoras.FirstOrDefaultAsync(x => x.Id == _editingDistribuidoraId.Value)
                    ?? throw new InvalidOperationException("Distribuidora no encontrada.");
            }
            else
            {
                distribuidora = new Distribuidora { CreatedAt = DateTime.UtcNow };
                await db.Distribuidoras.AddAsync(distribuidora);
            }

            distribuidora.Nombre = nombre;
            distribuidora.Rubro = ToNullable(DistribuidoraRubroInput.Text);
            distribuidora.Telefono = ToNullable(DistribuidoraTelefonoInput.Text);
            distribuidora.Email = ToNullable(DistribuidoraEmailInput.Text);
            distribuidora.Estado = DistribuidoraEstadoCombo.SelectedItem as string ?? "activa";
            distribuidora.Notas = ToNullable(DistribuidoraNotasInput.Text);
            distribuidora.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            ClearDistribuidoraForm();
            await RefreshAllAsync($"Distribuidora guardada: {nombre}.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar la distribuidora.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void EliminarDistribuidoraButton_Click(object sender, RoutedEventArgs e)
    {
        if (DistribuidorasGrid.SelectedItem is not DistribuidoraGridRow selected)
        {
            MessageBox.Show("Selecciona una distribuidora para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Se eliminara la distribuidora y su historial de trabajos. Queres continuar?",
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
            var distribuidora = await db.Distribuidoras.FirstOrDefaultAsync(x => x.Id == selected.Id);
            if (distribuidora is null)
            {
                MessageBox.Show("La distribuidora ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
                return;
            }

            db.Distribuidoras.Remove(distribuidora);
            await db.SaveChangesAsync();

            ClearDistribuidoraForm();
            await RefreshAllAsync("Distribuidora eliminada.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar la distribuidora.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NuevoDistribuidoraButton_Click(object sender, RoutedEventArgs e)
    {
        ClearDistribuidoraForm();
    }

    private void DistribuidorasGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DistribuidorasGrid.SelectedItem is not DistribuidoraGridRow selected)
        {
            return;
        }

        _editingDistribuidoraId = selected.Id;
        DistribuidoraNombreInput.Text = selected.Nombre;
        DistribuidoraRubroInput.Text = selected.Rubro ?? string.Empty;
        DistribuidoraTelefonoInput.Text = selected.Telefono ?? string.Empty;
        DistribuidoraEmailInput.Text = selected.Email ?? string.Empty;
        DistribuidoraEstadoCombo.SelectedItem = EstadoDistribuidoras.Contains(selected.Estado) ? selected.Estado : "activa";
        DistribuidoraNotasInput.Text = selected.Notas ?? string.Empty;

        StatusMessage = $"Editando distribuidora: {selected.Nombre}";
    }

    private async void GuardarTrabajoDistribuidoraButton_Click(object sender, RoutedEventArgs e)
    {
        var distribuidoraId = ParseNullableInt(TrabajoDistribuidoraCombo.SelectedValue);
        if (!distribuidoraId.HasValue)
        {
            MessageBox.Show("Debes seleccionar una distribuidora.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var descripcion = TrabajoDescripcionInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            MessageBox.Show("La descripcion es obligatoria.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseDecimal(TrabajoCostoInput.Text, out var costo))
        {
            MessageBox.Show("El costo debe ser numerico.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

            TrabajoDistribuidora trabajo;
            if (_editingTrabajoDistribuidoraId.HasValue)
            {
                trabajo = await db.TrabajosDistribuidora.FirstOrDefaultAsync(x => x.Id == _editingTrabajoDistribuidoraId.Value)
                    ?? throw new InvalidOperationException("Trabajo tercerizado no encontrado.");
            }
            else
            {
                trabajo = new TrabajoDistribuidora { CreatedAt = DateTime.UtcNow };
                await db.TrabajosDistribuidora.AddAsync(trabajo);
            }

            trabajo.DistribuidoraId = distribuidoraId.Value;
            trabajo.ClienteId = ParseNullableInt(TrabajoClienteCombo.SelectedValue);
            trabajo.VehiculoId = ParseNullableInt(TrabajoVehiculoCombo.SelectedValue);
            trabajo.Descripcion = descripcion;
            trabajo.Fecha = (TrabajoFechaInput.SelectedDate ?? DateTime.Today).Date;
            trabajo.Costo = costo;
            trabajo.EstadoPago = TrabajoEstadoPagoCombo.SelectedItem as string ?? "pagado";
            trabajo.Notas = ToNullable(TrabajoNotasInput.Text);
            trabajo.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            ClearTrabajoDistribuidoraForm();
            await RefreshAllAsync("Trabajo tercerizado guardado.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar el trabajo tercerizado.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void EliminarTrabajoDistribuidoraButton_Click(object sender, RoutedEventArgs e)
    {
        if (TrabajosDistribuidoraGrid.SelectedItem is not TrabajoDistribuidoraGridRow selected)
        {
            MessageBox.Show("Selecciona un trabajo para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Se eliminara el trabajo tercerizado seleccionado. Queres continuar?",
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
            var trabajo = await db.TrabajosDistribuidora.FirstOrDefaultAsync(x => x.Id == selected.Id);
            if (trabajo is null)
            {
                MessageBox.Show("El trabajo ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
                return;
            }

            db.TrabajosDistribuidora.Remove(trabajo);
            await db.SaveChangesAsync();

            ClearTrabajoDistribuidoraForm();
            await RefreshAllAsync("Trabajo tercerizado eliminado.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar el trabajo tercerizado.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NuevoTrabajoDistribuidoraButton_Click(object sender, RoutedEventArgs e)
    {
        ClearTrabajoDistribuidoraForm();
    }

    private void TrabajosDistribuidoraGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TrabajosDistribuidoraGrid.SelectedItem is not TrabajoDistribuidoraGridRow selected)
        {
            return;
        }

        _editingTrabajoDistribuidoraId = selected.Id;
        TrabajoDistribuidoraCombo.SelectedValue = selected.DistribuidoraId;
        TrabajoClienteCombo.SelectedValue = selected.ClienteId;
        TrabajoVehiculoCombo.SelectedValue = selected.VehiculoId;
        TrabajoFechaInput.SelectedDate = selected.Fecha;
        TrabajoEstadoPagoCombo.SelectedItem = EstadoPagoTrabajoDistribuidora.Contains(selected.EstadoPago) ? selected.EstadoPago : "pagado";
        TrabajoCostoInput.Text = selected.Costo.ToString("0.##", CultureInfo.InvariantCulture);
        TrabajoDescripcionInput.Text = selected.Descripcion;
        TrabajoNotasInput.Text = selected.Notas ?? string.Empty;

        StatusMessage = $"Editando trabajo: {selected.Descripcion}";
    }

    private void ClearSolicitudForm()
    {
        _editingSolicitudId = null;
        SolicitudClienteCombo.SelectedValue = null;
        SolicitudVehiculoCombo.SelectedValue = null;
        SolicitudFechaInput.SelectedDate = DateTime.Today;
        SolicitudEstadoCombo.SelectedItem = "pendiente";
        SolicitudPrioridadCombo.SelectedItem = "media";
        SolicitudCanalCombo.SelectedItem = "telefono";
        SolicitudDescripcionInput.Text = string.Empty;
        SolicitudNotasInput.Text = string.Empty;
        SolicitudesGrid.SelectedItem = null;
    }

    private void ClearDistribuidoraForm()
    {
        _editingDistribuidoraId = null;
        DistribuidoraNombreInput.Text = string.Empty;
        DistribuidoraRubroInput.Text = string.Empty;
        DistribuidoraTelefonoInput.Text = string.Empty;
        DistribuidoraEmailInput.Text = string.Empty;
        DistribuidoraEstadoCombo.SelectedItem = "activa";
        DistribuidoraNotasInput.Text = string.Empty;
        DistribuidorasGrid.SelectedItem = null;
    }

    private void ClearTrabajoDistribuidoraForm()
    {
        _editingTrabajoDistribuidoraId = null;
        TrabajoDistribuidoraCombo.SelectedValue = null;
        TrabajoClienteCombo.SelectedValue = null;
        TrabajoVehiculoCombo.SelectedValue = null;
        TrabajoFechaInput.SelectedDate = DateTime.Today;
        TrabajoEstadoPagoCombo.SelectedItem = "pagado";
        TrabajoCostoInput.Text = string.Empty;
        TrabajoDescripcionInput.Text = string.Empty;
        TrabajoNotasInput.Text = string.Empty;
        TrabajosDistribuidoraGrid.SelectedItem = null;
    }
}

public sealed class SolicitudGridRow
{
    public int Id { get; init; }
    public int? ClienteId { get; init; }
    public int? VehiculoId { get; init; }
    public string ClienteNombre { get; init; } = "Sin cliente";
    public string? Patente { get; init; }
    public string VehiculoNombre { get; init; } = "Sin vehiculo";
    public string Descripcion { get; init; } = string.Empty;
    public DateTime FechaSolicitud { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Prioridad { get; init; } = string.Empty;
    public string? Canal { get; init; }
    public string? Notas { get; init; }
}

public sealed class DistribuidoraGridRow
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Rubro { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string? Notas { get; init; }
    public decimal GastoTotal { get; init; }
    public int CantidadTrabajos { get; init; }
}

public sealed class TrabajoDistribuidoraGridRow
{
    public int Id { get; init; }
    public int DistribuidoraId { get; init; }
    public int? ClienteId { get; init; }
    public int? VehiculoId { get; init; }
    public string DistribuidoraNombre { get; init; } = string.Empty;
    public string ClienteNombre { get; init; } = "Sin cliente";
    public string? Patente { get; init; }
    public string VehiculoNombre { get; init; } = "Sin vehiculo";
    public string Descripcion { get; init; } = string.Empty;
    public DateTime Fecha { get; init; }
    public decimal Costo { get; init; }
    public string EstadoPago { get; init; } = string.Empty;
    public string? Notas { get; init; }
}

public sealed class DistribuidoraLookupItem
{
    public int? Id { get; init; }
    public string Display { get; init; } = string.Empty;
}



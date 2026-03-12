using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using PipitaDesktop.Data;
using PipitaDesktop.Models;
using PipitaDesktop.Services;

namespace PipitaDesktop;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private int? _editingClienteId;
    private int? _editingVehiculoId;
    private int? _editingParteId;
    private int? _editingServicioId;
    private int? _editingReporteId;
    private bool _isBusy;
    private string _statusMessage = "Listo para cargar datos.";

    private List<Cliente> _allClientes = new();
    private List<VehiculoGridRow> _allVehiculos = new();
    private List<Parte> _allPartes = new();
    private List<ServicioGridRow> _allServicios = new();
    private List<Reporte> _allReportes = new();

    private readonly GridViewState _clientesState = new();
    private readonly GridViewState _vehiculosState = new();
    private readonly GridViewState _partesState = new();
    private readonly GridViewState _serviciosState = new();
    private readonly GridViewState _reportesState = new();

    private string _dashboardPeriodo = "Ultimos 30 dias";
    private string _dashboardClientesActivos = "0";
    private string _dashboardVehiculosActivos = "0";
    private string _dashboardStockPartes = "0";
    private string _dashboardServiciosPeriodo = "0";
    private string _dashboardCostoServiciosPeriodo = "$0.00";
    private string _dashboardReportesPeriodo = "0";
    private string _clienteHistorialTitulo = "Historial del cliente";
    private string _clienteHistorialResumen = "Selecciona un cliente para ver su actividad.";
    private string _clienteHistorialAutosMetric = "0";
    private string _clienteHistorialSolicitudesMetric = "0";
    private string _clienteHistorialServiciosMetric = "$0.00";
    private string _clienteHistorialTercerosMetric = "$0.00";
    private string _clienteHistorialTotalMetric = "$0.00";

    public ObservableCollection<Cliente> Clientes { get; } = new();
    public ObservableCollection<VehiculoGridRow> Vehiculos { get; } = new();
    public ObservableCollection<Parte> Partes { get; } = new();
    public ObservableCollection<ServicioGridRow> Servicios { get; } = new();
    public ObservableCollection<Reporte> Reportes { get; } = new();
    public ObservableCollection<VehiculoGridRow> ClienteHistorialVehiculos { get; } = new();
    public ObservableCollection<SolicitudGridRow> ClienteHistorialSolicitudes { get; } = new();
    public ObservableCollection<ServicioGridRow> ClienteHistorialServicios { get; } = new();
    public ObservableCollection<TrabajoDistribuidoraGridRow> ClienteHistorialTrabajosDistribuidora { get; } = new();
    public ObservableCollection<ClienteLookupItem> ClienteOptions { get; } = new();
    public ObservableCollection<VehiculoLookupItem> VehiculoOptions { get; } = new();
    public ObservableCollection<ClienteLookupItem> ClienteFilterOptions { get; } = new();
    public ObservableCollection<string> EstadoClientes { get; } = new(new[] { "activo", "inactivo" });
    public ObservableCollection<string> EstadoVehiculos { get; } = new(new[] { "disponible", "reservado", "en_taller", "vendido" });
    public ObservableCollection<string> EstadoClientesConTodos { get; } = new(new[] { "Todos", "activo", "inactivo" });
    public ObservableCollection<string> EstadoVehiculosConTodos { get; } = new(new[] { "Todos", "disponible", "reservado", "en_taller", "vendido" });

    public string DashboardPeriodo
    {
        get => _dashboardPeriodo;
        private set => SetField(ref _dashboardPeriodo, value);
    }

    public string DashboardClientesActivos
    {
        get => _dashboardClientesActivos;
        private set => SetField(ref _dashboardClientesActivos, value);
    }

    public string DashboardVehiculosActivos
    {
        get => _dashboardVehiculosActivos;
        private set => SetField(ref _dashboardVehiculosActivos, value);
    }

    public string DashboardStockPartes
    {
        get => _dashboardStockPartes;
        private set => SetField(ref _dashboardStockPartes, value);
    }

    public string DashboardServiciosPeriodo
    {
        get => _dashboardServiciosPeriodo;
        private set => SetField(ref _dashboardServiciosPeriodo, value);
    }

    public string DashboardCostoServiciosPeriodo
    {
        get => _dashboardCostoServiciosPeriodo;
        private set => SetField(ref _dashboardCostoServiciosPeriodo, value);
    }

    public string DashboardReportesPeriodo
    {
        get => _dashboardReportesPeriodo;
        private set => SetField(ref _dashboardReportesPeriodo, value);
    }

    public string ClientesPaginacionTexto => _clientesState.PageText;
    public bool PuedeRetrocederClientes => _clientesState.Page > 1;
    public bool PuedeAvanzarClientes => _clientesState.Page < _clientesState.TotalPages;

    public string VehiculosPaginacionTexto => _vehiculosState.PageText;
    public bool PuedeRetrocederVehiculos => _vehiculosState.Page > 1;
    public bool PuedeAvanzarVehiculos => _vehiculosState.Page < _vehiculosState.TotalPages;

    public string PartesPaginacionTexto => _partesState.PageText;
    public bool PuedeRetrocederPartes => _partesState.Page > 1;
    public bool PuedeAvanzarPartes => _partesState.Page < _partesState.TotalPages;

    public string ServiciosPaginacionTexto => _serviciosState.PageText;
    public bool PuedeRetrocederServicios => _serviciosState.Page > 1;
    public bool PuedeAvanzarServicios => _serviciosState.Page < _serviciosState.TotalPages;

    public string ReportesPaginacionTexto => _reportesState.PageText;
    public bool PuedeRetrocederReportes => _reportesState.Page > 1;
    public bool PuedeAvanzarReportes => _reportesState.Page < _reportesState.TotalPages;

    public string ClienteHistorialTitulo
    {
        get => _clienteHistorialTitulo;
        private set => SetField(ref _clienteHistorialTitulo, value);
    }

    public string ClienteHistorialResumen
    {
        get => _clienteHistorialResumen;
        private set => SetField(ref _clienteHistorialResumen, value);
    }

    public string ClienteHistorialAutosMetric
    {
        get => _clienteHistorialAutosMetric;
        private set => SetField(ref _clienteHistorialAutosMetric, value);
    }

    public string ClienteHistorialSolicitudesMetric
    {
        get => _clienteHistorialSolicitudesMetric;
        private set => SetField(ref _clienteHistorialSolicitudesMetric, value);
    }

    public string ClienteHistorialServiciosMetric
    {
        get => _clienteHistorialServiciosMetric;
        private set => SetField(ref _clienteHistorialServiciosMetric, value);
    }

    public string ClienteHistorialTercerosMetric
    {
        get => _clienteHistorialTercerosMetric;
        private set => SetField(ref _clienteHistorialTercerosMetric, value);
    }

    public string ClienteHistorialTotalMetric
    {
        get => _clienteHistorialTotalMetric;
        private set => SetField(ref _clienteHistorialTotalMetric, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ClienteEstadoCombo.SelectedItem = "activo";
        VehiculoEstadoCombo.SelectedItem = "disponible";
        ServicioFechaInput.SelectedDate = DateTime.Today;
        ReporteGeneradoElInput.SelectedDate = DateTime.Today;

        ClientesFiltroEstadoCombo.SelectedItem = "Todos";
        VehiculosFiltroEstadoCombo.SelectedItem = "Todos";
        VehiculosFiltroClienteCombo.SelectedValue = null;
        ServiciosFiltroDesdeInput.SelectedDate = null;
        ServiciosFiltroHastaInput.SelectedDate = null;
        ReportesFiltroDesdeInput.SelectedDate = null;
        ReportesFiltroHastaInput.SelectedDate = null;
        SolicitudesFiltroEstadoCombo.SelectedItem = "Todos";
        DistribuidorasFiltroEstadoCombo.SelectedItem = "Todos";
        TrabajosDistribuidoraFiltroDistribuidoraCombo.SelectedValue = null;
        SolicitudFechaInput.SelectedDate = DateTime.Today;
        SolicitudHoraInput.Text = "09:00";
        SolicitudDuracionInput.Text = "60";
        SolicitudEstadoCombo.SelectedItem = "pendiente";
        SolicitudPrioridadCombo.SelectedItem = "media";
        SolicitudCanalCombo.SelectedItem = "telefono";
        DistribuidoraEstadoCombo.SelectedItem = "activa";
        TrabajoFechaInput.SelectedDate = DateTime.Today;
        TrabajoEstadoPagoCombo.SelectedItem = "pagado";
        AgendaFiltroEstadoCombo.SelectedItem = "Todos";
        DashboardDesdeInput.SelectedDate = DateTime.Today.AddDays(-30);
        DashboardHastaInput.SelectedDate = DateTime.Today;

        await RefreshAllAsync();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={DatabasePathProvider.DatabasePath}")
            .Options;

        return new AppDbContext(options);
    }

    private async Task RefreshAllAsync(string? status = null)
    {
        try
        {
            IsBusy = true;
            await LoadClientesAsync();
            await LoadVehiculosAsync();
            await LoadPartesAsync();
            await LoadServiciosAsync();
            await LoadReportesAsync();
            await LoadSolicitudesAsync();
            await LoadDistribuidorasModuleAsync();
            await LoadAgendaAsync();
            UpdateDashboardMetrics();
            RefreshClienteHistorial(_editingClienteId);
            var effectiveStatus = status ?? $"Datos actualizados ({DateTime.Now:HH:mm:ss}).";
            StatusMessage = effectiveStatus;

            if (!string.IsNullOrWhiteSpace(status))
            {
                ShowSuccessToast(status, "Operacion completada");
            }
        }
        catch (Exception ex)
        {
            ShowError("No se pudieron cargar los datos.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadClientesAsync()
    {
        using var db = CreateDbContext();
        var clientes = await db.Clientes
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        _allClientes = clientes;

        ClienteOptions.Clear();
        ClienteOptions.Add(new ClienteLookupItem { Id = null, Display = "Sin cliente asignado" });

        ClienteFilterOptions.Clear();
        ClienteFilterOptions.Add(new ClienteLookupItem { Id = null, Display = "Todos los clientes" });

        foreach (var cliente in clientes)
        {
            var label = $"{cliente.Nombre} ({cliente.Telefono ?? "sin telefono"})";
            ClienteOptions.Add(new ClienteLookupItem { Id = cliente.Id, Display = label });
            ClienteFilterOptions.Add(new ClienteLookupItem { Id = cliente.Id, Display = label });
        }

        ApplyClientesFilters();
    }

    private async Task LoadVehiculosAsync()
    {
        using var db = CreateDbContext();
        var vehiculos = await db.Vehiculos
            .AsNoTracking()
            .Include(x => x.Cliente)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        _allVehiculos = vehiculos
            .Select(
                vehiculo => new VehiculoGridRow
                {
                    Id = vehiculo.Id,
                    Patente = vehiculo.Patente,
                    Marca = vehiculo.Marca,
                    Modelo = vehiculo.Modelo,
                    Version = vehiculo.Version,
                    Anio = vehiculo.Anio,
                    Estado = vehiculo.Estado,
                    ClienteId = vehiculo.ClienteId,
                    ClienteNombre = vehiculo.Cliente?.Nombre ?? "Sin cliente",
                })
            .ToList();

        VehiculoOptions.Clear();
        VehiculoOptions.Add(new VehiculoLookupItem { Id = null, Display = "Sin vehiculo asignado" });
        foreach (var vehiculo in vehiculos.OrderBy(x => x.Patente).ThenBy(x => x.Marca).ThenBy(x => x.Modelo))
        {
            var patente = string.IsNullOrWhiteSpace(vehiculo.Patente) ? "Sin patente" : vehiculo.Patente;
            var cliente = vehiculo.Cliente?.Nombre ?? "Sin cliente";
            VehiculoOptions.Add(
                new VehiculoLookupItem
                {
                    Id = vehiculo.Id,
                    Display = $"{patente} - {vehiculo.Marca} {vehiculo.Modelo} ({cliente})",
                });
        }

        ApplyVehiculosFilters();
    }

    private async Task LoadPartesAsync()
    {
        using var db = CreateDbContext();
        var partes = await db.Partes
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        _allPartes = partes;
        ApplyPartesFilters();
    }

    private async Task LoadServiciosAsync()
    {
        using var db = CreateDbContext();
        var servicios = await db.Servicios
            .AsNoTracking()
            .Include(x => x.Vehiculo)
            .ThenInclude(x => x!.Cliente)
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        _allServicios = servicios
            .Select(
                servicio => new ServicioGridRow
                {
                    Id = servicio.Id,
                    VehiculoId = servicio.VehiculoId,
                    Patente = servicio.Vehiculo?.Patente,
                    VehiculoNombre = servicio.Vehiculo is null
                        ? "Vehiculo no encontrado"
                        : $"{servicio.Vehiculo.Marca} {servicio.Vehiculo.Modelo}",
                    ClienteNombre = servicio.Vehiculo?.Cliente?.Nombre ?? "Sin cliente",
                    Descripcion = servicio.Descripcion,
                    Fecha = servicio.Fecha,
                    Kilometraje = servicio.Kilometraje,
                    Costo = servicio.Costo,
                    Notas = servicio.Notas,
                })
            .ToList();

        ApplyServiciosFilters();
    }

    private async Task LoadReportesAsync()
    {
        using var db = CreateDbContext();
        var reportes = await db.Reportes
            .AsNoTracking()
            .OrderByDescending(x => x.GeneradoEl)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        _allReportes = reportes;
        ApplyReportesFilters();
    }

    private void AnyFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ResetAllPages();
        ApplyAllFilters();
    }

    private void AnyFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ResetAllPages();
        ApplyAllFilters();
    }

    private void LimpiarFiltroClientesButton_Click(object sender, RoutedEventArgs e)
    {
        ClientesFiltroTextoInput.Text = string.Empty;
        ClientesFiltroEstadoCombo.SelectedItem = "Todos";
        ApplyClientesFilters();
    }

    private void LimpiarFiltroVehiculosButton_Click(object sender, RoutedEventArgs e)
    {
        VehiculosFiltroTextoInput.Text = string.Empty;
        VehiculosFiltroEstadoCombo.SelectedItem = "Todos";
        VehiculosFiltroClienteCombo.SelectedValue = null;
        ApplyVehiculosFilters();
    }

    private void LimpiarFiltroPartesButton_Click(object sender, RoutedEventArgs e)
    {
        PartesFiltroTextoInput.Text = string.Empty;
        ApplyPartesFilters();
    }

    private void LimpiarFiltroServiciosButton_Click(object sender, RoutedEventArgs e)
    {
        ServiciosFiltroPatenteInput.Text = string.Empty;
        ServiciosFiltroClienteInput.Text = string.Empty;
        ServiciosFiltroDesdeInput.SelectedDate = null;
        ServiciosFiltroHastaInput.SelectedDate = null;
        ApplyServiciosFilters();
    }

    private void LimpiarFiltroReportesButton_Click(object sender, RoutedEventArgs e)
    {
        ReportesFiltroTextoInput.Text = string.Empty;
        ReportesFiltroDesdeInput.SelectedDate = null;
        ReportesFiltroHastaInput.SelectedDate = null;
        ApplyReportesFilters();
    }

    private void ApplyAllFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyClientesFilters();
        ApplyVehiculosFilters();
        ApplyPartesFilters();
        ApplyServiciosFilters();
        ApplyReportesFilters();
        ApplySolicitudesFilters();
        ApplyDistribuidorasFilters();
        ApplyTrabajosDistribuidoraFilters();
        ApplyAgendaFilters();
    }

    private void ApplyClientesFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        var query = _allClientes.AsEnumerable();
        var text = ToNullable(ClientesFiltroTextoInput.Text);
        var estado = ClientesFiltroEstadoCombo.SelectedItem as string;

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(
                x => ContainsIgnoreCase(x.Nombre, text)
                    || ContainsIgnoreCase(x.Telefono, text)
                    || ContainsIgnoreCase(x.Email, text));
        }

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => string.Equals(x.Estado, estado, StringComparison.OrdinalIgnoreCase));
        }

        query = ApplyClientesSort(query);
        var pageItems = Paginate(query, _clientesState);
        ReplaceCollection(Clientes, pageItems);
        NotifyClientesPaginationChanged();
    }

    private void ApplyVehiculosFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        var query = _allVehiculos.AsEnumerable();
        var text = ToNullable(VehiculosFiltroTextoInput.Text);
        var estado = VehiculosFiltroEstadoCombo.SelectedItem as string;
        var clienteId = ParseNullableInt(VehiculosFiltroClienteCombo.SelectedValue);

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(
                x => ContainsIgnoreCase(x.Patente, text)
                    || ContainsIgnoreCase(x.Marca, text)
                    || ContainsIgnoreCase(x.Modelo, text)
                    || ContainsIgnoreCase(x.ClienteNombre, text));
        }

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => string.Equals(x.Estado, estado, StringComparison.OrdinalIgnoreCase));
        }

        if (clienteId.HasValue)
        {
            query = query.Where(x => x.ClienteId == clienteId.Value);
        }

        query = ApplyVehiculosSort(query);
        var pageItems = Paginate(query, _vehiculosState);
        ReplaceCollection(Vehiculos, pageItems);
        NotifyVehiculosPaginationChanged();
    }

    private void ApplyPartesFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        var query = _allPartes.AsEnumerable();
        var text = ToNullable(PartesFiltroTextoInput.Text);

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(x => ContainsIgnoreCase(x.Nombre, text));
        }

        query = ApplyPartesSort(query);
        var pageItems = Paginate(query, _partesState);
        ReplaceCollection(Partes, pageItems);
        NotifyPartesPaginationChanged();
    }

    private void ApplyServiciosFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        var query = _allServicios.AsEnumerable();
        var patente = ToNullable(ServiciosFiltroPatenteInput.Text);
        var cliente = ToNullable(ServiciosFiltroClienteInput.Text);
        var desde = ServiciosFiltroDesdeInput.SelectedDate?.Date;
        var hasta = ServiciosFiltroHastaInput.SelectedDate?.Date;

        if (!string.IsNullOrWhiteSpace(patente))
        {
            query = query.Where(x => ContainsIgnoreCase(x.Patente, patente));
        }

        if (!string.IsNullOrWhiteSpace(cliente))
        {
            query = query.Where(x => ContainsIgnoreCase(x.ClienteNombre, cliente));
        }

        if (desde.HasValue)
        {
            query = query.Where(x => x.Fecha.Date >= desde.Value);
        }

        if (hasta.HasValue)
        {
            query = query.Where(x => x.Fecha.Date <= hasta.Value);
        }

        query = ApplyServiciosSort(query);
        var pageItems = Paginate(query, _serviciosState);
        ReplaceCollection(Servicios, pageItems);
        NotifyServiciosPaginationChanged();
    }

    private void ApplyReportesFilters()
    {
        if (!IsLoaded)
        {
            return;
        }

        var query = _allReportes.AsEnumerable();
        var text = ToNullable(ReportesFiltroTextoInput.Text);
        var desde = ReportesFiltroDesdeInput.SelectedDate?.Date;
        var hasta = ReportesFiltroHastaInput.SelectedDate?.Date;

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(x => ContainsIgnoreCase(x.Titulo, text) || ContainsIgnoreCase(x.Periodo, text));
        }

        if (desde.HasValue)
        {
            query = query.Where(x => x.GeneradoEl.Date >= desde.Value);
        }

        if (hasta.HasValue)
        {
            query = query.Where(x => x.GeneradoEl.Date <= hasta.Value);
        }

        query = ApplyReportesSort(query);
        var pageItems = Paginate(query, _reportesState);
        ReplaceCollection(Reportes, pageItems);
        NotifyReportesPaginationChanged();
    }

    private IEnumerable<Cliente> ApplyClientesSort(IEnumerable<Cliente> query)
    {
        return (_clientesState.SortMember, _clientesState.SortDirection) switch
        {
            ("Nombre", ListSortDirection.Descending) => query.OrderByDescending(x => x.Nombre),
            ("Telefono", ListSortDirection.Ascending) => query.OrderBy(x => x.Telefono),
            ("Telefono", ListSortDirection.Descending) => query.OrderByDescending(x => x.Telefono),
            ("Email", ListSortDirection.Ascending) => query.OrderBy(x => x.Email),
            ("Email", ListSortDirection.Descending) => query.OrderByDescending(x => x.Email),
            ("Estado", ListSortDirection.Ascending) => query.OrderBy(x => x.Estado),
            ("Estado", ListSortDirection.Descending) => query.OrderByDescending(x => x.Estado),
            _ => query.OrderBy(x => x.Nombre),
        };
    }

    private IEnumerable<VehiculoGridRow> ApplyVehiculosSort(IEnumerable<VehiculoGridRow> query)
    {
        return (_vehiculosState.SortMember, _vehiculosState.SortDirection) switch
        {
            ("Patente", ListSortDirection.Ascending) => query.OrderBy(x => x.Patente),
            ("Patente", ListSortDirection.Descending) => query.OrderByDescending(x => x.Patente),
            ("Marca", ListSortDirection.Ascending) => query.OrderBy(x => x.Marca),
            ("Marca", ListSortDirection.Descending) => query.OrderByDescending(x => x.Marca),
            ("Modelo", ListSortDirection.Ascending) => query.OrderBy(x => x.Modelo),
            ("Modelo", ListSortDirection.Descending) => query.OrderByDescending(x => x.Modelo),
            ("Version", ListSortDirection.Ascending) => query.OrderBy(x => x.Version),
            ("Version", ListSortDirection.Descending) => query.OrderByDescending(x => x.Version),
            ("Anio", ListSortDirection.Ascending) => query.OrderBy(x => x.Anio),
            ("Anio", ListSortDirection.Descending) => query.OrderByDescending(x => x.Anio),
            ("Estado", ListSortDirection.Ascending) => query.OrderBy(x => x.Estado),
            ("Estado", ListSortDirection.Descending) => query.OrderByDescending(x => x.Estado),
            ("ClienteNombre", ListSortDirection.Ascending) => query.OrderBy(x => x.ClienteNombre),
            ("ClienteNombre", ListSortDirection.Descending) => query.OrderByDescending(x => x.ClienteNombre),
            _ => query.OrderByDescending(x => x.Id),
        };
    }

    private IEnumerable<Parte> ApplyPartesSort(IEnumerable<Parte> query)
    {
        return (_partesState.SortMember, _partesState.SortDirection) switch
        {
            ("Nombre", ListSortDirection.Descending) => query.OrderByDescending(x => x.Nombre),
            ("Stock", ListSortDirection.Ascending) => query.OrderBy(x => x.Stock),
            ("Stock", ListSortDirection.Descending) => query.OrderByDescending(x => x.Stock),
            ("Costo", ListSortDirection.Ascending) => query.OrderBy(x => x.Costo),
            ("Costo", ListSortDirection.Descending) => query.OrderByDescending(x => x.Costo),
            ("CreatedAt", ListSortDirection.Ascending) => query.OrderBy(x => x.CreatedAt),
            ("CreatedAt", ListSortDirection.Descending) => query.OrderByDescending(x => x.CreatedAt),
            _ => query.OrderBy(x => x.Nombre),
        };
    }

    private IEnumerable<ServicioGridRow> ApplyServiciosSort(IEnumerable<ServicioGridRow> query)
    {
        return (_serviciosState.SortMember, _serviciosState.SortDirection) switch
        {
            ("Fecha", ListSortDirection.Ascending) => query.OrderBy(x => x.Fecha),
            ("Fecha", ListSortDirection.Descending) => query.OrderByDescending(x => x.Fecha),
            ("Patente", ListSortDirection.Ascending) => query.OrderBy(x => x.Patente),
            ("Patente", ListSortDirection.Descending) => query.OrderByDescending(x => x.Patente),
            ("VehiculoNombre", ListSortDirection.Ascending) => query.OrderBy(x => x.VehiculoNombre),
            ("VehiculoNombre", ListSortDirection.Descending) => query.OrderByDescending(x => x.VehiculoNombre),
            ("ClienteNombre", ListSortDirection.Ascending) => query.OrderBy(x => x.ClienteNombre),
            ("ClienteNombre", ListSortDirection.Descending) => query.OrderByDescending(x => x.ClienteNombre),
            ("Descripcion", ListSortDirection.Ascending) => query.OrderBy(x => x.Descripcion),
            ("Descripcion", ListSortDirection.Descending) => query.OrderByDescending(x => x.Descripcion),
            ("Kilometraje", ListSortDirection.Ascending) => query.OrderBy(x => x.Kilometraje),
            ("Kilometraje", ListSortDirection.Descending) => query.OrderByDescending(x => x.Kilometraje),
            ("Costo", ListSortDirection.Ascending) => query.OrderBy(x => x.Costo),
            ("Costo", ListSortDirection.Descending) => query.OrderByDescending(x => x.Costo),
            _ => query.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id),
        };
    }

    private IEnumerable<Reporte> ApplyReportesSort(IEnumerable<Reporte> query)
    {
        return (_reportesState.SortMember, _reportesState.SortDirection) switch
        {
            ("Titulo", ListSortDirection.Ascending) => query.OrderBy(x => x.Titulo),
            ("Titulo", ListSortDirection.Descending) => query.OrderByDescending(x => x.Titulo),
            ("Periodo", ListSortDirection.Ascending) => query.OrderBy(x => x.Periodo),
            ("Periodo", ListSortDirection.Descending) => query.OrderByDescending(x => x.Periodo),
            ("GeneradoEl", ListSortDirection.Ascending) => query.OrderBy(x => x.GeneradoEl),
            ("GeneradoEl", ListSortDirection.Descending) => query.OrderByDescending(x => x.GeneradoEl),
            ("CreatedAt", ListSortDirection.Ascending) => query.OrderBy(x => x.CreatedAt),
            ("CreatedAt", ListSortDirection.Descending) => query.OrderByDescending(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.GeneradoEl).ThenByDescending(x => x.Id),
        };
    }

    private IEnumerable<T> Paginate<T>(IEnumerable<T> source, GridViewState state)
    {
        var materialized = source.ToList();
        state.TotalItems = materialized.Count;
        state.TotalPages = Math.Max(1, (int)Math.Ceiling(state.TotalItems / (double)state.PageSize));

        if (state.Page > state.TotalPages)
        {
            state.Page = state.TotalPages;
        }

        if (state.Page < 1)
        {
            state.Page = 1;
        }

        return materialized
            .Skip((state.Page - 1) * state.PageSize)
            .Take(state.PageSize);
    }

    private void NotifyClientesPaginationChanged()
    {
        OnPropertyChanged(nameof(ClientesPaginacionTexto));
        OnPropertyChanged(nameof(PuedeRetrocederClientes));
        OnPropertyChanged(nameof(PuedeAvanzarClientes));
    }

    private void NotifyVehiculosPaginationChanged()
    {
        OnPropertyChanged(nameof(VehiculosPaginacionTexto));
        OnPropertyChanged(nameof(PuedeRetrocederVehiculos));
        OnPropertyChanged(nameof(PuedeAvanzarVehiculos));
    }

    private void NotifyPartesPaginationChanged()
    {
        OnPropertyChanged(nameof(PartesPaginacionTexto));
        OnPropertyChanged(nameof(PuedeRetrocederPartes));
        OnPropertyChanged(nameof(PuedeAvanzarPartes));
    }

    private void NotifyServiciosPaginationChanged()
    {
        OnPropertyChanged(nameof(ServiciosPaginacionTexto));
        OnPropertyChanged(nameof(PuedeRetrocederServicios));
        OnPropertyChanged(nameof(PuedeAvanzarServicios));
    }

    private void NotifyReportesPaginationChanged()
    {
        OnPropertyChanged(nameof(ReportesPaginacionTexto));
        OnPropertyChanged(nameof(PuedeRetrocederReportes));
        OnPropertyChanged(nameof(PuedeAvanzarReportes));
    }

    private void ResetAllPages()
    {
        _clientesState.Page = 1;
        _vehiculosState.Page = 1;
        _partesState.Page = 1;
        _serviciosState.Page = 1;
        _reportesState.Page = 1;
    }

    private void HandleSorting(
        System.Windows.Controls.DataGrid grid,
        System.Windows.Controls.DataGridSortingEventArgs e,
        GridViewState state,
        Action applyFilters)
    {
        e.Handled = true;
        var member = e.Column.SortMemberPath;
        if (string.IsNullOrWhiteSpace(member))
        {
            return;
        }

        if (string.Equals(state.SortMember, member, StringComparison.OrdinalIgnoreCase))
        {
            state.SortDirection = state.SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            state.SortMember = member;
            state.SortDirection = ListSortDirection.Ascending;
        }

        state.Page = 1;
        UpdateSortIndicators(grid, state.SortMember, state.SortDirection);
        applyFilters();
    }

    private static void UpdateSortIndicators(
        System.Windows.Controls.DataGrid grid,
        string? member,
        ListSortDirection direction)
    {
        foreach (var column in grid.Columns)
        {
            if (!string.IsNullOrWhiteSpace(member)
                && string.Equals(column.SortMemberPath, member, StringComparison.OrdinalIgnoreCase))
            {
                column.SortDirection = direction;
            }
            else
            {
                column.SortDirection = null;
            }
        }
    }

    private void ClientesGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
    {
        HandleSorting(ClientesGrid, e, _clientesState, ApplyClientesFilters);
    }

    private void VehiculosGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
    {
        HandleSorting(VehiculosGrid, e, _vehiculosState, ApplyVehiculosFilters);
    }

    private void PartesGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
    {
        HandleSorting(PartesGrid, e, _partesState, ApplyPartesFilters);
    }

    private void ServiciosGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
    {
        HandleSorting(ServiciosGrid, e, _serviciosState, ApplyServiciosFilters);
    }

    private void ReportesGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
    {
        HandleSorting(ReportesGrid, e, _reportesState, ApplyReportesFilters);
    }

    private void ClientesPaginaAnteriorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_clientesState.Page <= 1)
        {
            return;
        }

        _clientesState.Page--;
        ApplyClientesFilters();
    }

    private void ClientesPaginaSiguienteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_clientesState.Page >= _clientesState.TotalPages)
        {
            return;
        }

        _clientesState.Page++;
        ApplyClientesFilters();
    }

    private void VehiculosPaginaAnteriorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vehiculosState.Page <= 1)
        {
            return;
        }

        _vehiculosState.Page--;
        ApplyVehiculosFilters();
    }

    private void VehiculosPaginaSiguienteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vehiculosState.Page >= _vehiculosState.TotalPages)
        {
            return;
        }

        _vehiculosState.Page++;
        ApplyVehiculosFilters();
    }

    private void PartesPaginaAnteriorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_partesState.Page <= 1)
        {
            return;
        }

        _partesState.Page--;
        ApplyPartesFilters();
    }

    private void PartesPaginaSiguienteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_partesState.Page >= _partesState.TotalPages)
        {
            return;
        }

        _partesState.Page++;
        ApplyPartesFilters();
    }

    private void ServiciosPaginaAnteriorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviciosState.Page <= 1)
        {
            return;
        }

        _serviciosState.Page--;
        ApplyServiciosFilters();
    }

    private void ServiciosPaginaSiguienteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_serviciosState.Page >= _serviciosState.TotalPages)
        {
            return;
        }

        _serviciosState.Page++;
        ApplyServiciosFilters();
    }

    private void ReportesPaginaAnteriorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_reportesState.Page <= 1)
        {
            return;
        }

        _reportesState.Page--;
        ApplyReportesFilters();
    }

    private void ReportesPaginaSiguienteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_reportesState.Page >= _reportesState.TotalPages)
        {
            return;
        }

        _reportesState.Page++;
        ApplyReportesFilters();
    }

    private void DashboardActualizarButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateDashboardMetrics();
    }

    private void UpdateDashboardMetrics()
    {
        var desde = DashboardDesdeInput.SelectedDate?.Date ?? DateTime.Today.AddDays(-30);
        var hasta = DashboardHastaInput.SelectedDate?.Date ?? DateTime.Today;
        if (desde > hasta)
        {
            (desde, hasta) = (hasta, desde);
            DashboardDesdeInput.SelectedDate = desde;
            DashboardHastaInput.SelectedDate = hasta;
        }

        var serviciosPeriodo = _allServicios.Where(x => x.Fecha.Date >= desde && x.Fecha.Date <= hasta).ToList();
        var reportesPeriodo = _allReportes.Where(x => x.GeneradoEl.Date >= desde && x.GeneradoEl.Date <= hasta).ToList();

        DashboardPeriodo = $"Periodo: {desde:dd/MM/yyyy} a {hasta:dd/MM/yyyy}";
        DashboardClientesActivos = _allClientes.Count(x => string.Equals(x.Estado, "activo", StringComparison.OrdinalIgnoreCase)).ToString("N0");
        DashboardVehiculosActivos = _allVehiculos.Count.ToString("N0");
        DashboardStockPartes = _allPartes.Sum(x => x.Stock).ToString("N0");
        DashboardServiciosPeriodo = serviciosPeriodo.Count.ToString("N0");
        DashboardCostoServiciosPeriodo = serviciosPeriodo.Sum(x => x.Costo).ToString("C2");
        DashboardReportesPeriodo = reportesPeriodo.Count.ToString("N0");
    }
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private async void ImportarLegacyDbButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar base legacy",
            Filter = "SQLite DB (*.db;*.sqlite)|*.db;*.sqlite|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            FileName = "pipita.db",
        };

        var defaultLegacyPath = BuildLegacyImportDefaultPath();
        if (File.Exists(defaultLegacyPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(defaultLegacyPath);
            dialog.FileName = Path.GetFileName(defaultLegacyPath);
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "Esta accion reemplazara los datos actuales de la app desktop por los datos de la base seleccionada.\n\nQueres continuar?",
            "Importar base legacy",
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
            var result = await LegacyImportService.ImportAsync(db, dialog.FileName, replaceExistingData: true);

            await RefreshAllAsync(
                $"Importacion completada. C:{result.Clientes} V:{result.Vehiculos} P:{result.Partes} S:{result.Servicios} R:{result.Reportes}");

            MessageBox.Show(
                $"Importacion finalizada.\n\nClientes: {result.Clientes}\nVehiculos: {result.Vehiculos}\nPartes: {result.Partes}\nServicios: {result.Servicios}\nReportes: {result.Reportes}",
                "Importar base legacy",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError("No se pudo importar la base legacy.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ExportarExcelButton_Click(object sender, RoutedEventArgs e)
    {
        var exportDirectory = GetExcelExportDirectory();
        var dialog = new ExportExcelDialog(exportDirectory, GetDefaultExcelFileName())
        {
            Owner = this,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var requestedPath = Path.Combine(exportDirectory, dialog.FileName);
        var exportPath = ResolveExcelExportPath(exportDirectory, dialog.FileName, dialog.ExportMode);
        var generatedNewPath = !string.Equals(requestedPath, exportPath, StringComparison.OrdinalIgnoreCase);

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();
            var data = await ExcelExportService.LoadAsync(db);
            await Task.Run(() => ExcelExportService.ExportToFile(data, exportPath));
            StatusMessage = $"Excel exportado: {exportPath}";

            var extraMessage = generatedNewPath
                ? "Se genero un archivo nuevo porque el nombre elegido ya existia."
                : "Archivo actualizado correctamente.";

            ShowSuccessToast($"{Path.GetFileName(exportPath)}. {extraMessage}", "Exportacion Excel");
        }
        catch (IOException)
        {
            ShowWarningToast("Cierra el archivo Excel y reintenta la exportacion.", "Excel en uso");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo exportar el archivo Excel.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ImportarExcelButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar Excel para importar",
            Filter = "Excel (*.xlsx)|*.xlsx",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var importDialog = new ImportExcelDialog(dialog.FileName)
        {
            Owner = this,
        };

        if (importDialog.ShowDialog() != true)
        {
            return;
        }

        var isSyncExact = importDialog.ImportMode == ImportExcelMode.SyncExact;
        var confirmMessage = isSyncExact
            ? @"Se aplicara una sincronizacion exacta desde Excel.

- Actualiza filas con ID
- Crea filas nuevas sin ID
- Elimina registros locales ausentes en las hojas presentes del archivo

Queres continuar?"
            : @"Se aplicara una importacion tipo merge desde Excel.

- Actualiza filas con ID
- Crea filas nuevas sin ID
- No elimina registros locales

Queres continuar?";

        var confirm = MessageBox.Show(
            confirmMessage,
            "Importar Excel",
            MessageBoxButton.YesNo,
            isSyncExact ? MessageBoxImage.Warning : MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();
            var result = await ExcelImportService.ImportAsync(db, dialog.FileName, importDialog.ImportMode);
            await RefreshAllAsync($"Importacion Excel completada. +{result.TotalCreated} / ~{result.TotalUpdated} / -{result.TotalDeleted} / !{result.TotalSkipped}");
            var importSummary = importDialog.ImportMode == ImportExcelMode.SyncExact
                ? "La base local quedo sincronizada con las hojas presentes del Excel."
                : "Los cambios del Excel se mezclaron con la base local sin eliminar registros.";
            new OperationResultDialog("Importacion Excel completada", importSummary, result.BuildSummary())
            {
                Owner = this,
            }.ShowDialog();
        }
        catch (Exception ex)
        {
            ShowError("No se pudo importar el archivo Excel.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void GuardarClienteButton_Click(object sender, RoutedEventArgs e)
    {
        var nombre = ClienteNombreInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.Show("El nombre es obligatorio.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

            Cliente cliente;
            if (_editingClienteId.HasValue)
            {
                cliente = await db.Clientes.FirstOrDefaultAsync(x => x.Id == _editingClienteId.Value)
                    ?? throw new InvalidOperationException("Cliente no encontrado.");
            }
            else
            {
                cliente = new Cliente { CreatedAt = DateTime.UtcNow };
                await db.Clientes.AddAsync(cliente);
            }

            cliente.Nombre = nombre;
            cliente.Telefono = ToNullable(ClienteTelefonoInput.Text);
            cliente.Email = ToNullable(ClienteEmailInput.Text);
            cliente.Estado = ClienteEstadoCombo.SelectedItem as string ?? "activo";
            cliente.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var nombreCliente = cliente.Nombre;
            ClearClienteForm();
            await RefreshAllAsync($"Cliente guardado: {nombreCliente}.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar el cliente.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void EliminarClienteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ClientesGrid.SelectedItem is not Cliente selected)
        {
            MessageBox.Show("Selecciona un cliente para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Se eliminara el cliente y sus vehiculos asociados. Queres continuar?",
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
            var cliente = await db.Clientes.FirstOrDefaultAsync(x => x.Id == selected.Id);
            if (cliente is null)
            {
                MessageBox.Show("El cliente ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
                return;
            }

            db.Clientes.Remove(cliente);
            await db.SaveChangesAsync();

            ClearClienteForm();
            await RefreshAllAsync("Cliente eliminado.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar el cliente.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NuevoClienteButton_Click(object sender, RoutedEventArgs e)
    {
        ClearClienteForm();
    }

    private void ClientesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ClientesGrid.SelectedItem is not Cliente selected)
        {
            ClearClienteHistorial();
            return;
        }

        _editingClienteId = selected.Id;
        ClienteNombreInput.Text = selected.Nombre;
        ClienteTelefonoInput.Text = selected.Telefono ?? string.Empty;
        ClienteEmailInput.Text = selected.Email ?? string.Empty;
        ClienteEstadoCombo.SelectedItem = EstadoClientes.Contains(selected.Estado)
            ? selected.Estado
            : "activo";

        StatusMessage = $"Editando cliente: {selected.Nombre}";
        RefreshClienteHistorial(selected.Id);
    }

    private async void GuardarVehiculoButton_Click(object sender, RoutedEventArgs e)
    {
        var marca = VehiculoMarcaInput.Text.Trim();
        var modelo = VehiculoModeloInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(marca) || string.IsNullOrWhiteSpace(modelo))
        {
            MessageBox.Show("Marca y modelo son obligatorios.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseAnio(VehiculoAnioInput.Text, out var anio))
        {
            MessageBox.Show("El anio debe ser numerico.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

            Vehiculo vehiculo;
            if (_editingVehiculoId.HasValue)
            {
                vehiculo = await db.Vehiculos.FirstOrDefaultAsync(x => x.Id == _editingVehiculoId.Value)
                    ?? throw new InvalidOperationException("Vehiculo no encontrado.");
            }
            else
            {
                vehiculo = new Vehiculo { CreatedAt = DateTime.UtcNow };
                await db.Vehiculos.AddAsync(vehiculo);
            }

            vehiculo.Patente = ToNullable(VehiculoPatenteInput.Text)?.ToUpperInvariant();
            vehiculo.Marca = marca;
            vehiculo.Modelo = modelo;
            vehiculo.Version = ToNullable(VehiculoVersionInput.Text);
            vehiculo.Anio = anio;
            vehiculo.Estado = VehiculoEstadoCombo.SelectedItem as string ?? "disponible";
            vehiculo.ClienteId = ParseNullableInt(VehiculoClienteCombo.SelectedValue);
            vehiculo.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            ClearVehiculoForm();
            await RefreshAllAsync($"Vehiculo guardado: {vehiculo.Marca} {vehiculo.Modelo}.");
        }
        catch (DbUpdateException)
        {
            MessageBox.Show(
                "No se pudo guardar el vehiculo. Verifica que la patente no este repetida.",
                "Error de datos",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar el vehiculo.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void EliminarVehiculoButton_Click(object sender, RoutedEventArgs e)
    {
        if (VehiculosGrid.SelectedItem is not VehiculoGridRow selected)
        {
            MessageBox.Show("Selecciona un vehiculo para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Se eliminara el vehiculo seleccionado. Queres continuar?",
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
            var vehiculo = await db.Vehiculos.FirstOrDefaultAsync(x => x.Id == selected.Id);
            if (vehiculo is null)
            {
                MessageBox.Show("El vehiculo ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
                return;
            }

            db.Vehiculos.Remove(vehiculo);
            await db.SaveChangesAsync();

            ClearVehiculoForm();
            await RefreshAllAsync("Vehiculo eliminado.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar el vehiculo.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NuevoVehiculoButton_Click(object sender, RoutedEventArgs e)
    {
        ClearVehiculoForm();
    }

    private void VehiculosGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (VehiculosGrid.SelectedItem is not VehiculoGridRow selected)
        {
            return;
        }

        _editingVehiculoId = selected.Id;
        VehiculoPatenteInput.Text = selected.Patente ?? string.Empty;
        VehiculoMarcaInput.Text = selected.Marca;
        VehiculoModeloInput.Text = selected.Modelo;
        VehiculoVersionInput.Text = selected.Version ?? string.Empty;
        VehiculoAnioInput.Text = selected.Anio?.ToString() ?? string.Empty;
        VehiculoEstadoCombo.SelectedItem = EstadoVehiculos.Contains(selected.Estado)
            ? selected.Estado
            : "disponible";
        VehiculoClienteCombo.SelectedValue = selected.ClienteId;

        StatusMessage = $"Editando vehiculo: {selected.Marca} {selected.Modelo}";
    }

    private async void GuardarParteButton_Click(object sender, RoutedEventArgs e)
    {
        var nombre = ParteNombreInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.Show("El nombre de la parte es obligatorio.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseNonNegativeInt(ParteStockInput.Text, out var stock))
        {
            MessageBox.Show("El stock debe ser numerico y mayor o igual a cero.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseDecimal(ParteCostoInput.Text, out var costo))
        {
            MessageBox.Show("El precio unitario debe ser numerico.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

            Parte parte;
            if (_editingParteId.HasValue)
            {
                parte = await db.Partes.FirstOrDefaultAsync(x => x.Id == _editingParteId.Value)
                    ?? throw new InvalidOperationException("Parte no encontrada.");
            }
            else
            {
                parte = new Parte { CreatedAt = DateTime.UtcNow };
                await db.Partes.AddAsync(parte);
            }

            parte.Nombre = nombre;
            parte.Stock = stock;
            parte.Costo = costo;
            parte.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            ClearParteForm();
            await RefreshAllAsync($"Parte guardada: {parte.Nombre}.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar la parte.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void EliminarParteButton_Click(object sender, RoutedEventArgs e)
    {
        if (PartesGrid.SelectedItem is not Parte selected)
        {
            MessageBox.Show("Selecciona una parte para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Se eliminara la parte seleccionada. Queres continuar?",
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
            var parte = await db.Partes.FirstOrDefaultAsync(x => x.Id == selected.Id);
            if (parte is null)
            {
                MessageBox.Show("La parte ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
                return;
            }

            db.Partes.Remove(parte);
            await db.SaveChangesAsync();

            ClearParteForm();
            await RefreshAllAsync("Parte eliminada.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar la parte.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NuevoParteButton_Click(object sender, RoutedEventArgs e)
    {
        ClearParteForm();
    }

    private void PartesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (PartesGrid.SelectedItem is not Parte selected)
        {
            return;
        }

        _editingParteId = selected.Id;
        ParteNombreInput.Text = selected.Nombre;
        ParteStockInput.Text = selected.Stock.ToString(CultureInfo.InvariantCulture);
        ParteCostoInput.Text = selected.Costo.ToString("0.##", CultureInfo.InvariantCulture);

        StatusMessage = $"Editando parte: {selected.Nombre}";
    }

    private async void GuardarServicioButton_Click(object sender, RoutedEventArgs e)
    {
        var vehiculoId = ParseNullableInt(ServicioVehiculoCombo.SelectedValue);
        if (!vehiculoId.HasValue)
        {
            MessageBox.Show("Debes seleccionar un vehiculo.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var descripcion = ServicioDescripcionInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            MessageBox.Show("La descripcion es obligatoria.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseNullableNonNegativeInt(ServicioKilometrajeInput.Text, out var kilometraje))
        {
            MessageBox.Show("El kilometraje debe ser numerico y mayor o igual a cero.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseDecimal(ServicioCostoInput.Text, out var costo))
        {
            MessageBox.Show("El costo debe ser numerico.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

            Servicio servicio;
            if (_editingServicioId.HasValue)
            {
                servicio = await db.Servicios.FirstOrDefaultAsync(x => x.Id == _editingServicioId.Value)
                    ?? throw new InvalidOperationException("Servicio no encontrado.");
            }
            else
            {
                servicio = new Servicio { CreatedAt = DateTime.UtcNow };
                await db.Servicios.AddAsync(servicio);
            }

            servicio.VehiculoId = vehiculoId.Value;
            servicio.Descripcion = descripcion;
            servicio.Fecha = (ServicioFechaInput.SelectedDate ?? DateTime.Today).Date;
            servicio.Kilometraje = kilometraje;
            servicio.Costo = costo;
            servicio.Notas = ToNullable(ServicioNotasInput.Text);
            servicio.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            ClearServicioForm();
            await RefreshAllAsync($"Servicio guardado: {descripcion}.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar el servicio.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void EliminarServicioButton_Click(object sender, RoutedEventArgs e)
    {
        if (ServiciosGrid.SelectedItem is not ServicioGridRow selected)
        {
            MessageBox.Show("Selecciona un servicio para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Se eliminara el servicio seleccionado. Queres continuar?",
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
            var servicio = await db.Servicios.FirstOrDefaultAsync(x => x.Id == selected.Id);
            if (servicio is null)
            {
                MessageBox.Show("El servicio ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
                return;
            }

            db.Servicios.Remove(servicio);
            await db.SaveChangesAsync();

            ClearServicioForm();
            await RefreshAllAsync("Servicio eliminado.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar el servicio.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NuevoServicioButton_Click(object sender, RoutedEventArgs e)
    {
        ClearServicioForm();
    }

    private void ServiciosGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ServiciosGrid.SelectedItem is not ServicioGridRow selected)
        {
            return;
        }

        _editingServicioId = selected.Id;
        ServicioVehiculoCombo.SelectedValue = selected.VehiculoId;
        ServicioDescripcionInput.Text = selected.Descripcion;
        ServicioFechaInput.SelectedDate = selected.Fecha;
        ServicioKilometrajeInput.Text = selected.Kilometraje?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        ServicioCostoInput.Text = selected.Costo.ToString("0.##", CultureInfo.InvariantCulture);
        ServicioNotasInput.Text = selected.Notas ?? string.Empty;

        StatusMessage = $"Editando servicio: {selected.Descripcion}";
    }

    private async void GuardarReporteButton_Click(object sender, RoutedEventArgs e)
    {
        var titulo = ReporteTituloInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(titulo))
        {
            MessageBox.Show("El titulo es obligatorio.", "Validacion", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            using var db = CreateDbContext();

            Reporte reporte;
            if (_editingReporteId.HasValue)
            {
                reporte = await db.Reportes.FirstOrDefaultAsync(x => x.Id == _editingReporteId.Value)
                    ?? throw new InvalidOperationException("Reporte no encontrado.");
            }
            else
            {
                reporte = new Reporte { CreatedAt = DateTime.UtcNow };
                await db.Reportes.AddAsync(reporte);
            }

            reporte.Titulo = titulo;
            reporte.Periodo = ToNullable(ReportePeriodoInput.Text);
            reporte.GeneradoEl = (ReporteGeneradoElInput.SelectedDate ?? DateTime.Today).Date;
            reporte.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            ClearReporteForm();
            await RefreshAllAsync($"Reporte guardado: {titulo}.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar el reporte.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void EliminarReporteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ReportesGrid.SelectedItem is not Reporte selected)
        {
            MessageBox.Show("Selecciona un reporte para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Se eliminara el reporte seleccionado. Queres continuar?",
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
            var reporte = await db.Reportes.FirstOrDefaultAsync(x => x.Id == selected.Id);
            if (reporte is null)
            {
                MessageBox.Show("El reporte ya no existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
                return;
            }

            db.Reportes.Remove(reporte);
            await db.SaveChangesAsync();

            ClearReporteForm();
            await RefreshAllAsync("Reporte eliminado.");
        }
        catch (Exception ex)
        {
            ShowError("No se pudo eliminar el reporte.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NuevoReporteButton_Click(object sender, RoutedEventArgs e)
    {
        ClearReporteForm();
    }

    private void ReportesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ReportesGrid.SelectedItem is not Reporte selected)
        {
            return;
        }

        _editingReporteId = selected.Id;
        ReporteTituloInput.Text = selected.Titulo;
        ReportePeriodoInput.Text = selected.Periodo ?? string.Empty;
        ReporteGeneradoElInput.SelectedDate = selected.GeneradoEl;

        StatusMessage = $"Editando reporte: {selected.Titulo}";
    }


    private void RefreshClienteHistorial(int? clienteId)
    {
        if (!clienteId.HasValue)
        {
            ClearClienteHistorial();
            return;
        }

        var cliente = _allClientes.FirstOrDefault(x => x.Id == clienteId.Value);
        var nombre = cliente?.Nombre ?? "Cliente";

        ClienteHistorialTitulo = $"Historial de {nombre}";

        var vehiculos = _allVehiculos
            .Where(x => x.ClienteId == clienteId.Value)
            .OrderBy(x => x.Patente)
            .ThenBy(x => x.Marca)
            .ToList();

        var solicitudes = _allSolicitudes
            .Where(x => x.ClienteId == clienteId.Value)
            .OrderByDescending(x => x.FechaHoraCita)
            .ToList();

        var servicios = _allServicios
            .Where(x => string.Equals(x.ClienteNombre, nombre, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Fecha)
            .ToList();

        var trabajos = _allTrabajosDistribuidora
            .Where(x => x.ClienteId == clienteId.Value)
            .OrderByDescending(x => x.Fecha)
            .ToList();

        ReplaceCollection(ClienteHistorialVehiculos, vehiculos);
        ReplaceCollection(ClienteHistorialSolicitudes, solicitudes);
        ReplaceCollection(ClienteHistorialServicios, servicios);
        ReplaceCollection(ClienteHistorialTrabajosDistribuidora, trabajos);

        var costoServicios = servicios.Sum(x => x.Costo);
        var gastoTerceros = trabajos.Sum(x => x.Costo);
        var costoTotal = costoServicios + gastoTerceros;

        ClienteHistorialAutosMetric = vehiculos.Count.ToString(CultureInfo.InvariantCulture);
        ClienteHistorialSolicitudesMetric = solicitudes.Count.ToString(CultureInfo.InvariantCulture);
        ClienteHistorialServiciosMetric = costoServicios.ToString("C2", CultureInfo.CurrentCulture);
        ClienteHistorialTercerosMetric = gastoTerceros.ToString("C2", CultureInfo.CurrentCulture);
        ClienteHistorialTotalMetric = costoTotal.ToString("C2", CultureInfo.CurrentCulture);
        ClienteHistorialResumen = $"{vehiculos.Count} auto(s), {solicitudes.Count} solicitud(es), {servicios.Count} servicio(s), {trabajos.Count} trabajo(s) externo(s).";
    }

    private void ClearClienteHistorial()
    {
        ClienteHistorialTitulo = "Historial del cliente";
        ClienteHistorialResumen = "Selecciona un cliente para ver su actividad.";
        ClienteHistorialAutosMetric = "0";
        ClienteHistorialSolicitudesMetric = "0";
        ClienteHistorialServiciosMetric = "$0.00";
        ClienteHistorialTercerosMetric = "$0.00";
        ClienteHistorialTotalMetric = "$0.00";
        ReplaceCollection(ClienteHistorialVehiculos, Array.Empty<VehiculoGridRow>());
        ReplaceCollection(ClienteHistorialSolicitudes, Array.Empty<SolicitudGridRow>());
        ReplaceCollection(ClienteHistorialServicios, Array.Empty<ServicioGridRow>());
        ReplaceCollection(ClienteHistorialTrabajosDistribuidora, Array.Empty<TrabajoDistribuidoraGridRow>());
    }
    private void ClearClienteForm()
    {
        _editingClienteId = null;
        ClienteNombreInput.Text = string.Empty;
        ClienteTelefonoInput.Text = string.Empty;
        ClienteEmailInput.Text = string.Empty;
        ClienteEstadoCombo.SelectedItem = "activo";
        ClientesGrid.SelectedItem = null;
        ClearClienteHistorial();
    }

    private void ClearVehiculoForm()
    {
        _editingVehiculoId = null;
        VehiculoPatenteInput.Text = string.Empty;
        VehiculoMarcaInput.Text = string.Empty;
        VehiculoModeloInput.Text = string.Empty;
        VehiculoVersionInput.Text = string.Empty;
        VehiculoAnioInput.Text = string.Empty;
        VehiculoEstadoCombo.SelectedItem = "disponible";
        VehiculoClienteCombo.SelectedValue = null;
        VehiculosGrid.SelectedItem = null;
    }

    private void ClearParteForm()
    {
        _editingParteId = null;
        ParteNombreInput.Text = string.Empty;
        ParteStockInput.Text = string.Empty;
        ParteCostoInput.Text = string.Empty;
        PartesGrid.SelectedItem = null;
    }

    private void ClearServicioForm()
    {
        _editingServicioId = null;
        ServicioVehiculoCombo.SelectedValue = null;
        ServicioDescripcionInput.Text = string.Empty;
        ServicioFechaInput.SelectedDate = DateTime.Today;
        ServicioKilometrajeInput.Text = string.Empty;
        ServicioCostoInput.Text = string.Empty;
        ServicioNotasInput.Text = string.Empty;
        ServiciosGrid.SelectedItem = null;
    }

    private void ClearReporteForm()
    {
        _editingReporteId = null;
        ReporteTituloInput.Text = string.Empty;
        ReportePeriodoInput.Text = string.Empty;
        ReporteGeneradoElInput.SelectedDate = DateTime.Today;
        ReportesGrid.SelectedItem = null;
    }

    private static bool TryParseAnio(string rawValue, out int? anio)
    {
        var value = rawValue.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            anio = null;
            return true;
        }

        if (int.TryParse(value, out var parsed))
        {
            anio = parsed;
            return true;
        }

        anio = null;
        return false;
    }

    private static bool TryParseNonNegativeInt(string rawValue, out int value)
    {
        var valueText = rawValue.Trim();
        if (string.IsNullOrWhiteSpace(valueText))
        {
            value = 0;
            return true;
        }

        if (int.TryParse(valueText, out var parsed) && parsed >= 0)
        {
            value = parsed;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryParseNullableNonNegativeInt(string rawValue, out int? value)
    {
        var valueText = rawValue.Trim();
        if (string.IsNullOrWhiteSpace(valueText))
        {
            value = null;
            return true;
        }

        if (int.TryParse(valueText, out var parsed) && parsed >= 0)
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryParseDecimal(string rawValue, out decimal value)
    {
        var valueText = rawValue.Trim();
        if (string.IsNullOrWhiteSpace(valueText))
        {
            value = 0;
            return true;
        }

        if (decimal.TryParse(valueText, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed) ||
            decimal.TryParse(valueText, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            value = parsed;
            return true;
        }

        value = 0;
        return false;
    }

    private static string? ToNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static int? ParseNullableInt(object? value)
    {
        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => null,
        };
    }

    private static bool ContainsIgnoreCase(string? source, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        return source.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private static string BuildLegacyImportDefaultPath()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "back",
                "instance",
                "pipita.db"));
    }

    private static string GetDefaultExcelFileName()
    {
        return "pipita-garage-maestro.xlsx";
    }

    private static string GetExcelExportDirectory()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documentsPath))
        {
            documentsPath = AppContext.BaseDirectory;
        }

        return documentsPath;
    }

    private static string ResolveExcelExportPath(string directoryPath, string fileName, ExportExcelMode exportMode)
    {
        var normalizedFileName = NormalizeExcelFileName(fileName);
        var requestedPath = Path.Combine(directoryPath, normalizedFileName);
        if (exportMode == ExportExcelMode.ReplaceExisting || !File.Exists(requestedPath))
        {
            return requestedPath;
        }

        var baseName = Path.GetFileNameWithoutExtension(normalizedFileName);
        var extension = Path.GetExtension(normalizedFileName);
        var counter = 2;
        while (true)
        {
            var candidatePath = Path.Combine(directoryPath, $"{baseName} ({counter}){extension}");
            if (!File.Exists(candidatePath))
            {
                return candidatePath;
            }

            counter++;
        }
    }

    private static string NormalizeExcelFileName(string fileName)
    {
        var trimmed = fileName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return GetDefaultExcelFileName();
        }

        return trimmed.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed}.xlsx";
    }

    private void ShowError(string message, Exception ex)
    {
        ShowErrorToast(message, "Error");
        MessageBox.Show(
            $"{message}\n\nDetalle: {ex.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class VehiculoGridRow
{
    public int? Id { get; init; }
    public string? Patente { get; init; }
    public string Marca { get; init; } = string.Empty;
    public string Modelo { get; init; } = string.Empty;
    public string? Version { get; init; }
    public int? Anio { get; init; }
    public string Estado { get; init; } = string.Empty;
    public int? ClienteId { get; init; }
    public string ClienteNombre { get; init; } = "Sin cliente";
}

public sealed class ClienteLookupItem
{
    public int? Id { get; init; }
    public string Display { get; init; } = string.Empty;
}

public sealed class VehiculoLookupItem
{
    public int? Id { get; init; }
    public string Display { get; init; } = string.Empty;
}

public sealed class ServicioGridRow
{
    public int Id { get; init; }
    public int VehiculoId { get; init; }
    public string? Patente { get; init; }
    public string VehiculoNombre { get; init; } = string.Empty;
    public string ClienteNombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public DateTime Fecha { get; init; }
    public int? Kilometraje { get; init; }
    public decimal Costo { get; init; }
    public string? Notas { get; init; }
}














internal sealed class GridViewState
{
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
    public int PageSize { get; } = 20;
    public string? SortMember { get; set; }
    public ListSortDirection SortDirection { get; set; } = ListSortDirection.Ascending;
    public string PageText => $"Pagina {Page}/{TotalPages} - {TotalItems} registros";
}








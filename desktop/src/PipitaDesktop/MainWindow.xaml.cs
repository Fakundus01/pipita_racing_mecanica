using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PipitaDesktop.Data;
using PipitaDesktop.Models;

namespace PipitaDesktop;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private int? _editingClienteId;
    private int? _editingVehiculoId;
    private bool _isBusy;
    private string _statusMessage = "Listo para cargar datos.";

    public ObservableCollection<Cliente> Clientes { get; } = new();
    public ObservableCollection<VehiculoGridRow> Vehiculos { get; } = new();
    public ObservableCollection<ClienteLookupItem> ClienteOptions { get; } = new();
    public ObservableCollection<string> EstadoClientes { get; } = new(new[] { "activo", "inactivo" });
    public ObservableCollection<string> EstadoVehiculos { get; } = new(new[] { "disponible", "reservado", "en_taller", "vendido" });

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
        await RefreshAllAsync("Aplicacion lista.");
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
            StatusMessage = status ?? $"Datos actualizados ({DateTime.Now:HH:mm:ss}).";
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

        Clientes.Clear();
        foreach (var cliente in clientes)
        {
            Clientes.Add(cliente);
        }

        ClienteOptions.Clear();
        ClienteOptions.Add(new ClienteLookupItem { Id = null, Display = "Sin cliente asignado" });
        foreach (var cliente in clientes)
        {
            ClienteOptions.Add(
                new ClienteLookupItem
                {
                    Id = cliente.Id,
                    Display = $"{cliente.Nombre} ({cliente.Telefono ?? "sin telefono"})",
                });
        }
    }

    private async Task LoadVehiculosAsync()
    {
        using var db = CreateDbContext();
        var vehiculos = await db.Vehiculos
            .AsNoTracking()
            .Include(x => x.Cliente)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        Vehiculos.Clear();
        foreach (var vehiculo in vehiculos)
        {
            Vehiculos.Add(
                new VehiculoGridRow
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
                });
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAllAsync();
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
                cliente = new Cliente
                {
                    CreatedAt = DateTime.UtcNow,
                };
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
                vehiculo = new Vehiculo
                {
                    CreatedAt = DateTime.UtcNow,
                };
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

    private void ClearClienteForm()
    {
        _editingClienteId = null;
        ClienteNombreInput.Text = string.Empty;
        ClienteTelefonoInput.Text = string.Empty;
        ClienteEmailInput.Text = string.Empty;
        ClienteEstadoCombo.SelectedItem = "activo";
        ClientesGrid.SelectedItem = null;
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

    private void ShowError(string message, Exception ex)
    {
        MessageBox.Show(
            $"{message}\n\nDetalle: {ex.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class VehiculoGridRow
{
    public int Id { get; init; }
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

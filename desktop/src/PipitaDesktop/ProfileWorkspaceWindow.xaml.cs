using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using PipitaDesktop.Data;
using PipitaDesktop.Services;

namespace PipitaDesktop;

public partial class ProfileWorkspaceWindow : Window
{
    private readonly AppProfilesState _state;
    private readonly AppProfile _currentProfile;
    private readonly RemoteSyncService _remoteSyncService = new();
    private List<RemoteProfileSummary> _remoteProfiles = new();
    private bool _isLoadingRemoteProfiles;

    public bool RequiresDataRefresh { get; private set; }
    public bool HeaderNeedsRefresh { get; private set; }
    public string? RequestedProfileId { get; private set; }

    public ProfileWorkspaceWindow(AppProfilesState state, AppProfile currentProfile)
    {
        InitializeComponent();
        _state = state;
        _currentProfile = currentProfile;

        ProfilesListBox.ItemsSource = _state.Profiles;
        ProfilesListBox.SelectedItem = _state.Profiles.FirstOrDefault(x => x.Id == currentProfile.Id) ?? _state.Profiles.FirstOrDefault();
        ApiBaseUrlInput.Text = _state.ApiBaseUrl;
        RemoteEmailInput.Text = _state.RemoteAuthEmail ?? currentProfile.RemoteAccountEmail ?? string.Empty;

        Loaded += ProfileWorkspaceWindow_Loaded;
    }

    private async void ProfileWorkspaceWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshHeader();
        RefreshSelectedProfileUi();
        await RefreshRemoteProfilesAsync(showSuccessMessage: false);
    }

    private AppProfile? SelectedProfile => ProfilesListBox.SelectedItem as AppProfile;

    private string? CurrentRemoteToken => string.IsNullOrWhiteSpace(_state.RemoteAccessToken) ? null : _state.RemoteAccessToken;

    private void ProfilesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RefreshSelectedProfileUi();
    }

    private void CerrarButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CrearPerfilButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var profile = ProfileManager.CreateProfile(_state, ProfileNameInput.Text, ProfilePinInput.Password);
            ProfilesListBox.Items.Refresh();
            ProfilesListBox.SelectedItem = profile;
            ProfilePinInput.Password = string.Empty;
            RemoteProfileNameInput.Text = profile.Name;
            HeaderNeedsRefresh = true;
            SetStatus($"Perfil creado: {profile.Name}.");
        }
        catch (Exception ex)
        {
            ShowWarning(ex.Message);
        }
    }

    private void RenombrarPerfilButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = RequireSelectedProfile();
        if (profile is null)
        {
            return;
        }

        try
        {
            ProfileManager.RenameProfile(_state, profile.Id, ProfileNameInput.Text);
            ProfilesListBox.Items.Refresh();
            RefreshSelectedProfileUi();
            HeaderNeedsRefresh = true;
            SetStatus($"Perfil actualizado: {profile.Name}.");
        }
        catch (Exception ex)
        {
            ShowWarning(ex.Message);
        }
    }

    private void EliminarPerfilButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = RequireSelectedProfile();
        if (profile is null)
        {
            return;
        }

        if (profile.Id == _currentProfile.Id)
        {
            ShowWarning("No se puede eliminar el perfil activo. Cambia a otro perfil primero.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Se eliminara el perfil {profile.Name} con su base local. Queres continuar?",
            "Eliminar perfil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        ProfileManager.DeleteProfile(_state, profile.Id);
        ProfilesListBox.Items.Refresh();
        ProfilesListBox.SelectedItem = _state.Profiles.FirstOrDefault(x => x.Id == _currentProfile.Id) ?? _state.Profiles.FirstOrDefault();
        HeaderNeedsRefresh = true;
        SetStatus("Perfil eliminado.");
    }

    private void CambiarPerfilButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = RequireSelectedProfile();
        if (profile is null)
        {
            return;
        }

        if (profile.Id == _currentProfile.Id)
        {
            ShowWarning("Ese perfil ya esta abierto.");
            return;
        }

        ProfileManager.SetLastProfile(_state, profile.Id);
        RequestedProfileId = profile.Id;
        HeaderNeedsRefresh = true;
        DialogResult = true;
    }

    private void GuardarPinButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = RequireSelectedProfile();
        if (profile is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ProfilePinInput.Password))
        {
            ShowWarning("Ingresa un PIN para guardarlo en el perfil seleccionado.");
            return;
        }

        ProfileManager.SetPin(_state, profile.Id, ProfilePinInput.Password);
        ProfilePinInput.Password = string.Empty;
        ProfilesListBox.Items.Refresh();
        RefreshSelectedProfileUi();
        HeaderNeedsRefresh = true;
        SetStatus("PIN guardado.");
    }

    private void QuitarPinButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = RequireSelectedProfile();
        if (profile is null)
        {
            return;
        }

        ProfileManager.ClearPin(_state, profile.Id);
        ProfilesListBox.Items.Refresh();
        RefreshSelectedProfileUi();
        HeaderNeedsRefresh = true;
        SetStatus("PIN removido.");
    }

    private void AbrirCarpetaPerfilButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = RequireSelectedProfile();
        if (profile is null)
        {
            return;
        }

        var folder = ProfileManager.GetProfileDirectory(profile);
        Directory.CreateDirectory(folder);
        OpenFolder(folder);
    }

    private void AbrirCarpetaBackupsButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = RequireSelectedProfile();
        if (profile is null)
        {
            return;
        }

        var folder = ProfileManager.GetBackupDirectory(profile);
        Directory.CreateDirectory(folder);
        OpenFolder(folder);
    }

    private void CrearBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = RequireSelectedProfile();
        if (profile is null)
        {
            return;
        }

        try
        {
            var backupDirectory = ProfileManager.GetBackupDirectory(profile);
            Directory.CreateDirectory(backupDirectory);
            var dialog = new SaveFileDialog
            {
                Title = "Guardar backup del perfil",
                Filter = "Backup ZIP (*.zip)|*.zip",
                InitialDirectory = backupDirectory,
                FileName = ProfileBackupService.BuildDefaultBackupName(profile),
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            ProfileBackupService.CreateBackup(profile, dialog.FileName);
            RefreshSelectedProfileUi();
            SetStatus($"Backup generado en {dialog.FileName}.");
        }
        catch (Exception ex)
        {
            ShowWarning(ex.Message);
        }
    }

    private void RestaurarBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = RequireSelectedProfile();
        if (profile is null)
        {
            return;
        }

        try
        {
            var backupDirectory = ProfileManager.GetBackupDirectory(profile);
            Directory.CreateDirectory(backupDirectory);
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar backup del perfil",
                Filter = "Backup ZIP (*.zip)|*.zip",
                InitialDirectory = backupDirectory,
                CheckFileExists = true,
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            ProfileBackupService.RestoreBackup(profile, dialog.FileName);
            if (profile.Id == _currentProfile.Id)
            {
                RequiresDataRefresh = true;
            }

            RefreshSelectedProfileUi();
            SetStatus("Backup restaurado correctamente.");
        }
        catch (Exception ex)
        {
            ShowWarning(ex.Message);
        }
    }

    private void GuardarApiUrlButton_Click(object sender, RoutedEventArgs e)
    {
        var apiBaseUrl = ApiBaseUrlInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            ShowWarning("La URL base de la API es obligatoria.");
            return;
        }

        _state.ApiBaseUrl = apiBaseUrl;
        ProfileManager.SaveState(_state);
        HeaderNeedsRefresh = true;
        SetStatus("URL de API guardada.");
    }

    private async void ProbarConexionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _remoteSyncService.TestConnectionAsync(ApiBaseUrlInput.Text.Trim());
            SetStatus("Conexion con la API verificada.");
        }
        catch (Exception ex)
        {
            ShowWarning(ex.Message);
        }
    }

    private async void RegistrarCuentaButton_Click(object sender, RoutedEventArgs e)
    {
        await AuthenticateAsync(() => _remoteSyncService.RegisterAsync(ApiBaseUrlInput.Text.Trim(), RemoteEmailInput.Text.Trim(), RemotePasswordInput.Password));
    }

    private async void IngresarCuentaButton_Click(object sender, RoutedEventArgs e)
    {
        await AuthenticateAsync(() => _remoteSyncService.LoginAsync(ApiBaseUrlInput.Text.Trim(), RemoteEmailInput.Text.Trim(), RemotePasswordInput.Password));
    }

    private void CerrarSesionRemotaButton_Click(object sender, RoutedEventArgs e)
    {
        _state.RemoteAccessToken = null;
        _state.RemoteAuthEmail = null;
        _state.RemoteAuthUserId = null;
        _state.RemoteAuthExpiresAtUtc = null;
        ProfileManager.SaveState(_state);
        HeaderNeedsRefresh = true;
        RefreshHeader();
        RefreshRemoteSessionUi();
        ReplaceRemoteProfiles(Array.Empty<RemoteProfileSummary>());
        SetStatus("Sesion remota cerrada.");
    }

    private async void ActualizarPerfilesRemotosButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRemoteProfilesAsync(showSuccessMessage: true);
    }

    private async void CrearPerfilRemotoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var token = RequireRemoteToken();
            if (token is null)
            {
                return;
            }

            var profileName = string.IsNullOrWhiteSpace(RemoteProfileNameInput.Text)
                ? SelectedProfile?.Name
                : RemoteProfileNameInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(profileName))
            {
                ShowWarning("Ingresa un nombre para el perfil remoto.");
                return;
            }

            var created = await _remoteSyncService.CreateProfileAsync(ApiBaseUrlInput.Text.Trim(), token, profileName);
            await RefreshRemoteProfilesAsync(showSuccessMessage: false);
            SelectRemoteProfile(created.Id);
            SetStatus($"Perfil remoto creado: {created.Name}.");
        }
        catch (Exception ex)
        {
            HandleRemoteError(ex);
        }
    }

    private void VincularPerfilRemotoButton_Click(object sender, RoutedEventArgs e)
    {
        var localProfile = RequireSelectedProfile();
        if (localProfile is null)
        {
            return;
        }

        if (RemoteProfilesCombo.SelectedItem is not RemoteProfileSummary remoteProfile)
        {
            ShowWarning("Selecciona un perfil remoto para vincularlo.");
            return;
        }

        localProfile.RemoteProfileId = remoteProfile.Id;
        localProfile.RemoteAccountEmail = _state.RemoteAuthEmail;
        localProfile.LastSnapshotRevision = remoteProfile.SnapshotRevision;
        localProfile.UpdatedAt = DateTime.UtcNow;
        ProfileManager.SaveState(_state);
        ProfilesListBox.Items.Refresh();
        RefreshSelectedProfileUi();
        HeaderNeedsRefresh = true;
        SetStatus($"Perfil local vinculado con {remoteProfile.Name}.");
    }

    private void DesvincularPerfilRemotoButton_Click(object sender, RoutedEventArgs e)
    {
        var localProfile = RequireSelectedProfile();
        if (localProfile is null)
        {
            return;
        }

        localProfile.RemoteProfileId = null;
        localProfile.RemoteAccountEmail = null;
        localProfile.LastSnapshotRevision = null;
        localProfile.UpdatedAt = DateTime.UtcNow;
        ProfileManager.SaveState(_state);
        ProfilesListBox.Items.Refresh();
        RefreshSelectedProfileUi();
        HeaderNeedsRefresh = true;
        SetStatus("Perfil remoto desvinculado.");
    }

    private async void SubirSnapshotButton_Click(object sender, RoutedEventArgs e)
    {
        var localProfile = RequireSelectedProfile();
        if (localProfile is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(localProfile.RemoteProfileId))
        {
            ShowWarning("El perfil local no tiene un perfil remoto vinculado.");
            return;
        }

        try
        {
            var token = RequireRemoteToken();
            if (token is null)
            {
                return;
            }

            var tempZip = Path.Combine(Path.GetTempPath(), $"pipita-sync-{Guid.NewGuid():N}.zip");
            try
            {
                ProfileBackupService.CreateBackup(localProfile, tempZip);
                var revision = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                var metadata = await _remoteSyncService.UploadSnapshotAsync(ApiBaseUrlInput.Text.Trim(), token, localProfile.RemoteProfileId, tempZip, revision);
                localProfile.LastSnapshotRevision = metadata.Revision;
                localProfile.RemoteAccountEmail = _state.RemoteAuthEmail;
                localProfile.UpdatedAt = DateTime.UtcNow;
                ProfileManager.SaveState(_state);
                ProfilesListBox.Items.Refresh();
                RefreshSelectedProfileUi();
                await RefreshRemoteProfilesAsync(showSuccessMessage: false);
                SetStatus($"Snapshot subido. Revision remota: {metadata.Revision ?? "sin revision"}.");
            }
            finally
            {
                if (File.Exists(tempZip))
                {
                    File.Delete(tempZip);
                }
            }
        }
        catch (Exception ex)
        {
            HandleRemoteError(ex);
        }
    }

    private async void DescargarSnapshotButton_Click(object sender, RoutedEventArgs e)
    {
        var localProfile = RequireSelectedProfile();
        if (localProfile is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(localProfile.RemoteProfileId))
        {
            ShowWarning("El perfil local no tiene un perfil remoto vinculado.");
            return;
        }

        try
        {
            var token = RequireRemoteToken();
            if (token is null)
            {
                return;
            }

            var metadata = await _remoteSyncService.GetSnapshotMetadataAsync(ApiBaseUrlInput.Text.Trim(), token, localProfile.RemoteProfileId);
            if (!metadata.HasSnapshot)
            {
                ShowWarning("Ese perfil remoto todavia no tiene un snapshot cargado.");
                return;
            }

            var tempZip = Path.Combine(Path.GetTempPath(), $"pipita-sync-download-{Guid.NewGuid():N}.zip");
            try
            {
                await _remoteSyncService.DownloadSnapshotAsync(ApiBaseUrlInput.Text.Trim(), token, localProfile.RemoteProfileId, tempZip);
                ProfileBackupService.RestoreBackup(localProfile, tempZip);
                localProfile.LastSnapshotRevision = metadata.Revision;
                localProfile.RemoteAccountEmail = _state.RemoteAuthEmail;
                localProfile.UpdatedAt = DateTime.UtcNow;
                ProfileManager.SaveState(_state);
                ProfilesListBox.Items.Refresh();
                RefreshSelectedProfileUi();
                await RefreshRemoteProfilesAsync(showSuccessMessage: false);

                if (localProfile.Id == _currentProfile.Id)
                {
                    RequiresDataRefresh = true;
                }

                SetStatus($"Snapshot descargado. Revision aplicada: {metadata.Revision ?? "sin revision"}.");
            }
            finally
            {
                if (File.Exists(tempZip))
                {
                    File.Delete(tempZip);
                }
            }
        }
        catch (Exception ex)
        {
            HandleRemoteError(ex);
        }
    }

    private void RemoteProfilesCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RefreshRemoteSelectionUi();
    }

    private async Task AuthenticateAsync(Func<Task<RemoteAuthResult>> action)
    {
        try
        {
            var auth = await action();
            _state.ApiBaseUrl = ApiBaseUrlInput.Text.Trim();
            _state.RemoteAccessToken = auth.AccessToken;
            _state.RemoteAuthEmail = auth.Email;
            _state.RemoteAuthUserId = auth.UserId;
            _state.RemoteAuthExpiresAtUtc = auth.ExpiresAtUtc;
            ProfileManager.SaveState(_state);
            HeaderNeedsRefresh = true;
            RefreshHeader();
            RefreshRemoteSessionUi();
            await RefreshRemoteProfilesAsync(showSuccessMessage: false);
            RemotePasswordInput.Password = string.Empty;
            SetStatus($"Sesion remota iniciada para {auth.Email}.");
        }
        catch (Exception ex)
        {
            HandleRemoteError(ex);
        }
    }

    private async Task RefreshRemoteProfilesAsync(bool showSuccessMessage)
    {
        if (_isLoadingRemoteProfiles)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(CurrentRemoteToken))
        {
            RefreshRemoteSessionUi();
            ReplaceRemoteProfiles(Array.Empty<RemoteProfileSummary>());
            return;
        }

        try
        {
            _isLoadingRemoteProfiles = true;
            var profiles = await _remoteSyncService.GetProfilesAsync(ApiBaseUrlInput.Text.Trim(), CurrentRemoteToken!);
            ReplaceRemoteProfiles(profiles);
            RefreshRemoteSessionUi();
            if (showSuccessMessage)
            {
                SetStatus("Perfiles remotos actualizados.");
            }
        }
        catch (Exception ex)
        {
            HandleRemoteError(ex);
        }
        finally
        {
            _isLoadingRemoteProfiles = false;
        }
    }

    private void ReplaceRemoteProfiles(IEnumerable<RemoteProfileSummary> profiles)
    {
        _remoteProfiles = profiles.OrderBy(x => x.Name).ToList();
        RemoteProfilesCombo.ItemsSource = _remoteProfiles;
        var selectedProfile = SelectedProfile;
        if (!string.IsNullOrWhiteSpace(selectedProfile?.RemoteProfileId))
        {
            SelectRemoteProfile(selectedProfile.RemoteProfileId);
            return;
        }

        RemoteProfilesCombo.SelectedItem = _remoteProfiles.FirstOrDefault();
        RefreshRemoteSelectionUi();
    }

    private void SelectRemoteProfile(string? remoteProfileId)
    {
        if (string.IsNullOrWhiteSpace(remoteProfileId))
        {
            RemoteProfilesCombo.SelectedItem = _remoteProfiles.FirstOrDefault();
            return;
        }

        RemoteProfilesCombo.SelectedItem = _remoteProfiles.FirstOrDefault(x => x.Id == remoteProfileId)
            ?? _remoteProfiles.FirstOrDefault();
    }

    private void RefreshSelectedProfileUi()
    {
        var profile = SelectedProfile;
        if (profile is null)
        {
            SelectedProfileSummaryText.Text = "Sin perfil seleccionado.";
            ProfileFolderText.Text = "-";
            ProfileDatabaseText.Text = "-";
            ProfileBackupsText.Text = "-";
            PinStatusText.Text = "-";
            LinkedRemoteText.Text = "-";
            LocalSnapshotText.Text = "-";
            RemoteSnapshotText.Text = "-";
            return;
        }

        ProfileNameInput.Text = profile.Name;
        RemoteProfileNameInput.Text = profile.Name;
        SelectedProfileSummaryText.Text = profile.Id == _currentProfile.Id
            ? $"{profile.Name} es el perfil activo. Si restauras este perfil, la app refresca al cerrar esta ventana."
            : $"{profile.Name} usa una base local separada y se puede abrir sin reiniciar la app.";
        ProfileFolderText.Text = ProfileManager.GetProfileDirectory(profile);
        ProfileDatabaseText.Text = ProfileManager.GetDatabasePath(profile);
        ProfileBackupsText.Text = ProfileManager.GetBackupDirectory(profile);
        PinStatusText.Text = profile.HasPin ? "PIN activo en este perfil." : "Sin PIN configurado.";
        LocalSnapshotText.Text = string.IsNullOrWhiteSpace(profile.LastSnapshotRevision)
            ? "Todavia no hay revision remota aplicada en este perfil."
            : $"Ultima revision conocida: {profile.LastSnapshotRevision}";
        LinkedRemoteText.Text = string.IsNullOrWhiteSpace(profile.RemoteProfileId)
            ? "Sin perfil remoto vinculado."
            : $"Vinculado a {profile.RemoteProfileId} | cuenta {_state.RemoteAuthEmail ?? profile.RemoteAccountEmail ?? "sin sesion"}";

        RefreshHeader();
        RefreshRemoteSessionUi();
        SelectRemoteProfile(profile.RemoteProfileId);
        RefreshRemoteSelectionUi();
    }

    private void RefreshHeader()
    {
        CurrentProfileHeaderText.Text = $"Perfil actual: {_currentProfile.Name}";
        CurrentRemoteHeaderText.Text = string.IsNullOrWhiteSpace(_state.RemoteAuthEmail)
            ? "Sync remoto no configurado"
            : $"Cuenta remota: {_state.RemoteAuthEmail}";
    }

    private void RefreshRemoteSessionUi()
    {
        RemoteSessionText.Text = string.IsNullOrWhiteSpace(_state.RemoteAccessToken)
            ? "Sin sesion remota activa."
            : $"Sesion activa: {_state.RemoteAuthEmail} | vence {_state.RemoteAuthExpiresAtUtc:dd/MM HH:mm}";
    }

    private void RefreshRemoteSelectionUi()
    {
        if (RemoteProfilesCombo.SelectedItem is not RemoteProfileSummary remoteProfile)
        {
            RemoteSnapshotText.Text = _remoteProfiles.Count == 0
                ? "No hay perfiles remotos cargados."
                : "Selecciona un perfil remoto para ver su estado.";
            return;
        }

        RemoteSnapshotText.Text = remoteProfile.SnapshotUploadedAtUtc.HasValue
            ? $"Revision {remoteProfile.SnapshotRevision ?? "sin revision"} | subido {remoteProfile.SnapshotUploadedAtUtc:dd/MM/yyyy HH:mm} | {FormatFileSize(remoteProfile.SnapshotSizeBytes)}"
            : "El perfil remoto existe pero todavia no tiene snapshot cargado.";
    }

    private AppProfile? RequireSelectedProfile()
    {
        if (SelectedProfile is AppProfile profile)
        {
            return profile;
        }

        ShowWarning("Selecciona un perfil primero.");
        return null;
    }

    private string? RequireRemoteToken()
    {
        if (!string.IsNullOrWhiteSpace(CurrentRemoteToken))
        {
            return CurrentRemoteToken;
        }

        ShowWarning("Inicia sesion en la API remota antes de usar sync.");
        return null;
    }

    private void HandleRemoteError(Exception ex)
    {
        if (ex.Message.Contains("sesion remota", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("autenticado", StringComparison.OrdinalIgnoreCase))
        {
            _state.RemoteAccessToken = null;
            _state.RemoteAuthExpiresAtUtc = null;
            ProfileManager.SaveState(_state);
            HeaderNeedsRefresh = true;
        }

        RefreshHeader();
        RefreshRemoteSessionUi();
        ShowWarning(ex.Message);
    }

    private void OpenFolder(string path)
    {
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
    }

    private void ShowWarning(string message)
    {
        StatusText.Text = message;
        MessageBox.Show(message, "Perfiles", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string FormatFileSize(long? sizeBytes)
    {
        if (!sizeBytes.HasValue || sizeBytes <= 0)
        {
            return "sin tamaño";
        }

        var size = sizeBytes.Value / 1024d;
        if (size < 1024)
        {
            return $"{size:0.#} KB";
        }

        return $"{size / 1024d:0.##} MB";
    }
}


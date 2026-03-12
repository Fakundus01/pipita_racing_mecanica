using System.Windows;
using PipitaDesktop.Data;

namespace PipitaDesktop;

public partial class MainWindow
{
    private string _currentProfileHeaderText = "Perfil actual";
    private string _currentProfileRemoteHeaderText = "Sync remoto no configurado";

    public string CurrentProfileHeaderText
    {
        get => _currentProfileHeaderText;
        private set => SetField(ref _currentProfileHeaderText, value);
    }

    public string CurrentProfileRemoteHeaderText
    {
        get => _currentProfileRemoteHeaderText;
        private set => SetField(ref _currentProfileRemoteHeaderText, value);
    }

    private void RefreshProfileHeader()
    {
        var activeProfile = ActiveProfileContext.CurrentProfile;
        CurrentProfileHeaderText = activeProfile is null
            ? "Perfil actual: sin perfil"
            : $"Perfil actual: {activeProfile.Name}";

        CurrentProfileRemoteHeaderText = activeProfile is null || string.IsNullOrWhiteSpace(activeProfile.RemoteProfileId)
            ? "Sync remoto no configurado"
            : $"Sync remoto: {activeProfile.RemoteAccountEmail ?? "perfil vinculado"}";
    }

    private async void PerfilButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var state = ProfileManager.LoadState();
            var currentProfile = ActiveProfileContext.CurrentProfile is null
                ? ProfileManager.TryGetLastProfile(state)
                : state.Profiles.FirstOrDefault(x => x.Id == ActiveProfileContext.CurrentProfile.Id) ?? ProfileManager.TryGetLastProfile(state);

            if (currentProfile is null)
            {
                MessageBox.Show("No se encontro un perfil local para administrar.", "Perfiles", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new ProfileWorkspaceWindow(state, currentProfile)
            {
                Owner = this,
            };

            dialog.ShowDialog();

            var refreshedState = ProfileManager.LoadState();
            if (!string.IsNullOrWhiteSpace(dialog.RequestedProfileId))
            {
                await SwitchToProfileAsync(refreshedState, dialog.RequestedProfileId);
                return;
            }

            if (ActiveProfileContext.CurrentProfile is not null)
            {
                var refreshedActive = refreshedState.Profiles.FirstOrDefault(x => x.Id == ActiveProfileContext.CurrentProfile.Id);
                if (refreshedActive is not null)
                {
                    ActiveProfileContext.SetCurrentProfile(refreshedActive);
                }
            }

            RefreshProfileHeader();

            if (dialog.RequiresDataRefresh)
            {
                await RefreshAllAsync("Perfil actualizado desde backup o sync.");
            }
            else if (dialog.HeaderNeedsRefresh)
            {
                ShowSuccessToast("Perfil actualizado.", "Perfiles");
            }
        }
        catch (Exception ex)
        {
            ShowError("No se pudo abrir la gestion de perfiles.", ex);
        }
    }

    private async Task SwitchToProfileAsync(AppProfilesState state, string profileId)
    {
        var nextProfile = ProfileManager.GetProfile(state, profileId);
        if (nextProfile.HasPin && !PromptForProfilePin(nextProfile))
        {
            ShowWarningToast("No se abrio el perfil porque el PIN no pudo validarse.", "Perfiles");
            RefreshProfileHeader();
            return;
        }

        ActiveProfileContext.SetCurrentProfile(nextProfile);
        ProfileManager.SetLastProfile(state, nextProfile.Id);
        DatabaseInitializer.EnsureCreated();
        ResetUiForProfileSwitch();
        RefreshProfileHeader();
        await RefreshAllAsync($"Perfil cargado: {nextProfile.Name}.");
    }

    private bool PromptForProfilePin(AppProfile profile)
    {
        while (true)
        {
            var pinDialog = new ProfilePinWindow(profile)
            {
                Owner = this,
            };

            if (pinDialog.ShowDialog() != true)
            {
                return false;
            }

            if (ProfileSecurity.VerifyPin(pinDialog.EnteredPin, profile.PinHash, profile.PinSalt))
            {
                return true;
            }

            var retry = MessageBox.Show(
                "El PIN no coincide. Podes reintentar o cancelar el cambio de perfil.",
                "PIN incorrecto",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (retry != MessageBoxResult.OK)
            {
                return false;
            }
        }
    }

    private void ResetUiForProfileSwitch()
    {
        ClearClienteForm();
        ClearVehiculoForm();
        ClearParteForm();
        ClearServicioForm();
        ClearReporteForm();
        ClearSolicitudForm();
        ClearDistribuidoraForm();
        ClearTrabajoDistribuidoraForm();
        ClienteHistorialVehiculos.Clear();
        ClienteHistorialSolicitudes.Clear();
        ClienteHistorialServicios.Clear();
        ClienteHistorialTrabajosDistribuidora.Clear();
    }
}

using System.Diagnostics;
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

            if (dialog.RequiresAppRestart)
            {
                RestartApplication();
            }
        }
        catch (Exception ex)
        {
            ShowError("No se pudo abrir la gestion de perfiles.", ex);
        }
    }

    private static void RestartApplication()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            executablePath = Process.GetCurrentProcess().MainModule?.FileName;
        }

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            Application.Current.Shutdown();
            return;
        }

        Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = true });
        Application.Current.Shutdown();
    }
}

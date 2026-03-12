using System.Windows;
using PipitaDesktop.Data;

namespace PipitaDesktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var selectedProfile = PromptForStartupProfile();
            if (selectedProfile is null)
            {
                Shutdown();
                return;
            }

            ActiveProfileContext.SetCurrentProfile(selectedProfile);
            var state = ProfileManager.LoadState();
            ProfileManager.SetLastProfile(state, selectedProfile.Id);
            DatabaseInitializer.EnsureCreated();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo inicializar la app.\n\n{ex.Message}",
                "Error de inicio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private static AppProfile? PromptForStartupProfile()
    {
        while (true)
        {
            var state = ProfileManager.EnsureInitializedState();
            var selector = new ProfileSelectorWindow(state);
            if (selector.ShowDialog() != true || selector.SelectedProfile is null)
            {
                return null;
            }

            var selectedProfile = selector.SelectedProfile;
            if (!selectedProfile.HasPin)
            {
                return selectedProfile;
            }

            if (PromptForPin(selectedProfile))
            {
                return selectedProfile;
            }
        }
    }

    private static bool PromptForPin(AppProfile profile)
    {
        while (true)
        {
            var pinDialog = new ProfilePinWindow(profile);
            if (pinDialog.ShowDialog() != true)
            {
                return false;
            }

            if (ProfileSecurity.VerifyPin(pinDialog.EnteredPin, profile.PinHash, profile.PinSalt))
            {
                return true;
            }

            var retry = MessageBox.Show(
                "El PIN no coincide. Podes reintentar o volver a la seleccion de perfiles.",
                "PIN incorrecto",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (retry != MessageBoxResult.OK)
            {
                return false;
            }
        }
    }
}

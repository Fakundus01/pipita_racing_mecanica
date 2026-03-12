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
            var state = ProfileManager.EnsureInitializedState();
            var selector = new ProfileSelectorWindow(state);
            if (selector.ShowDialog() != true || selector.SelectedProfile is null)
            {
                Shutdown();
                return;
            }

            var selectedProfile = selector.SelectedProfile;
            if (selectedProfile.HasPin)
            {
                var pinDialog = new ProfilePinWindow(selectedProfile);
                if (pinDialog.ShowDialog() != true || !ProfileSecurity.VerifyPin(pinDialog.EnteredPin, selectedProfile.PinHash, selectedProfile.PinSalt))
                {
                    MessageBox.Show(
                        "No se pudo validar el PIN del perfil.",
                        "PIN incorrecto",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Shutdown();
                    return;
                }
            }

            ActiveProfileContext.SetCurrentProfile(selectedProfile);
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
}

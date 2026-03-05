using System.Windows;
using PipitaDesktop.Data;

namespace PipitaDesktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            DatabaseInitializer.EnsureCreated();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo inicializar la base local.\n\n{ex.Message}",
                "Error de inicio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        base.OnStartup(e);
    }
}

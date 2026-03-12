using System.Windows;
using PipitaDesktop.Data;

namespace PipitaDesktop;

public partial class ProfilePinWindow : Window
{
    private readonly AppProfile _profile;

    public string EnteredPin => PinInput.Password;

    public ProfilePinWindow(AppProfile profile)
    {
        InitializeComponent();
        _profile = profile;
        ProfileNameText.Text = $"Perfil: {profile.Name}";
    }

    private void IngresarButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PinInput.Password))
        {
            MessageBox.Show("Ingresa el PIN del perfil.", "PIN", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }

    private void CancelarButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

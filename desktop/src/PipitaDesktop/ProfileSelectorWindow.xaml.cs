using System.Windows;
using PipitaDesktop.Data;

namespace PipitaDesktop;

public partial class ProfileSelectorWindow : Window
{
    private readonly AppProfilesState _state;

    public AppProfile? SelectedProfile { get; private set; }

    public ProfileSelectorWindow(AppProfilesState state)
    {
        InitializeComponent();
        _state = state;
        ProfilesListBox.ItemsSource = _state.Profiles;
        ProfilesListBox.SelectedItem = ProfileManager.TryGetLastProfile(_state) ?? _state.Profiles.FirstOrDefault();
        UpdateSelectedProfileText();
    }

    private void ProfilesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateSelectedProfileText();
    }

    private void AbrirPerfilButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesListBox.SelectedItem is not AppProfile profile)
        {
            MessageBox.Show("Selecciona un perfil.", "Perfiles", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SelectedProfile = profile;
        DialogResult = true;
    }

    private void CrearPerfilButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var profile = ProfileManager.CreateProfile(_state, ProfileNameInput.Text, ProfilePinInput.Password);
            ProfilesListBox.Items.Refresh();
            ProfilesListBox.SelectedItem = profile;
            ProfileNameInput.Text = string.Empty;
            ProfilePinInput.Password = string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Perfiles", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RenombrarPerfilButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesListBox.SelectedItem is not AppProfile profile)
        {
            MessageBox.Show("Selecciona un perfil para renombrar.", "Perfiles", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            ProfileManager.RenameProfile(_state, profile.Id, ProfileNameInput.Text);
            ProfilesListBox.Items.Refresh();
            UpdateSelectedProfileText();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Perfiles", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void EliminarPerfilButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesListBox.SelectedItem is not AppProfile profile)
        {
            MessageBox.Show("Selecciona un perfil para eliminar.", "Perfiles", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Se eliminara el perfil {profile.Name} con sus datos locales. Queres continuar?",
            "Eliminar perfil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        ProfileManager.DeleteProfile(_state, profile.Id);
        ProfilesListBox.Items.Refresh();
        ProfilesListBox.SelectedItem = ProfileManager.TryGetLastProfile(_state) ?? _state.Profiles.FirstOrDefault();
        UpdateSelectedProfileText();
    }

    private void CancelarButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void UpdateSelectedProfileText()
    {
        if (ProfilesListBox.SelectedItem is not AppProfile profile)
        {
            SelectedProfileText.Text = "Sin seleccion";
            return;
        }

        SelectedProfileText.Text = profile.HasPin
            ? $"{profile.Name} | PIN activo"
            : $"{profile.Name} | Sin PIN";
        if (string.IsNullOrWhiteSpace(ProfileNameInput.Text))
        {
            ProfileNameInput.Text = profile.Name;
        }
    }
}

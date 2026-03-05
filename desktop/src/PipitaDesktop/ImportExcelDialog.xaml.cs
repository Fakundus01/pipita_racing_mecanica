using System.Windows;

namespace PipitaDesktop;

public enum ImportExcelMode
{
    Merge,
    SyncExact,
}

public partial class ImportExcelDialog : Window
{
    public ImportExcelDialog(string filePath)
    {
        InitializeComponent();
        FilePathText.Text = filePath;
    }

    public ImportExcelMode ImportMode { get; private set; } = ImportExcelMode.Merge;

    private void AceptarButton_Click(object sender, RoutedEventArgs e)
    {
        ImportMode = SincronizarOption.IsChecked == true
            ? ImportExcelMode.SyncExact
            : ImportExcelMode.Merge;

        DialogResult = true;
    }

    private void CancelarButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

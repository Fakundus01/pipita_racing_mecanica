using System;
using System.IO;
using System.Windows;

namespace PipitaDesktop;

public enum ExportExcelMode
{
    ReplaceExisting,
    CreateNew,
}

public partial class ExportExcelDialog : Window
{
    public ExportExcelDialog(string destinationFolder, string suggestedFileName)
    {
        InitializeComponent();
        DestinationFolderText.Text = destinationFolder;
        FileNameInput.Text = suggestedFileName;
        Loaded += (_, _) =>
        {
            FileNameInput.Focus();
            FileNameInput.SelectAll();
        };
    }

    public string FileName { get; private set; } = string.Empty;

    public ExportExcelMode ExportMode { get; private set; } = ExportExcelMode.ReplaceExisting;

    private void AceptarButton_Click(object sender, RoutedEventArgs e)
    {
        var fileName = NormalizeExcelFileName(FileNameInput.Text);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            MessageBox.Show(
                "Ingresa un nombre de archivo.",
                "Exportar Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            MessageBox.Show(
                "El nombre del archivo contiene caracteres no validos.",
                "Exportar Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        FileName = fileName;
        ExportMode = CrearNuevoOption.IsChecked == true
            ? ExportExcelMode.CreateNew
            : ExportExcelMode.ReplaceExisting;

        DialogResult = true;
    }

    private void CancelarButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static string NormalizeExcelFileName(string? fileName)
    {
        var trimmed = fileName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        return trimmed.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed}.xlsx";
    }
}

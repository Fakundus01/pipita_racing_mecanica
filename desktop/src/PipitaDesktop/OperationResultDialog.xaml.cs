using System.Windows;

namespace PipitaDesktop;

public partial class OperationResultDialog : Window
{
    public OperationResultDialog(string title, string summary, string detail)
    {
        InitializeComponent();
        TitleText.Text = title;
        SummaryText.Text = summary;
        DetailText.Text = detail;
    }

    private void CerrarButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}

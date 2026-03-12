using System.Collections.ObjectModel;
using System.Windows.Media;

namespace PipitaDesktop;

public partial class MainWindow
{
    public ObservableCollection<AppToast> Toasts { get; } = new();

    private void ShowSuccessToast(string message, string title = "Listo", int durationMs = 3200)
    {
        ShowToast(title, message, "#FFF0F9F2", "#FFB8DEBF", "#FF155D31", durationMs);
    }

    private void ShowWarningToast(string message, string title = "Atencion", int durationMs = 4200)
    {
        ShowToast(title, message, "#FFFFF6DF", "#FFE5C86B", "#FF7B5400", durationMs);
    }

    private void ShowErrorToast(string message, string title = "Error", int durationMs = 4800)
    {
        ShowToast(title, message, "#FFFBEAEC", "#FFE4B5BB", "#FF8C2431", durationMs);
    }

    private void ShowToast(string title, string message, string backgroundHex, string borderHex, string foregroundHex, int durationMs)
    {
        var toast = new AppToast
        {
            Title = title,
            Message = message,
            Background = CreateToastBrush(backgroundHex),
            BorderBrush = CreateToastBrush(borderHex),
            Foreground = CreateToastBrush(foregroundHex),
        };

        Toasts.Add(toast);
        _ = DismissToastAsync(toast, durationMs);
    }

    private async Task DismissToastAsync(AppToast toast, int durationMs)
    {
        await Task.Delay(durationMs);
        await Dispatcher.InvokeAsync(() => Toasts.Remove(toast));
    }

    private static SolidColorBrush CreateToastBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }
}

public sealed class AppToast
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Brush Background { get; init; } = Brushes.White;
    public Brush BorderBrush { get; init; } = Brushes.Transparent;
    public Brush Foreground { get; init; } = Brushes.Black;
}

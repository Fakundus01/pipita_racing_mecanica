
using System.Diagnostics;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace PipitaDesktop;

public partial class MainWindow
{
    private async void AvisoEmailAgendaButton_Click(object sender, RoutedEventArgs e)
    {
        var cita = (sender as FrameworkElement)?.DataContext as CitaGridRow;
        var notification = await BuildAgendaNotificationAsync(cita);
        if (notification is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(notification.Email))
        {
            MessageBox.Show("El cliente no tiene email cargado.", "Aviso al cliente", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var subject = Uri.EscapeDataString(notification.Subject);
        var body = Uri.EscapeDataString(notification.Message);
        var gmailUrl = "https://mail.google.com/mail/?view=cm&fs=1&to=" + Uri.EscapeDataString(notification.Email) + "&su=" + subject + "&body=" + body;
        OpenExternalUri(gmailUrl);
        await RecordAgendaNotificationAsync(notification.CitaId, notification.Tipo, "email");
        StatusMessage = "Email preparado en Gmail para " + notification.ClienteNombre + ".";
    }

    private async void AvisoWhatsAppAgendaButton_Click(object sender, RoutedEventArgs e)
    {
        var cita = (sender as FrameworkElement)?.DataContext as CitaGridRow;
        var notification = await BuildAgendaNotificationAsync(cita);
        if (notification is null)
        {
            return;
        }

        var phone = NormalizePhoneForWhatsApp(notification.Telefono);
        if (string.IsNullOrWhiteSpace(phone))
        {
            MessageBox.Show("El cliente no tiene un telefono valido para WhatsApp.", "Aviso al cliente", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        OpenWhatsApp(phone, notification.Message);
        await RecordAgendaNotificationAsync(notification.CitaId, notification.Tipo, "whatsapp");
        StatusMessage = "WhatsApp preparado para " + notification.ClienteNombre + ".";
    }

    private async void CopiarMensajeAgendaButton_Click(object sender, RoutedEventArgs e)
    {
        var cita = (sender as FrameworkElement)?.DataContext as CitaGridRow;
        var notification = await BuildAgendaNotificationAsync(cita);
        if (notification is null)
        {
            return;
        }

        Clipboard.SetText(notification.Message);
        await RecordAgendaNotificationAsync(notification.CitaId, notification.Tipo, "copia");
        StatusMessage = "Mensaje copiado para " + notification.ClienteNombre + ".";
    }

    private async Task<AgendaNotification?> BuildAgendaNotificationAsync(CitaGridRow? cita)
    {
        if (cita is null)
        {
            MessageBox.Show("Selecciona una cita para avisar al cliente.", "Aviso al cliente", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        if (!cita.ClienteId.HasValue)
        {
            MessageBox.Show("La cita no tiene un cliente asociado.", "Aviso al cliente", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        using var db = CreateDbContext();
        var cliente = await db.Clientes.FirstOrDefaultAsync(x => x.Id == cita.ClienteId.Value);
        if (cliente is null)
        {
            MessageBox.Show("El cliente de la cita ya no existe.", "Aviso al cliente", MessageBoxButton.OK, MessageBoxImage.Information);
            await RefreshAllAsync();
            return null;
        }

        var tipo = GetAgendaNotificationType();
        var asunto = BuildAgendaNotificationSubject(tipo);
        var mensaje = BuildAgendaNotificationMessage(tipo, cliente.Nombre, cita);
        return new AgendaNotification(cita.Id, tipo, cliente.Nombre, cliente.Telefono, cliente.Email, asunto, mensaje);
    }

    private async Task RecordAgendaNotificationAsync(int citaId, string tipo, string canal)
    {
        using var db = CreateDbContext();
        var solicitud = await db.SolicitudesCliente.FirstOrDefaultAsync(x => x.Id == citaId);
        if (solicitud is null)
        {
            await RefreshAllAsync();
            return;
        }

        solicitud.UltimoAvisoTipo = tipo;
        solicitud.UltimoAvisoCanal = canal;
        solicitud.UltimoAvisoAt = DateTime.Now;
        await db.SaveChangesAsync();
        await RefreshAllAsync("Aviso registrado por " + canal + ".");
    }

    private string GetAgendaNotificationType()
    {
        var selected = AgendaNotificacionTipoCombo.SelectedItem as System.Windows.Controls.ComboBoxItem;
        return selected?.Content?.ToString() ?? "Recordatorio de cita";
    }

    private static string BuildAgendaNotificationSubject(string tipo)
    {
        return tipo switch
        {
            "Cita proxima" => "Tu cita se aproxima",
            "Auto listo" => "Tu auto ya esta listo para retirar",
            _ => "Recordatorio de cita",
        };
    }

    private static string BuildAgendaNotificationMessage(string tipo, string clienteNombre, CitaGridRow cita)
    {
        var vehiculo = string.IsNullOrWhiteSpace(cita.VehiculoNombre) ? "vehiculo" : cita.VehiculoNombre;
        var patente = string.IsNullOrWhiteSpace(cita.Patente) ? string.Empty : " patente " + cita.Patente;
        var horario = cita.FechaHoraInicio.ToString("dd/MM HH:mm");

        return tipo switch
        {
            "Cita proxima" => "Hola " + clienteNombre + ", te recordamos que tu cita para el " + vehiculo + patente + " se aproxima. Te esperamos el " + horario + ".",
            "Auto listo" => "Hola " + clienteNombre + ", te avisamos que tu " + vehiculo + patente + " ya esta listo para retirar. Cuando quieras, podes pasar por el taller para buscarlo. Gracias.",
            _ => "Hola " + clienteNombre + ", te recordamos tu cita para el " + vehiculo + patente + " el " + horario + ". Si necesitas reprogramarla, avisanos.",
        };
    }

    private static string? NormalizePhoneForWhatsApp(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return null;
        }

        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.StartsWith("0", StringComparison.Ordinal))
        {
            digits = digits[1..];
        }

        if (!digits.StartsWith("54", StringComparison.Ordinal) && digits.Length >= 10)
        {
            digits = "54" + digits;
        }

        return digits;
    }

    private static void OpenWhatsApp(string phone, string message)
    {
        var encodedMessage = Uri.EscapeDataString(message);

        try
        {
            OpenExternalUri("whatsapp://send?phone=" + phone + "&text=" + encodedMessage);
        }
        catch
        {
            OpenExternalUri("https://web.whatsapp.com/send?phone=" + phone + "&text=" + encodedMessage + "&app_absent=0");
        }
    }

    private static void OpenExternalUri(string uri)
    {
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}

internal sealed record AgendaNotification(int CitaId, string Tipo, string ClienteNombre, string? Telefono, string? Email, string Subject, string Message);


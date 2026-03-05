namespace PipitaDesktop.Models;

public sealed class SolicitudCliente : BaseEntity
{
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow.Date;
    public DateTime FechaHoraCita { get; set; } = DateTime.Today.AddHours(9);
    public int DuracionMinutos { get; set; } = 60;
    public string Estado { get; set; } = "pendiente";
    public string Prioridad { get; set; } = "media";
    public string? Canal { get; set; }
    public string? Notas { get; set; }
}

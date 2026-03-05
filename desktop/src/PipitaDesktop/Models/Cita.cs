namespace PipitaDesktop.Models;

public sealed class Cita : BaseEntity
{
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }

    public DateTime FechaHoraInicio { get; set; }
    public int DuracionMinutos { get; set; } = 60;
    public string Estado { get; set; } = "pendiente";
    public string Motivo { get; set; } = string.Empty;
    public string? Notas { get; set; }
}

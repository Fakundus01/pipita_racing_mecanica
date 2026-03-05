namespace PipitaDesktop.Models;

public sealed class TrabajoDistribuidora : BaseEntity
{
    public int DistribuidoraId { get; set; }
    public Distribuidora? Distribuidora { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow.Date;
    public decimal Costo { get; set; }
    public string EstadoPago { get; set; } = "pagado";
    public string? Notas { get; set; }
}

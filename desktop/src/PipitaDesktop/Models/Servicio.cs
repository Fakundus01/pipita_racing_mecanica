namespace PipitaDesktop.Models;

public sealed class Servicio : BaseEntity
{
    public int VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow.Date;
    public int? Kilometraje { get; set; }
    public decimal Costo { get; set; }
    public string? Notas { get; set; }
}

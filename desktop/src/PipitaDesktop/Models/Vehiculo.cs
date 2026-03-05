namespace PipitaDesktop.Models;

public sealed class Vehiculo : BaseEntity
{
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string? Version { get; set; }
    public int? Anio { get; set; }
    public string? Patente { get; set; }
    public string Estado { get; set; } = "disponible";

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
}

namespace PipitaDesktop.Models;

public sealed class Distribuidora : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Rubro { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string Estado { get; set; } = "activa";
    public string? Notas { get; set; }

    public ICollection<TrabajoDistribuidora> Trabajos { get; set; } = new List<TrabajoDistribuidora>();
}

namespace PipitaDesktop.Models;

public sealed class Reporte : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string? Periodo { get; set; }
    public DateTime GeneradoEl { get; set; } = DateTime.UtcNow.Date;
}

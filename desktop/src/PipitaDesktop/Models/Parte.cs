namespace PipitaDesktop.Models;

public sealed class Parte : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Costo { get; set; }
}

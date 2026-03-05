namespace PipitaDesktop.Models;

public sealed class Cliente : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string Estado { get; set; } = "activo";

    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    public ICollection<SolicitudCliente> Solicitudes { get; set; } = new List<SolicitudCliente>();
    public ICollection<TrabajoDistribuidora> TrabajosDistribuidora { get; set; } = new List<TrabajoDistribuidora>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}



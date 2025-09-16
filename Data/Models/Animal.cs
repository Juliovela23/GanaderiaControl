using System.ComponentModel.DataAnnotations;

namespace GanaderiaControl.Models;

public class Animal : AuditableEntity
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Arete { get; set; } = null!;   // único

    [MaxLength(120)] public string? Nombre { get; set; }
    [MaxLength(60)]  public string? Raza { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public EstadoReproductivo EstadoReproductivo { get; set; } = EstadoReproductivo.Abierta;

    public int? MadreId { get; set; }
    public Animal? Madre { get; set; }
    public int? PadreId { get; set; }
    public Animal? Padre { get; set; }

    public ICollection<ServicioReproductivo> Servicios { get; set; } = new List<ServicioReproductivo>();
    public ICollection<ChequeoGestacion> Chequeos { get; set; } = new List<ChequeoGestacion>();
    public ICollection<Secado> Secados { get; set; } = new List<Secado>();
    public ICollection<Parto> Partos { get; set; } = new List<Parto>();
    public ICollection<Lactancia> Lactancias { get; set; } = new List<Lactancia>();
    public ICollection<RegistroLeche> RegistrosLeche { get; set; } = new List<RegistroLeche>();
    public ICollection<EventoSalud> EventosSalud { get; set; } = new List<EventoSalud>();
    public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
}

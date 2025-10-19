using System.ComponentModel.DataAnnotations;

namespace GanaderiaControl.Models;

public class Cria : AuditableEntity
{
    public int Id { get; set; }
    public int PartoId { get; set; }
    public Parto Parto { get; set; } = null!;
    public SexoCria Sexo { get; set; }
    public decimal? PesoNacimientoKg { get; set; }
    [MaxLength(50)]  public string? AreteAsignado { get; set; }
    [MaxLength(120)] public string? Estado { get; set; } // vivo, muerto al nacer...
    [MaxLength(240)] public string? Observaciones { get; set; }

    public string? userId { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace GanaderiaControl.Models;

public class Lactancia : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal? ProduccionPromedioDiaLitros { get; set; }
    [MaxLength(240)] public string? Observaciones { get; set; }
    public string? UserId { get; set; }

}

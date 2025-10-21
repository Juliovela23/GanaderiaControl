using System.ComponentModel.DataAnnotations;

namespace GanaderiaControl.Models;

public class Secado : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public DateTime FechaSecado { get; set; }

    [MaxLength(240)] public string? Motivo { get; set; }
    [MaxLength(240)] public string? Observaciones { get; set; }
    public string? UserId { get; set; }

}

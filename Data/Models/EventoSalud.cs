using System.ComponentModel.DataAnnotations;

namespace GanaderiaControl.Models;

public class EventoSalud : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public DateTime Fecha { get; set; }
    [MaxLength(160)] public string Diagnostico { get; set; } = null!;
    [MaxLength(240)] public string? Tratamiento { get; set; }
    [MaxLength(240)] public string? Restricciones { get; set; } // retiro leche/carne
}

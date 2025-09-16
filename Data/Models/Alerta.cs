using System.ComponentModel.DataAnnotations;

namespace GanaderiaControl.Models;

public class Alerta : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public TipoAlerta Tipo { get; set; }
    public DateTime FechaObjetivo { get; set; }
    public EstadoAlerta Estado { get; set; } = EstadoAlerta.Pendiente;
    [MaxLength(240)] public string? Disparador { get; set; }
    [MaxLength(240)] public string? Notas { get; set; }
}

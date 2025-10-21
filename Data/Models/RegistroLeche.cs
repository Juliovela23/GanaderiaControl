namespace GanaderiaControl.Models;

public class RegistroLeche : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public decimal LitrosDia { get; set; }
    public string? UserId { get; set; }



}

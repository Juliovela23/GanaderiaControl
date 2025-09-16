using System.ComponentModel.DataAnnotations;

namespace GanaderiaControl.Models;

public class ChequeoGestacion : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public DateTime FechaChequeo { get; set; }
    public ResultadoGestacion Resultado { get; set; } = ResultadoGestacion.NoDeterminado;

    [MaxLength(120)] public string? Metodo { get; set; } // palpación/ecografía
    [MaxLength(240)] public string? Observaciones { get; set; }

    public int? ServicioReproductivoId { get; set; }
    public ServicioReproductivo? ServicioReproductivo { get; set; }
}

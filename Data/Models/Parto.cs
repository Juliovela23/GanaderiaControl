    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace GanaderiaControl.Models;

    public class Parto : AuditableEntity
    {
        public int Id { get; set; }
        public int MadreId { get; set; }
        [ValidateNever]
        public Animal? Madre { get; set; } = null!;
        public DateTime FechaParto { get; set; }
        public TipoParto TipoParto { get; set; } = TipoParto.Normal;
        public bool RetencionPlacenta { get; set; }

        [MaxLength(240)] public string? Asistencia { get; set; }
        [MaxLength(240)] public string? Observaciones { get; set; }
        public string? Complicaciones { get; set; }
        public string? userId { get; set; }
        public ICollection<Cria> Crias { get; set; } = new List<Cria>();
    }

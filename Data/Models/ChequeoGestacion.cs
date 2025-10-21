using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GanaderiaControl.Models
{
    // using System.ComponentModel.DataAnnotations.Schema;  (ya lo tienes)
    public class ChequeoGestacion
    {
        public int Id { get; set; }

        [Required]
        public int AnimalId { get; set; }
        public Animal? Animal { get; set; }

        [DataType(DataType.Date)]
        [Required]
        public DateTime FechaChequeo { get; set; }

        [Required]
        public ResultadoGestacion Resultado { get; set; } = ResultadoGestacion.NoDeterminado;

        [StringLength(250)]
        public string? Observaciones { get; set; }

        // 🔹 NUEVO: referencia opcional al último servicio reproductivo
        public int? ServicioReproductivoId { get; set; }
        public ServicioReproductivo? ServicioReproductivo { get; set; }

        // Auditoría / soft-delete
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UserId { get; set; }
    }

}

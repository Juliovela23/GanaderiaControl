using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GanaderiaControl.Models
{
    public class ServicioReproductivo
    {
        public int Id { get; set; }

        [Display(Name = "Animal")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un animal.")]
        public int AnimalId { get; set; }

        [ValidateNever]
        public Animal? Animal { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de servicio")]
        public DateTime FechaServicio { get; set; }

        [Required]
        [Display(Name = "Tipo")]
        public TipoServicio Tipo { get; set; }

        [Display(Name = "Toro / Proveedor")]
        public string? ToroOProveedor { get; set; }

        public string? Observaciones { get; set; }

        // 👇 Auditoría / Soft-delete
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

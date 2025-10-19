using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using GanaderiaControl.Models;

namespace GanaderiaControl.Models.ViewModels
{
    public class PartoCreateVM
    {
        // ===== PARTO =====
        [Display(Name = "Madre"), Required, Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar la madre.")]
        public int MadreId { get; set; }

        [Display(Name = "Fecha de parto"), DataType(DataType.Date), Required]
        public DateTime FechaParto { get; set; } = DateTime.Today;

        [Display(Name = "Tipo de parto")]
        public TipoParto TipoParto { get; set; } = TipoParto.Normal;

        [Display(Name = "Retención de placenta")]
        public bool RetencionPlacenta { get; set; }

        [MaxLength(240)] public string? Asistencia { get; set; }
        [MaxLength(240)] public string? Observaciones { get; set; }
        public string? Complicaciones { get; set; }

        // ===== CRÍA (opcional) =====
        public bool CrearCria { get; set; } = true;

        [Display(Name = "Sexo")]
        public SexoCria? CriaSexo { get; set; }

        [Display(Name = "Peso nacimiento (kg)")]
        [Range(0, 999, ErrorMessage = "Peso inválido.")]
        public decimal? CriaPesoNacimientoKg { get; set; }

        [Display(Name = "Arete asignado"), MaxLength(50)]
        public string? CriaAreteAsignado { get; set; }

        [Display(Name = "Estado"), MaxLength(120)]
        public string? CriaEstado { get; set; } // vivo, muerto al nacer...

        [Display(Name = "Obs. Cría"), MaxLength(240)]
        public string? CriaObservaciones { get; set; }

        public bool RegistrarCriaComoAnimal { get; set; } = true;

        // Navegación solo para mostrar si se quiere (no se postea)
        [ValidateNever]
        public Animal? Madre { get; set; }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GanaderiaControl.Models.ViewModels
{
    public class RegistroLecheCreateVM
    {
        public RegistroLeche Registro { get; set; } = new RegistroLeche { Fecha = DateTime.Today };
        public IEnumerable<SelectListItem> Animales { get; set; } = new List<SelectListItem>();
    }
}

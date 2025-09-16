using System;
using System.Collections.Generic;

namespace GanaderiaControl.Models.ViewModels
{
    public class DashboardViewModel
    {
        // KPIs
        public int TotalAnimales { get; set; }
        public int Gestantes { get; set; }
        public int ServiciosMes { get; set; }
        public int AlertasPendientes { get; set; }
        public int AlertasHoyPendientes { get; set; }
        public int AlertasSemanaPendientes { get; set; }
        public int AlertasVencidas { get; set; }
        public decimal ProduccionHoyLitros { get; set; }

        // Listas
        public List<AlertaItem> ProximasAlertas { get; set; } = new();
        public List<ServicioItem> UltimosServicios { get; set; } = new();
        public List<PartoItem> PartosRecientes { get; set; } = new();

        public class AlertaItem
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public string Animal { get; set; } = "";
            public string Tipo { get; set; } = "";
            public string Estado { get; set; } = "";
            public string? Notas { get; set; }
        }

        public class ServicioItem
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public string Animal { get; set; } = "";
            public string Tipo { get; set; } = "";
        }

        public class PartoItem
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public string Madre { get; set; } = "";
            public string TipoParto { get; set; } = "";
        }
    }
}
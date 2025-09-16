using System;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using GanaderiaControl.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GanaderiaControl.Controllers
{
    [Authorize] // requiere login
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var weekEnd = today.AddDays(7);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var vm = new DashboardViewModel
            {
                TotalAnimales = await _db.Animales.CountAsync(),
                Gestantes = await _db.Animales.CountAsync(a => a.EstadoReproductivo == EstadoReproductivo.Gestante),
                ServiciosMes = await _db.Servicios.CountAsync(s => s.FechaServicio >= monthStart && s.FechaServicio <= today),
                AlertasPendientes = await _db.Alertas.CountAsync(a => a.Estado == EstadoAlerta.Pendiente),
                AlertasHoyPendientes = await _db.Alertas.CountAsync(a => a.Estado == EstadoAlerta.Pendiente && a.FechaObjetivo == today),
                AlertasSemanaPendientes = await _db.Alertas.CountAsync(a => a.Estado == EstadoAlerta.Pendiente && a.FechaObjetivo > today && a.FechaObjetivo <= weekEnd),
                AlertasVencidas = await _db.Alertas.CountAsync(a => a.Estado == EstadoAlerta.Pendiente && a.FechaObjetivo < today),
                ProduccionHoyLitros = await _db.RegistrosLeche.Where(r => r.Fecha == today).SumAsync(r => (decimal?)r.LitrosDia) ?? 0m
            };

            vm.ProximasAlertas = await _db.Alertas
                .Where(a => a.Estado == EstadoAlerta.Pendiente && a.FechaObjetivo >= today)
                .OrderBy(a => a.FechaObjetivo).ThenBy(a => a.Animal.Arete)
                .Take(10)
                .Select(a => new DashboardViewModel.AlertaItem {
                    Id = a.Id,
                    Fecha = a.FechaObjetivo,
                    Animal = a.Animal.Arete + (a.Animal.Nombre != null ? " - " + a.Animal.Nombre : ""),
                    Tipo = a.Tipo.ToString(),
                    Estado = a.Estado.ToString(),
                    Notas = a.Notas
                })
                .ToListAsync();

            vm.UltimosServicios = await _db.Servicios
                .OrderByDescending(s => s.FechaServicio).ThenByDescending(s => s.Id)
                .Take(10)
                .Select(s => new DashboardViewModel.ServicioItem {
                    Id = s.Id,
                    Fecha = s.FechaServicio,
                    Animal = s.Animal.Arete + (s.Animal.Nombre != null ? " - " + s.Animal.Nombre : ""),
                    Tipo = s.Tipo.ToString()
                }).ToListAsync();

            vm.PartosRecientes = await _db.Partos
                .OrderByDescending(p => p.FechaParto).ThenByDescending(p => p.Id)
                .Take(10)
                .Select(p => new DashboardViewModel.PartoItem {
                    Id = p.Id,
                    Fecha = p.FechaParto,
                    Madre = p.Madre.Arete + (p.Madre.Nombre != null ? " - " + p.Madre.Nombre : ""),
                    TipoParto = p.TipoParto.ToString()
                }).ToListAsync();

            return View(vm);
        }
    }
}
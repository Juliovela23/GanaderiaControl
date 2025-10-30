using System;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using GanaderiaControl.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GanaderiaControl.Controllers
{
    [Authorize] // requiere login
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User); // usuario actual
            var today = DateTime.Today;
            var weekEnd = today.AddDays(7);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // --------- MÉTRICAS (FILTRADAS POR USUARIO) ----------
            var vm = new DashboardViewModel
            {
                // OJO: Animal tiene 'userId' (minúscula)
                TotalAnimales = await _db.Animales
                    .CountAsync(a => !a.IsDeleted && a.userId == userId),

                Gestantes = await _db.Animales
                    .CountAsync(a => !a.IsDeleted
                                  && a.userId == userId
                                  && a.EstadoReproductivo == EstadoReproductivo.Gestante),

                // ServicioReproductivo tiene 'UserId' (mayúscula)
                ServiciosMes = await _db.Servicios
                    .CountAsync(s => !s.IsDeleted
                                  && s.UserId == userId
                                  && s.FechaServicio >= monthStart
                                  && s.FechaServicio <= today),

                // --------- ALERTAS (por destinatario) ----------
                AlertasPendientes = await _db.Alertas
                    .CountAsync(a => !a.IsDeleted
                                  && a.Estado == EstadoAlerta.Pendiente
                                  && a.DestinatarioUserId == userId),

                AlertasHoyPendientes = await _db.Alertas
                    .CountAsync(a => !a.IsDeleted
                                  && a.Estado == EstadoAlerta.Pendiente
                                  && a.FechaObjetivo == today
                                  && a.DestinatarioUserId == userId),

                AlertasSemanaPendientes = await _db.Alertas
                    .CountAsync(a => !a.IsDeleted
                                  && a.Estado == EstadoAlerta.Pendiente
                                  && a.FechaObjetivo > today
                                  && a.FechaObjetivo <= weekEnd
                                  && a.DestinatarioUserId == userId),

                AlertasVencidas = await _db.Alertas
                    .CountAsync(a => !a.IsDeleted
                                  && a.Estado == EstadoAlerta.Pendiente
                                  && a.FechaObjetivo < today
                                  && a.DestinatarioUserId == userId),

                // RegistrosLeche tiene 'UserId' (mayúscula)
                ProduccionHoyLitros = await _db.RegistrosLeche
                    .Where(r => r.UserId == userId && r.Fecha == today)
                    .SumAsync(r => (decimal?)r.LitrosDia) ?? 0m
            };

            // --------- Próximas alertas (por destinatario + no borradas) ----------
            vm.ProximasAlertas = await _db.Alertas
                .AsNoTracking()
                .Where(a => !a.IsDeleted
                         && a.Estado == EstadoAlerta.Pendiente
                         && a.FechaObjetivo >= today
                         && a.DestinatarioUserId == userId)
                .Include(a => a.Animal)
                .Where(a => a.Animal != null && !a.Animal.IsDeleted)
                .OrderBy(a => a.FechaObjetivo).ThenBy(a => a.Animal.Arete)
                .Take(10)
                .Select(a => new DashboardViewModel.AlertaItem
                {
                    Id = a.Id,
                    Fecha = a.FechaObjetivo,
                    Animal = a.Animal.Arete + (a.Animal.Nombre != null ? " - " + a.Animal.Nombre : ""),
                    Tipo = a.Tipo.ToString(),
                    Estado = a.Estado.ToString(),
                    Notas = a.Notas
                })
                .ToListAsync();

            // --------- Últimos servicios (FILTRADO POR USUARIO) ----------
            vm.UltimosServicios = await _db.Servicios
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.UserId == userId)
                .Include(s => s.Animal)
                .Where(s => s.Animal != null && !s.Animal.IsDeleted && s.Animal.userId == userId)
                .OrderByDescending(s => s.FechaServicio).ThenByDescending(s => s.Id)
                .Take(10)
                .Select(s => new DashboardViewModel.ServicioItem
                {
                    Id = s.Id,
                    Fecha = s.FechaServicio,
                    Animal = s.Animal.Arete + (s.Animal.Nombre != null ? " - " + s.Animal.Nombre : ""),
                    Tipo = s.Tipo.ToString()
                })
                .ToListAsync();

            // --------- Partos recientes (FILTRADO POR USUARIO) ----------
            vm.PartosRecientes = await _db.Partos
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.userId == userId) // Parto tiene 'userId' (minúscula)
                .Include(p => p.Madre)
                .Where(p => p.Madre != null && !p.Madre.IsDeleted && p.Madre.userId == userId)
                .OrderByDescending(p => p.FechaParto).ThenByDescending(p => p.Id)
                .Take(10)
                .Select(p => new DashboardViewModel.PartoItem
                {
                    Id = p.Id,
                    Fecha = p.FechaParto,
                    Madre = p.Madre.Arete + (p.Madre.Nombre != null ? " - " + p.Madre.Nombre : ""),
                    TipoParto = p.TipoParto.ToString()
                })
                .ToListAsync();

            // ================== SERIES PARA GRÁFICOS (FILTRADAS POR USUARIO) ==================
            var weekStart = today.AddDays(-6);

            var semanaQuery = await _db.RegistrosLeche
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Fecha >= weekStart && r.Fecha <= today)
                .GroupBy(r => r.Fecha)
                .Select(g => new { Fecha = g.Key, Litros = g.Sum(x => x.LitrosDia) })
                .ToListAsync();

            var semanaLabels = Enumerable.Range(0, 7)
                .Select(i => weekStart.AddDays(i))
                .Select(d => d.ToString("dd/MM"))
                .ToArray();

            var semanaData = Enumerable.Range(0, 7)
                .Select(i => weekStart.AddDays(i))
                .Select(d => semanaQuery.FirstOrDefault(x => x.Fecha == d)?.Litros ?? 0m)
                .ToArray();

            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var monthEnd = monthStart.AddDays(daysInMonth - 1);

            var mesQuery = await _db.RegistrosLeche
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Fecha >= monthStart && r.Fecha <= monthEnd)
                .GroupBy(r => r.Fecha)
                .Select(g => new { Fecha = g.Key, Litros = g.Sum(x => x.LitrosDia) })
                .ToListAsync();

            var mesLabels = Enumerable.Range(0, daysInMonth)
                .Select(i => monthStart.AddDays(i).Day.ToString("00"))
                .ToArray();

            var mesData = Enumerable.Range(0, daysInMonth)
                .Select(i => monthStart.AddDays(i))
                .Select(d => mesQuery.FirstOrDefault(x => x.Fecha == d)?.Litros ?? 0m)
                .ToArray();

            var yearStart = new DateTime(today.Year, 1, 1);
            var yearEnd = new DateTime(today.Year, 12, 31);

            var anioQuery = await _db.RegistrosLeche
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.Fecha >= yearStart && r.Fecha <= yearEnd)
                .GroupBy(r => new { r.Fecha.Year, r.Fecha.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Litros = g.Sum(x => x.LitrosDia) })
                .ToListAsync();

            string[] mesesCortos = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
            var anioLabels = Enumerable.Range(1, 12).Select(m => mesesCortos[m - 1]).ToArray();
            var anioData = Enumerable.Range(1, 12)
                .Select(m => anioQuery.FirstOrDefault(x => x.Year == today.Year && x.Month == m)?.Litros ?? 0m)
                .ToArray();

            vm.LecheSemanaLabels = semanaLabels;
            vm.LecheSemanaData = semanaData;
            vm.LecheMesLabels = mesLabels;
            vm.LecheMesData = mesData;
            vm.LecheAnioLabels = anioLabels;
            vm.LecheAnioData = anioData;

            return View(vm);
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GanaderiaControl.Controllers
{
    [Authorize]
    public class AlertasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AlertasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================= LISTADO =======================
        // GET: /Alertas
        // Filtros: q (arete/nombre), estado, desde, hasta, proximos
        public async Task<IActionResult> Index(string? q, EstadoAlerta? estado, DateTime? desde, DateTime? hasta, bool proximos = true)
        {
            // 1) Marcar como "Vencida" lo pendiente cuya fecha ya pasó
            await ActualizarVencidasAsync();

            // 2) Query base
            var query = _context.Alertas
                .Include(a => a.Animal)
                .AsNoTracking()
                .AsQueryable();

            // Filtro texto
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(a =>
                    a.Animal.Arete.Contains(q) ||
                    (a.Animal.Nombre != null && a.Animal.Nombre.Contains(q)));
            }

            // Filtro estado
            if (estado.HasValue)
                query = query.Where(a => a.Estado == estado);

            // Rango por defecto: próximas ~5 semanas (solo pendientes)
            if (proximos && !desde.HasValue && !hasta.HasValue)
            {
                var start = DateTime.Today.AddDays(-3);   // pequeño buffer
                var end = DateTime.Today.AddDays(35);   // <- antes 28; ahora 35 para incluir la alerta de +32 días
                query = query.Where(a =>
                    a.FechaObjetivo >= start &&
                    a.FechaObjetivo <= end &&
                    a.Estado == EstadoAlerta.Pendiente);
            }
            else
            {
                if (desde.HasValue) query = query.Where(a => a.FechaObjetivo >= desde.Value.Date);
                if (hasta.HasValue) query = query.Where(a => a.FechaObjetivo <= hasta.Value.Date);
            }

            query = query.OrderBy(a => a.FechaObjetivo).ThenBy(a => a.Animal.Arete);

            // Pasar valores a la vista
            ViewData["q"] = q;
            ViewData["estado"] = estado;
            ViewData["desde"] = desde?.ToString("yyyy-MM-dd");
            ViewData["hasta"] = hasta?.ToString("yyyy-MM-dd");
            ViewData["proximos"] = proximos;

            var list = await query.ToListAsync();
            return View(list);
        }

        // ======================= DETALLE =======================
        // GET: /Alertas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var alerta = await _context.Alertas
                .Include(a => a.Animal)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (alerta == null) return NotFound();
            return View(alerta);
        }

        // ======================= CREAR (manual) =======================
        // Útil para TipoAlerta.Salud u otras alertas manuales
        // GET: /Alertas/Create
        public async Task<IActionResult> Create()
        {
            await PopulateAnimalesSelectAsync();
            return View(new Alerta
            {
                FechaObjetivo = DateTime.Today,
                Estado = EstadoAlerta.Pendiente,
                Tipo = TipoAlerta.Salud
            });
        }

        // POST: /Alertas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,Tipo,FechaObjetivo,Estado,Disparador,Notas")] Alerta alerta)
        {
            alerta.FechaObjetivo = alerta.FechaObjetivo.Date;

            // Evitar duplicados (AnimalId + Tipo + FechaObjetivo)
            if (await ExisteDuplicada(alerta))
                ModelState.AddModelError(string.Empty, "Ya existe una alerta del mismo tipo y fecha para este animal.");

            if (!ModelState.IsValid)
            {
                await PopulateAnimalesSelectAsync(alerta.AnimalId);
                return View(alerta);
            }

            alerta.CreatedAt = DateTime.UtcNow;
            _context.Alertas.Add(alerta);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ======================= EDITAR =======================
        // GET: /Alertas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var alerta = await _context.Alertas.FindAsync(id);
            if (alerta == null) return NotFound();
            await PopulateAnimalesSelectAsync(alerta.AnimalId);
            return View(alerta);
        }

        // POST: /Alertas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AnimalId,Tipo,FechaObjetivo,Estado,Disparador,Notas")] Alerta alerta)
        {
            if (id != alerta.Id) return NotFound();

            alerta.FechaObjetivo = alerta.FechaObjetivo.Date;

            // Evitar duplicados cuando cambian claves únicas
            if (await ExisteDuplicada(alerta, excluirId: id))
                ModelState.AddModelError(string.Empty, "Ya existe una alerta del mismo tipo y fecha para este animal.");

            if (!ModelState.IsValid)
            {
                await PopulateAnimalesSelectAsync(alerta.AnimalId);
                return View(alerta);
            }

            var entity = await _context.Alertas.FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return NotFound();

            entity.AnimalId = alerta.AnimalId;
            entity.Tipo = alerta.Tipo;
            entity.FechaObjetivo = alerta.FechaObjetivo;
            entity.Estado = alerta.Estado;
            entity.Disparador = alerta.Disparador;
            entity.Notas = alerta.Notas;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ======================= ELIMINAR (soft) =======================
        // GET: /Alertas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var alerta = await _context.Alertas
                .Include(a => a.Animal)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (alerta == null) return NotFound();
            return View(alerta);
        }

        // POST: /Alertas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var alerta = await _context.Alertas.FirstOrDefaultAsync(a => a.Id == id);
            if (alerta == null) return NotFound();
            alerta.IsDeleted = true;       // respeta el query filter de soft-delete
            alerta.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ======================= ACCIONES RÁPIDAS =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarNotificada(int id)
        {
            var a = await _context.Alertas.FindAsync(id);
            if (a == null) return NotFound();
            a.Estado = EstadoAlerta.Notificada;
            a.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarAtendida(int id)
        {
            var a = await _context.Alertas.FindAsync(id);
            if (a == null) return NotFound();
            a.Estado = EstadoAlerta.Atendida;
            a.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reabrir(int id)
        {
            var a = await _context.Alertas.FindAsync(id);
            if (a == null) return NotFound();
            a.Estado = EstadoAlerta.Pendiente;
            a.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ======================= HELPERS =======================
        private async Task PopulateAnimalesSelectAsync(int? animalId = null)
        {
            var animales = await _context.Animales
                .AsNoTracking()
                .OrderBy(a => a.Arete)
                .Select(a => new
                {
                    a.Id,
                    Texto = a.Arete + (string.IsNullOrWhiteSpace(a.Nombre) ? "" : " - " + a.Nombre)
                })
                .ToListAsync();

            ViewData["AnimalId"] = new SelectList(animales, "Id", "Texto", animalId);
        }

        private async Task<bool> ExisteDuplicada(Alerta alerta, int? excluirId = null)
        {
            var q = _context.Alertas.AsQueryable();
            q = q.Where(x =>
                x.AnimalId == alerta.AnimalId &&
                x.Tipo == alerta.Tipo &&
                x.FechaObjetivo == alerta.FechaObjetivo);

            if (excluirId.HasValue)
                q = q.Where(x => x.Id != excluirId.Value);

            return await q.AnyAsync();
        }

        private async Task ActualizarVencidasAsync()
        {
            var hoy = DateTime.Today;
            var vencidas = await _context.Alertas
                .Where(a => a.Estado == EstadoAlerta.Pendiente && a.FechaObjetivo < hoy)
                .ToListAsync();

            if (vencidas.Count == 0) return;

            foreach (var a in vencidas)
            {
                a.Estado = EstadoAlerta.Vencida;
                a.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }
    }
}

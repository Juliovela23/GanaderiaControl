using System;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GanaderiaControl.Controllers
{
    public class AlertasController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AlertasController(ApplicationDbContext context) => _context = context;

        // LISTADO
        public async Task<IActionResult> Index(string? q, EstadoAlerta? estado, DateTime? desde, DateTime? hasta, bool proximos = true)
        {
            await ActualizarVencidasAsync();

            var query = _context.Alertas
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .Include(a => a.Animal)
                .Where(a => a.Animal != null && !a.Animal.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(a =>
                    (a.Animal.Arete != null && a.Animal.Arete.Contains(q)) ||
                    (a.Animal.Nombre != null && a.Animal.Nombre.Contains(q)));
            }

            if (estado.HasValue)
                query = query.Where(a => a.Estado == estado);

            if (proximos && !desde.HasValue && !hasta.HasValue)
            {
                var start = DateTime.Today.AddDays(-3);
                var end = DateTime.Today.AddDays(35);
                query = query.Where(a => a.FechaObjetivo >= start && a.FechaObjetivo <= end && a.Estado == EstadoAlerta.Pendiente);
            }
            else
            {
                if (desde.HasValue) query = query.Where(a => a.FechaObjetivo >= desde.Value.Date);
                if (hasta.HasValue) query = query.Where(a => a.FechaObjetivo <= hasta.Value.Date);
            }

            query = query.OrderBy(a => a.FechaObjetivo).ThenBy(a => a.Animal.Arete);

            ViewData["q"] = q;
            ViewData["estado"] = estado;
            ViewData["desde"] = desde?.ToString("yyyy-MM-dd");
            ViewData["hasta"] = hasta?.ToString("yyyy-MM-dd");
            ViewData["proximos"] = proximos;

            var list = await query.ToListAsync();
            return View(list);
        }

        // DETALLE
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var alerta = await _context.Alertas
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .Include(a => a.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (alerta == null || alerta.Animal == null || alerta.Animal.IsDeleted) return NotFound();
            return View(alerta);
        }

        // CREATE GET
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

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,Tipo,FechaObjetivo,Estado,Disparador,Notas")] Alerta alerta)
        {
            var animalOk = await _context.Animales.AnyAsync(a => a.Id == alerta.AnimalId && !a.IsDeleted);
            if (!animalOk)
                ModelState.AddModelError(nameof(alerta.AnimalId), "Animal inválido.");

            alerta.FechaObjetivo = alerta.FechaObjetivo.Date;

            if (await ExisteDuplicada(alerta))
                ModelState.AddModelError(string.Empty, "Ya existe una alerta del mismo tipo y fecha para este animal.");

            if (!ModelState.IsValid)
            {
                await PopulateAnimalesSelectAsync(alerta.AnimalId);
                return View(alerta);
            }

            try
            {
                alerta.CreatedAt = DateTime.UtcNow;
                _context.Alertas.Add(alerta);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                ModelState.AddModelError(string.Empty, "No se pudo guardar: " + pex.MessageText);
                await PopulateAnimalesSelectAsync(alerta.AnimalId);
                return View(alerta);
            }
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var alerta = await _context.Alertas.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (alerta == null) return NotFound();

            await PopulateAnimalesSelectAsync(alerta.AnimalId);
            return View(alerta);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AnimalId,Tipo,FechaObjetivo,Estado,Disparador,Notas")] Alerta alerta)
        {
            if (id != alerta.Id) return NotFound();

            var animalOk = await _context.Animales.AnyAsync(a => a.Id == alerta.AnimalId && !a.IsDeleted);
            if (!animalOk)
                ModelState.AddModelError(nameof(alerta.AnimalId), "Animal inválido.");

            alerta.FechaObjetivo = alerta.FechaObjetivo.Date;

            if (await ExisteDuplicada(alerta, excluirId: id))
                ModelState.AddModelError(string.Empty, "Ya existe una alerta del mismo tipo y fecha para este animal.");

            if (!ModelState.IsValid)
            {
                await PopulateAnimalesSelectAsync(alerta.AnimalId);
                return View(alerta);
            }

            try
            {
                var entity = await _context.Alertas.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
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
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                ModelState.AddModelError(string.Empty, "Error al guardar: " + pex.MessageText);
                await PopulateAnimalesSelectAsync(alerta.AnimalId);
                return View(alerta);
            }
        }

        // DELETE GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var alerta = await _context.Alertas
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .Include(a => a.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (alerta == null || alerta.Animal == null || alerta.Animal.IsDeleted) return NotFound();
            return View(alerta);
        }

        // DELETE POST (soft)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var alerta = await _context.Alertas.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (alerta == null) return NotFound();

            alerta.IsDeleted = true;
            alerta.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // HELPERS
        private async Task PopulateAnimalesSelectAsync(int? animalId = null)
        {
            var animales = await _context.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
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
            var q = _context.Alertas.Where(x =>
                !x.IsDeleted &&
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
                .Where(a => !a.IsDeleted && a.Estado == EstadoAlerta.Pendiente && a.FechaObjetivo < hoy)
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

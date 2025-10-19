using System;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;                // Ajusta si tu namespace del DbContext es otro
using GanaderiaControl.Models;             // Ajusta si corresponde
using Microsoft.AspNetCore.Authorization;  // Opcional si usas [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GanaderiaControl.Controllers
{
    // [Authorize] // Descomenta si manejas auth
    public class RegistroLecheController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegistroLecheController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================= LISTADO =======================
        // GET: /RegistroLeche
        // Filtros: q (Arete/Nombre de la vaca), animalId, desde, hasta
        public async Task<IActionResult> Index(string? q, int? animalId, DateTime? desde, DateTime? hasta)
        {
            var query = _context.RegistrosLeche
                .Include(r => r.Animal)
                .AsQueryable();

            if (animalId.HasValue)
                query = query.Where(r => r.AnimalId == animalId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                string term = q.Trim().ToUpper();
                query = query.Where(r =>
                    (r.Animal.Arete != null && r.Animal.Arete.ToUpper().Contains(term)) ||
                    (r.Animal.Nombre != null && r.Animal.Nombre.ToUpper().Contains(term)));
            }

            if (desde.HasValue)
                query = query.Where(r => r.Fecha >= desde.Value.Date);
            if (hasta.HasValue)
            {
                var h = hasta.Value.Date.AddDays(1).AddTicks(-1); // fin del día
                query = query.Where(r => r.Fecha <= h);
            }

            // Orden más útil: fecha desc, luego arete
            var items = await query
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.Animal.Arete)
                .ToListAsync();

            // Para filtros en la vista
            ViewBag.Animales = await _context.Animales
                .OrderBy(a => a.Arete)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(a.Nombre) ? a.Arete : $"{a.Arete} - {a.Nombre}"
                })
                .ToListAsync();

            ViewBag.FiltroQ = q;
            ViewBag.FiltroAnimalId = animalId;
            ViewBag.FiltroDesde = desde?.ToString("yyyy-MM-dd");
            ViewBag.FiltroHasta = hasta?.ToString("yyyy-MM-dd");

            return View(items);
        }

        // ======================= DETALLE =======================
        public async Task<IActionResult> Details(int id)
        {
            var registro = await _context.RegistrosLeche
                .Include(r => r.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (registro == null) return NotFound();
            return View(registro);
        }

        // ======================= CREAR =======================
        public async Task<IActionResult> Create()
        {
            await CargarAnimalesSelectAsync();
            return View(new RegistroLeche { Fecha = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,Fecha,LitrosDia")] RegistroLeche registro)
        {
            // Normalizar fecha a solo día (por si la vista manda con hora)
            registro.Fecha = registro.Fecha.Date;

            // Validar duplicado: una vaca no debería tener dos registros el mismo día
            bool existe = await _context.RegistrosLeche
                .AnyAsync(r => r.AnimalId == registro.AnimalId && r.Fecha == registro.Fecha);

            if (existe)
                ModelState.AddModelError(string.Empty, "Ya existe un registro de leche para esta vaca en esa fecha.");

            if (!ModelState.IsValid)
            {
                await CargarAnimalesSelectAsync(registro.AnimalId);
                return View(registro);
            }

            _context.Add(registro);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ======================= EDITAR =======================
        public async Task<IActionResult> Edit(int id)
        {
            var registro = await _context.RegistrosLeche.FindAsync(id);
            if (registro == null) return NotFound();

            await CargarAnimalesSelectAsync(registro.AnimalId);
            return View(registro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AnimalId,Fecha,LitrosDia")] RegistroLeche registro)
        {
            if (id != registro.Id) return NotFound();

            registro.Fecha = registro.Fecha.Date;

            // Validar duplicado cuando se cambia fecha/animal
            bool existeOtro = await _context.RegistrosLeche
                .AnyAsync(r => r.Id != registro.Id &&
                               r.AnimalId == registro.AnimalId &&
                               r.Fecha == registro.Fecha);
            if (existeOtro)
                ModelState.AddModelError(string.Empty, "Ya existe un registro de leche para esta vaca en esa fecha.");

            if (!ModelState.IsValid)
            {
                await CargarAnimalesSelectAsync(registro.AnimalId);
                return View(registro);
            }

            try
            {
                _context.Update(registro);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await RegistroExists(registro.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ======================= ELIMINAR =======================
        public async Task<IActionResult> Delete(int id)
        {
            var registro = await _context.RegistrosLeche
                .Include(r => r.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (registro == null) return NotFound();

            return View(registro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registro = await _context.RegistrosLeche.FindAsync(id);
            if (registro != null)
            {
                _context.RegistrosLeche.Remove(registro);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ======================= AUXILIARES =======================
        private async Task CargarAnimalesSelectAsync(int? seleccionadoId = null)
        {
            var lista = await _context.Animales
                .OrderBy(a => a.Arete)
                .Select(a => new
                {
                    a.Id,
                    Texto = string.IsNullOrWhiteSpace(a.Nombre) ? a.Arete : $"{a.Arete} - {a.Nombre}"
                })
                .ToListAsync();

            ViewBag.AnimalId = new SelectList(lista, "Id", "Texto", seleccionadoId);
        }

        private Task<bool> RegistroExists(int id) =>
            _context.RegistrosLeche.AnyAsync(e => e.Id == id);

        // ======================= ENDPOINTS PARA MODAL / AJAX (Opcional) =======================
        // Devuelve las vacas en JSON para armar dropdowns dinámicos en un modal
        [HttpGet]
        public async Task<IActionResult> AnimalesLookup(string? q)
        {
            var query = _context.Animales.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                string term = q.Trim().ToUpper();
                query = query.Where(a =>
                    (a.Arete != null && a.Arete.ToUpper().Contains(term)) ||
                    (a.Nombre != null && a.Nombre.ToUpper().Contains(term)));
            }

            var data = await query
                .OrderBy(a => a.Arete)
                .Select(a => new
                {
                    a.Id,
                    Texto = string.IsNullOrWhiteSpace(a.Nombre) ? a.Arete : $"{a.Arete} - {a.Nombre}"
                })
                .Take(50)
                .ToListAsync();

            return Json(data);
        }
    }
}

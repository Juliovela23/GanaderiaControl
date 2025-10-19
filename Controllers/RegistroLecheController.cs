using GanaderiaControl.Data;
using GanaderiaControl.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GanaderiaControl.Controllers
{
    public class RegistroLecheController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RegistroLecheController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================= LISTADO =======================
        public async Task<IActionResult> Index(string? q, int? animalId, DateTime? desde, DateTime? hasta)
        {
            var query = _context.RegistrosLeche
                .AsNoTracking()
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
                var h = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.Fecha <= h);
            }

            var items = await query
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.Animal.Arete)
                .ToListAsync();

            ViewBag.Animales = await _context.Animales
                .AsNoTracking()
                .OrderBy(a => a.Arete)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(a.Nombre) ? a.Arete : $"{a.Arete} - {a.Nombre}"
                })
                .ToListAsync();

            return View(items);
        }

        // ======================= DETALLE =======================
        public async Task<IActionResult> Details(int id)
        {
            var registro = await _context.RegistrosLeche
                .AsNoTracking()
                .Include(r => r.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (registro == null) return NotFound();
            return View(registro);
        }

        // ======================= CREAR =======================
        // GET
        public async Task<IActionResult> Create()
        {
            await CargarAnimalesSelectAsync();
            return View(new RegistroLeche { Fecha = DateTime.Today });
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,Fecha,LitrosDia")] RegistroLeche registro)
        {
            // Muchas plantillas ponen [Required] en la navegación 'Animal'.
            // Ese campo NO se postea; limpiamos esa validación fantasma:
            ModelState.Remove("Animal");

            // --- Normalización de fecha ---
            if (registro.Fecha == default)
            {
                if (ModelState.ContainsKey(nameof(registro.Fecha)))
                    ModelState[nameof(registro.Fecha)].Errors.Clear();
                registro.Fecha = DateTime.Today;
            }
            registro.Fecha = registro.Fecha.Date;

            // --- Validar animal ---
            if (registro.AnimalId <= 0)
                ModelState.AddModelError(nameof(registro.AnimalId), "Selecciona una vaca.");

            // --- Reparar litros si el binder falló (coma/punto, vacío) ---
            if (ModelState.TryGetValue(nameof(registro.LitrosDia), out var litrosEntry) && litrosEntry.Errors.Count > 0)
            {
                var raw = Request.Form[nameof(registro.LitrosDia)].ToString()?.Trim();
                litrosEntry.Errors.Clear();

                if (string.IsNullOrWhiteSpace(raw))
                {
                    registro.LitrosDia = 0m;
                }
                else
                {
                    if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var p) ||
                        decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out p) ||
                        decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out p) ||
                        decimal.TryParse(raw.Replace('.', ','), NumberStyles.Number, CultureInfo.GetCultureInfo("es-ES"), out p))
                    {
                        registro.LitrosDia = p;
                    }
                    else
                    {
                        ModelState.AddModelError(nameof(registro.LitrosDia), "Formato de litros inválido.");
                    }
                }
            }

            // --- Validar duplicado (AnimalId + Fecha) ---
            if (ModelState.IsValid)
            {
                bool existe = await _context.RegistrosLeche
                    .AnyAsync(r => r.AnimalId == registro.AnimalId && r.Fecha == registro.Fecha);
                if (existe)
                    ModelState.AddModelError(string.Empty, "Ya existe un registro de leche para esta vaca en esa fecha.");
            }

            // --- Si hay errores, re-mostrar con combo cargado ---
            if (!ModelState.IsValid)
            {
                await CargarAnimalesSelectAsync(registro.AnimalId);
                return View(registro);
            }

            // --- Guardar ---
            _context.RegistrosLeche.Add(registro);
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

            // limpiar posible validación sobre navegación
            ModelState.Remove("Animal");

            registro.Fecha = registro.Fecha.Date;

            bool existeOtro = await _context.RegistrosLeche
                .AnyAsync(r => r.Id != registro.Id && r.AnimalId == registro.AnimalId && r.Fecha == registro.Fecha);
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
                .AsNoTracking()
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
        private async Task<bool> RegistroExists(int id) =>
            await _context.RegistrosLeche.AnyAsync(e => e.Id == id);

        private async Task CargarAnimalesSelectAsync(int? seleccionadoId = null)
        {
            ViewBag.AnimalId = await _context.Animales
                .AsNoTracking()
                .OrderBy(a => a.Arete)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(a.Nombre) ? a.Arete : $"{a.Arete} - {a.Nombre}",
                    Selected = (seleccionadoId.HasValue && a.Id == seleccionadoId.Value)
                })
                .ToListAsync();
        }
    }
}

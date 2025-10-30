using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GanaderiaControl.Controllers
{
    [Authorize]
    public class RegistroLecheController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RegistroLecheController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string? CurrentUserId() => _userManager.GetUserId(User);

        // ======================= LISTADO =======================
        public async Task<IActionResult> Index(string? q, int? animalId, DateTime? desde, DateTime? hasta)
        {
            var uid = CurrentUserId();

            var query = _context.RegistrosLeche
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.UserId == uid)
                .Include(r => r.Animal)
                .Where(r => r.Animal != null && !r.Animal.IsDeleted && r.Animal.userId == uid)
                .AsQueryable();

            if (animalId.HasValue)
                query = query.Where(r => r.AnimalId == animalId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                string term = q.Trim().ToUpperInvariant();
                query = query.Where(r =>
                    (r.Animal.Arete != null && r.Animal.Arete.ToUpper().Contains(term)) ||
                    (r.Animal.Nombre != null && r.Animal.Nombre.ToUpper().Contains(term)));
            }

            if (desde.HasValue)
                query = query.Where(r => r.Fecha >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(r => r.Fecha <= hasta.Value.Date);

            var items = await query
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.Animal.Arete)
                .ToListAsync();

            ViewBag.Animales = await _context.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.userId == uid)
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
            var uid = CurrentUserId();

            var registro = await _context.RegistrosLeche
                .AsNoTracking()
                .Include(r => r.Animal)
                .FirstOrDefaultAsync(m => m.Id == id
                                          && !m.IsDeleted
                                          && m.UserId == uid
                                          && m.Animal != null
                                          && !m.Animal.IsDeleted
                                          && m.Animal.userId == uid);

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
            var uid = CurrentUserId();

            // Navegación no posteada
            ModelState.Remove("Animal");

            // Normalizar fecha
            if (registro.Fecha == default)
            {
                if (ModelState.ContainsKey(nameof(registro.Fecha)))
                    ModelState[nameof(registro.Fecha)].Errors.Clear();
                registro.Fecha = DateTime.Today;
            }
            registro.Fecha = registro.Fecha.Date;

            // Validar animal del usuario
            if (registro.AnimalId <= 0 ||
                !await _context.Animales.AnyAsync(a => a.Id == registro.AnimalId && !a.IsDeleted && a.userId == uid))
            {
                ModelState.AddModelError(nameof(registro.AnimalId), "Selecciona una vaca válida.");
            }

            // Reparar posibles errores de parseo en LitrosDia
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

            // Validar duplicado (por usuario)
            if (ModelState.IsValid)
            {
                bool existe = await _context.RegistrosLeche
                    .AnyAsync(r => !r.IsDeleted
                                   && r.UserId == uid
                                   && r.AnimalId == registro.AnimalId
                                   && r.Fecha == registro.Fecha);
                if (existe)
                    ModelState.AddModelError(string.Empty, "Ya existe un registro de leche para esta vaca en esa fecha.");
            }

            if (!ModelState.IsValid)
            {
                await CargarAnimalesSelectAsync(registro.AnimalId);
                return View(registro);
            }

            // Guardar
            registro.UserId = uid;
            registro.CreatedAt = DateTime.UtcNow;
            registro.UpdatedAt = DateTime.UtcNow;

            _context.RegistrosLeche.Add(registro);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ======================= EDITAR =======================
        public async Task<IActionResult> Edit(int id)
        {
            var uid = CurrentUserId();

            var registro = await _context.RegistrosLeche
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.UserId == uid);
            if (registro == null) return NotFound();

            await CargarAnimalesSelectAsync(registro.AnimalId);
            return View(registro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AnimalId,Fecha,LitrosDia")] RegistroLeche registro)
        {
            var uid = CurrentUserId();
            if (id != registro.Id) return NotFound();

            // Navegación no posteada
            ModelState.Remove("Animal");

            registro.Fecha = registro.Fecha.Date;

            // Validar animal del usuario
            if (registro.AnimalId <= 0 ||
                !await _context.Animales.AnyAsync(a => a.Id == registro.AnimalId && !a.IsDeleted && a.userId == uid))
            {
                ModelState.AddModelError(nameof(registro.AnimalId), "Selecciona una vaca válida.");
            }

            // Validar duplicado (otro registro del mismo usuario)
            bool existeOtro = await _context.RegistrosLeche
                .AnyAsync(r => !r.IsDeleted
                               && r.UserId == uid
                               && r.Id != registro.Id
                               && r.AnimalId == registro.AnimalId
                               && r.Fecha == registro.Fecha);
            if (existeOtro)
                ModelState.AddModelError(string.Empty, "Ya existe un registro de leche para esta vaca en esa fecha.");

            if (!ModelState.IsValid)
            {
                await CargarAnimalesSelectAsync(registro.AnimalId);
                return View(registro);
            }

            // Actualizar con carga previa para no pisar UserId ni flags
            var entity = await _context.RegistrosLeche
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.UserId == uid);
            if (entity == null) return NotFound();

            entity.AnimalId = registro.AnimalId;
            entity.Fecha = registro.Fecha;
            entity.LitrosDia = registro.LitrosDia;
            entity.UserId = uid; // quién modificó
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ======================= ELIMINAR =======================
        public async Task<IActionResult> Delete(int id)
        {
            var uid = CurrentUserId();

            var registro = await _context.RegistrosLeche
                .AsNoTracking()
                .Include(r => r.Animal)
                .FirstOrDefaultAsync(m => m.Id == id
                                          && !m.IsDeleted
                                          && m.UserId == uid
                                          && m.Animal != null
                                          && !m.Animal.IsDeleted
                                          && m.Animal.userId == uid);
            if (registro == null) return NotFound();

            return View(registro);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uid = CurrentUserId();

            var registro = await _context.RegistrosLeche
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.UserId == uid);
            if (registro != null)
            {
                registro.IsDeleted = true;          // soft delete
                registro.UserId = uid;              // quién elimina
                registro.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ======================= AUXILIARES =======================
        private async Task<bool> RegistroExists(int id) =>
            await _context.RegistrosLeche.AnyAsync(e => e.Id == id && !e.IsDeleted);

        private async Task CargarAnimalesSelectAsync(int? seleccionadoId = null)
        {
            var uid = CurrentUserId();

            ViewBag.AnimalId = await _context.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.userId == uid)
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

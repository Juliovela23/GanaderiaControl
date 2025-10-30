using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql; // <-- importante para PostgresException
using System;

namespace GanaderiaControl.Controllers
{
    [Authorize]
    public class AnimalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AnimalesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string? CurrentUserId() => _userManager.GetUserId(User);

        // GET: Animales
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(a =>
                    a.Arete.Contains(q) ||
                    (a.Nombre != null && a.Nombre.Contains(q)));
            }

            var list = await query.ToListAsync();
            ViewData["q"] = q;
            return View(list);
        }

        // GET: Animales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var animal = await _context.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .Include(a => a.Madre)
                .Include(a => a.Padre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (animal == null) return NotFound();
            return View(animal);
        }

        // GET: Animales/Create
        public IActionResult Create()
        {
            PopulatePadresSelect();
            return View();
        }

        // POST: Animales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Arete,Nombre,Raza,FechaNacimiento,EstadoReproductivo,MadreId,PadreId")] Animal animal)
        {
            // Normaliza entradas
            if (!string.IsNullOrWhiteSpace(animal.Arete))
                animal.Arete = animal.Arete.Trim();

            if (animal.FechaNacimiento != null)
                animal.FechaNacimiento = animal.FechaNacimiento.Value.Date;

            if (string.IsNullOrWhiteSpace(animal.Arete))
                ModelState.AddModelError(nameof(animal.Arete), "El arete es obligatorio.");

            // Arete único (no borrados)
            if (!string.IsNullOrWhiteSpace(animal.Arete))
            {
                bool dup = await _context.Animales
                    .AnyAsync(a => !a.IsDeleted && a.Arete == animal.Arete);
                if (dup)
                    ModelState.AddModelError(nameof(animal.Arete), "El arete ya existe.");
            }

            // Evitar auto-referencia
            if (animal.MadreId.HasValue && animal.MadreId == animal.Id)
                ModelState.AddModelError(nameof(animal.MadreId), "No puede ser su propia madre.");
            if (animal.PadreId.HasValue && animal.PadreId == animal.Id)
                ModelState.AddModelError(nameof(animal.PadreId), "No puede ser su propio padre.");

            if (!ModelState.IsValid)
            {
                PopulatePadresSelect(animal.MadreId, animal.PadreId);
                return View(animal);
            }

            try
            {
                // Auditoría en UTC + UserId
                animal.CreatedAt = DateTime.UtcNow;
                animal.UpdatedAt = DateTime.UtcNow;
                animal.UserId = CurrentUserId();

                _context.Add(animal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                if (pex.SqlState == PostgresErrorCodes.UniqueViolation)
                    ModelState.AddModelError(nameof(animal.Arete), "El arete ya existe (índice único).");
                else if (pex.SqlState == PostgresErrorCodes.NotNullViolation)
                    ModelState.AddModelError(string.Empty, $"Falta un campo requerido.");
                else if (pex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
                    ModelState.AddModelError(string.Empty, "Referencia inválida (Madre/Padre).");
                else
                    ModelState.AddModelError(string.Empty, "No se pudo guardar el registro: " + pex.MessageText);

                PopulatePadresSelect(animal.MadreId, animal.PadreId);
                return View(animal);
            }
        }

        // GET: Animales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var animal = await _context.Animales
                .Where(a => !a.IsDeleted)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (animal == null) return NotFound();

            PopulatePadresSelect(animal.MadreId, animal.PadreId, excludeId: animal.Id);
            return View(animal);
        }

        // POST: Animales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Arete,Nombre,Raza,FechaNacimiento,EstadoReproductivo,MadreId,PadreId")] Animal animal)
        {
            if (id != animal.Id) return NotFound();

            if (!string.IsNullOrWhiteSpace(animal.Arete))
                animal.Arete = animal.Arete.Trim();

            if (animal.FechaNacimiento != null)
                animal.FechaNacimiento = animal.FechaNacimiento.Value.Date;

            if (string.IsNullOrWhiteSpace(animal.Arete))
                ModelState.AddModelError(nameof(animal.Arete), "El arete es obligatorio.");

            bool dup = await _context.Animales
                .AnyAsync(a => !a.IsDeleted && a.Arete == animal.Arete && a.Id != animal.Id);
            if (dup)
                ModelState.AddModelError(nameof(animal.Arete), "El arete ya está asignado a otro animal.");

            if (animal.MadreId.HasValue && animal.MadreId == animal.Id)
                ModelState.AddModelError(nameof(animal.MadreId), "No puede ser su propia madre.");
            if (animal.PadreId.HasValue && animal.PadreId == animal.Id)
                ModelState.AddModelError(nameof(animal.PadreId), "No puede ser su propio padre.");

            if (!ModelState.IsValid)
            {
                PopulatePadresSelect(animal.MadreId, animal.PadreId, excludeId: animal.Id);
                return View(animal);
            }

            try
            {
                var entity = await _context.Animales.FirstAsync(a => a.Id == animal.Id && !a.IsDeleted);
                entity.Arete = animal.Arete;
                entity.Nombre = animal.Nombre;
                entity.Raza = animal.Raza;
                entity.FechaNacimiento = animal.FechaNacimiento;
                entity.EstadoReproductivo = animal.EstadoReproductivo;
                entity.MadreId = animal.MadreId;
                entity.PadreId = animal.PadreId;
                entity.UpdatedAt = DateTime.UtcNow;     // auditoría UTC
                entity.UserId = CurrentUserId();      // último usuario que modificó

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                if (pex.SqlState == PostgresErrorCodes.UniqueViolation)
                    ModelState.AddModelError(nameof(animal.Arete), "El arete ya existe (índice único).");
                else if (pex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
                    ModelState.AddModelError(string.Empty, "Referencia inválida (Madre/Padre).");
                else
                    ModelState.AddModelError(string.Empty, "Error al guardar: " + pex.MessageText);

                PopulatePadresSelect(animal.MadreId, animal.PadreId, excludeId: animal.Id);
                return View(animal);
            }
        }

        // GET: Animales/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var animal = await _context.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (animal == null) return NotFound();
            return View(animal);
        }

        // POST: Animales/Delete/5  (soft delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var animal = await _context.Animales.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (animal == null) return NotFound();

            animal.IsDeleted = true;
            animal.UpdatedAt = DateTime.UtcNow; // auditoría UTC
            animal.UserId = CurrentUserId(); // quién lo marcó como eliminado
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private void PopulatePadresSelect(int? madreId = null, int? padreId = null, int? excludeId = null)
        {
            var baseQuery = _context.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.Arete)
                .AsQueryable();

            if (excludeId.HasValue) baseQuery = baseQuery.Where(a => a.Id != excludeId);

            ViewData["MadreId"] = new SelectList(baseQuery, "Id", "Arete", madreId);
            ViewData["PadreId"] = new SelectList(baseQuery, "Id", "Arete", padreId);
        }
    }
}

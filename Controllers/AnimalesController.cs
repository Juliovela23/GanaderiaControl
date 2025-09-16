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
   // [Authorize] 
    public class AnimalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnimalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Animales
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.Animales
                .AsNoTracking()
                .OrderByDescending(a => a.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(a => a.Arete.Contains(q) || (a.Nombre != null && a.Nombre.Contains(q)));
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
            if (await _context.Animales.AnyAsync(a => a.Arete == animal.Arete))
                ModelState.AddModelError(nameof(animal.Arete), "El arete ya existe.");

            if (ModelState.IsValid)
            {
                _context.Add(animal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulatePadresSelect(animal.MadreId, animal.PadreId);
            return View(animal);
        }

        // GET: Animales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var animal = await _context.Animales.FindAsync(id);
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

            if (await _context.Animales.AnyAsync(a => a.Arete == animal.Arete && a.Id != animal.Id))
                ModelState.AddModelError(nameof(animal.Arete), "El arete ya está asignado a otro animal.");

            if (!ModelState.IsValid)
            {
                PopulatePadresSelect(animal.MadreId, animal.PadreId, excludeId: animal.Id);
                return View(animal);
            }

            try
            {
                var entity = await _context.Animales.FirstAsync(a => a.Id == animal.Id);
                entity.Arete = animal.Arete;
                entity.Nombre = animal.Nombre;
                entity.Raza = animal.Raza;
                entity.FechaNacimiento = animal.FechaNacimiento;
                entity.EstadoReproductivo = animal.EstadoReproductivo;
                entity.MadreId = animal.MadreId;
                entity.PadreId = animal.PadreId;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Error al guardar. Verifica que el arete sea único.");
                PopulatePadresSelect(animal.MadreId, animal.PadreId, excludeId: animal.Id);
                return View(animal);
            }
        }

        // GET: Animales/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var animal = await _context.Animales.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (animal == null) return NotFound();
            return View(animal);
        }

        // POST: Animales/Delete/5  (soft delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var animal = await _context.Animales.FirstOrDefaultAsync(a => a.Id == id);
            if (animal == null) return NotFound();

            animal.IsDeleted = true; // soft delete
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private void PopulatePadresSelect(int? madreId = null, int? padreId = null, int? excludeId = null)
        {
            var baseQuery = _context.Animales.AsNoTracking().OrderBy(a => a.Arete).AsQueryable();
            if (excludeId.HasValue) baseQuery = baseQuery.Where(a => a.Id != excludeId);

            ViewData["MadreId"] = new SelectList(baseQuery, "Id", "Arete", madreId);
            ViewData["PadreId"] = new SelectList(baseQuery, "Id", "Arete", padreId);
        }
    }
}

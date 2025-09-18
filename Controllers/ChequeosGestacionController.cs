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
    [Authorize] // agrega Roles si ya los usas: [Authorize(Roles = "Admin,Operador")]
    public class ChequeosGestacionController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ChequeosGestacionController(ApplicationDbContext db) { _db = db; }

        // GET: /ChequeosGestacion
        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.ChequeosGestacion
                .Include(c => c.Animal)
                .OrderByDescending(c => c.FechaChequeo).ThenByDescending(c => c.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(c =>
                    c.Animal.Arete.Contains(q) ||
                    (c.Animal.Nombre != null && c.Animal.Nombre.Contains(q)) ||
                    (c.Observaciones ?? "").Contains(q));
            }

            var list = await query.Take(200).ToListAsync(); // simple, puedes paginar luego
            ViewBag.Q = q;
            return View(list);
        }

        // GET: /ChequeosGestacion/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var model = await _db.ChequeosGestacion
                .Include(c => c.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (model == null) return NotFound();
            return View(model);
        }

        // GET: /ChequeosGestacion/Create
        public async Task<IActionResult> Create(int? animalId)
        {
            await CargarAnimales(animalId);
            ViewBag.CrearAlertaReServicio = false;
            return View(new ChequeoGestacion { FechaChequeo = DateTime.Today });
        }

        // POST: /ChequeosGestacion/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,FechaChequeo,Resultado,Observaciones")] ChequeoGestacion model, bool crearAlertaReServicio = false)
        {
            if (!await _db.Animales.AnyAsync(a => a.Id == model.AnimalId))
                ModelState.AddModelError(nameof(model.AnimalId), "Animal inválido.");

            if (ModelState.IsValid)
            {
                _db.Add(model);
                await _db.SaveChangesAsync();

                await AplicarLogicaAlertasPostChequeo(model, crearAlertaReServicio);
                TempData["Ok"] = "Chequeo registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            await CargarAnimales(model.AnimalId);
            ViewBag.CrearAlertaReServicio = crearAlertaReServicio;
            return View(model);
        }

        // GET: /ChequeosGestacion/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _db.ChequeosGestacion.FindAsync(id);
            if (model == null) return NotFound();

            await CargarAnimales(model.AnimalId);
            ViewBag.CrearAlertaReServicio = false;
            return View(model);
        }

        // POST: /ChequeosGestacion/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AnimalId,FechaChequeo,Resultado,Observaciones")] ChequeoGestacion model, bool crearAlertaReServicio = false)
        {
            if (id != model.Id) return NotFound();
            if (!await _db.Animales.AnyAsync(a => a.Id == model.AnimalId))
                ModelState.AddModelError(nameof(model.AnimalId), "Animal inválido.");

            if (ModelState.IsValid)
            {
                var current = await _db.ChequeosGestacion.FirstOrDefaultAsync(x => x.Id == id);
                if (current == null) return NotFound();

                current.AnimalId = model.AnimalId;
                current.FechaChequeo = model.FechaChequeo.Date;
                current.Resultado = model.Resultado;
                current.Observaciones = model.Observaciones;
                current.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                await AplicarLogicaAlertasPostChequeo(current, crearAlertaReServicio);
                TempData["Ok"] = "Chequeo actualizado.";
                return RedirectToAction(nameof(Index));
            }

            await CargarAnimales(model.AnimalId);
            ViewBag.CrearAlertaReServicio = crearAlertaReServicio;
            return View(model);
        }

        // GET: /ChequeosGestacion/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _db.ChequeosGestacion
                .Include(c => c.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (model == null) return NotFound();
            return View(model);
        }

        // POST: /ChequeosGestacion/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await _db.ChequeosGestacion.FirstOrDefaultAsync(m => m.Id == id);
            if (model == null) return NotFound();

            model.IsDeleted = true;
            model.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Chequeo eliminado.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarAnimales(int? animalId = null)
        {
            var animales = await _db.Animales
                .OrderBy(a => a.Arete)
                .Select(a => new { a.Id, Etiqueta = a.Arete + (a.Nombre != null ? " - " + a.Nombre : "") })
                .ToListAsync();

            ViewBag.AnimalId = new SelectList(animales, "Id", "Etiqueta", animalId);
        }

        private async Task AplicarLogicaAlertasPostChequeo(ChequeoGestacion chk, bool crearAlertaReServicio)
        {
            // Actualiza estado del animal útil para tu dashboard
            var animal = await _db.Animales.FirstOrDefaultAsync(a => a.Id == chk.AnimalId);
            if (animal != null)
            {
                if (chk.Resultado == ResultadoGestacion.Gestante)
                    animal.EstadoReproductivo = EstadoReproductivo.Gestante;
                else if (chk.Resultado == ResultadoGestacion.NoGestante)
                    animal.EstadoReproductivo = EstadoReproductivo.Abierta;

                animal.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            if (chk.Resultado == ResultadoGestacion.Gestante)
            {
                // Usa el último servicio del animal si existe
                var servicio = await _db.Servicios
                    .Where(s => s.AnimalId == chk.AnimalId)
                    .OrderByDescending(s => s.FechaServicio)
                    .FirstOrDefaultAsync();

                var fechaBase = servicio?.FechaServicio.Date ?? chk.FechaChequeo.Date;
                var fechaPartoProbable = fechaBase.AddDays(283);

                await AsegurarAlerta(chk.AnimalId, TipoAlerta.PartoProbable, fechaPartoProbable,
                    "Asegurada por chequeo Gestante (+283d desde el último servicio o fecha de chequeo).");
            }
            else if (chk.Resultado == ResultadoGestacion.NoGestante && crearAlertaReServicio)
            {
                var fechaReServicio = chk.FechaChequeo.Date.AddDays(21);
                await AsegurarAlerta(chk.AnimalId, TipoAlerta.Salud, fechaReServicio,
                    "Sugerencia de Re-servicio (+21d) tras resultado No Gestante.");
            }
        }

        private async Task AsegurarAlerta(int animalId, TipoAlerta tipo, DateTime fechaObjetivo, string? nota = null)
        {
            var existe = await _db.Alertas.AnyAsync(a =>
                a.AnimalId == animalId && a.Tipo == tipo && a.FechaObjetivo == fechaObjetivo && !a.IsDeleted);

            if (!existe)
            {
                _db.Alertas.Add(new Alerta
                {
                    AnimalId = animalId,
                    Tipo = tipo,
                    FechaObjetivo = fechaObjetivo,
                    Estado = EstadoAlerta.Pendiente,
                    Notas = nota,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
        }
    }
}

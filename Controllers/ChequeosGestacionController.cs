using System;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GanaderiaControl.Controllers
{
    [Authorize]
    public class ChequeosGestacionController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ChequeosGestacionController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.ChequeosGestacion
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Include(c => c.Animal)
                .Where(c => c.Animal != null && !c.Animal.IsDeleted)
                .OrderByDescending(c => c.FechaChequeo).ThenByDescending(c => c.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(c =>
                    (c.Animal.Arete != null && c.Animal.Arete.Contains(q)) ||
                    (c.Animal.Nombre != null && c.Animal.Nombre.Contains(q)) ||
                    ((c.Observaciones ?? "").Contains(q)));
            }

            var list = await query.Take(200).ToListAsync();
            ViewBag.Q = q;
            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var model = await _db.ChequeosGestacion
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(c => c.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (model == null || model.Animal == null || model.Animal.IsDeleted) return NotFound();
            return View(model);
        }

        public async Task<IActionResult> Create(int? animalId)
        {
            await CargarAnimales(animalId);
            ViewBag.CrearAlertaReServicio = false;
            return View(new ChequeoGestacion { FechaChequeo = DateTime.Today });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,FechaChequeo,Resultado,Observaciones")] ChequeoGestacion model, bool crearAlertaReServicio = false)
        {
            var animalOk = await _db.Animales.AnyAsync(a => a.Id == model.AnimalId && !a.IsDeleted);
            if (!animalOk)
                ModelState.AddModelError(nameof(model.AnimalId), "Animal inválido.");

            model.FechaChequeo = model.FechaChequeo.Date;

            if (!ModelState.IsValid)
            {
                await CargarAnimales(model.AnimalId);
                ViewBag.CrearAlertaReServicio = crearAlertaReServicio;
                return View(model);
            }

            try
            {
                model.CreatedAt = DateTime.UtcNow; // UTC
                model.UpdatedAt = DateTime.UtcNow;

                _db.Add(model);
                await _db.SaveChangesAsync();

                await AplicarLogicaAlertasPostChequeo(model, crearAlertaReServicio);
                TempData["Ok"] = "Chequeo registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                ModelState.AddModelError(string.Empty, "No se pudo guardar: " + pex.MessageText);
                await CargarAnimales(model.AnimalId);
                ViewBag.CrearAlertaReServicio = crearAlertaReServicio;
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var model = await _db.ChequeosGestacion
                .Where(x => !x.IsDeleted)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (model == null) return NotFound();

            await CargarAnimales(model.AnimalId);
            ViewBag.CrearAlertaReServicio = false;
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AnimalId,FechaChequeo,Resultado,Observaciones")] ChequeoGestacion model, bool crearAlertaReServicio = false)
        {
            if (id != model.Id) return NotFound();

            var animalOk = await _db.Animales.AnyAsync(a => a.Id == model.AnimalId && !a.IsDeleted);
            if (!animalOk)
                ModelState.AddModelError(nameof(model.AnimalId), "Animal inválido.");

            model.FechaChequeo = model.FechaChequeo.Date;

            if (!ModelState.IsValid)
            {
                await CargarAnimales(model.AnimalId);
                ViewBag.CrearAlertaReServicio = crearAlertaReServicio;
                return View(model);
            }

            var current = await _db.ChequeosGestacion.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (current == null) return NotFound();

            current.AnimalId = model.AnimalId;
            current.FechaChequeo = model.FechaChequeo;
            current.Resultado = model.Resultado;
            current.Observaciones = model.Observaciones;
            current.UpdatedAt = DateTime.UtcNow; // UTC

            try
            {
                await _db.SaveChangesAsync();
                await AplicarLogicaAlertasPostChequeo(current, crearAlertaReServicio);
                TempData["Ok"] = "Chequeo actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                ModelState.AddModelError(string.Empty, "Error al guardar: " + pex.MessageText);
                await CargarAnimales(model.AnimalId);
                ViewBag.CrearAlertaReServicio = crearAlertaReServicio;
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var model = await _db.ChequeosGestacion
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(c => c.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (model == null || model.Animal == null || model.Animal.IsDeleted) return NotFound();
            return View(model);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await _db.ChequeosGestacion.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (model == null) return NotFound();

            model.IsDeleted = true;
            model.UpdatedAt = DateTime.UtcNow; // UTC
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Chequeo eliminado.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarAnimales(int? animalId = null)
        {
            var animales = await _db.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.Arete)
                .Select(a => new { a.Id, Etiqueta = a.Arete + (a.Nombre != null ? " - " + a.Nombre : "") })
                .ToListAsync();

            ViewBag.AnimalId = new SelectList(animales, "Id", "Etiqueta", animalId);
        }

        private async Task AplicarLogicaAlertasPostChequeo(ChequeoGestacion chk, bool crearAlertaReServicio)
        {
            var animal = await _db.Animales.FirstOrDefaultAsync(a => a.Id == chk.AnimalId && !a.IsDeleted);
            if (animal != null)
            {
                animal.EstadoReproductivo = chk.Resultado == ResultadoGestacion.Gestante
                    ? EstadoReproductivo.Gestante
                    : EstadoReproductivo.Abierta;

                animal.UpdatedAt = DateTime.UtcNow; // UTC
                await _db.SaveChangesAsync();
            }

            if (chk.Resultado == ResultadoGestacion.Gestante)
            {
                var servicio = await _db.Servicios
                    .Where(s => s.AnimalId == chk.AnimalId && !s.IsDeleted)
                    .OrderByDescending(s => s.FechaServicio)
                    .FirstOrDefaultAsync();

                var fechaBase = servicio?.FechaServicio.Date ?? chk.FechaChequeo.Date;
                var fechaPartoProbable = fechaBase.AddDays(283);

                await AsegurarAlerta(chk.AnimalId, TipoAlerta.PartoProbable, fechaPartoProbable,
                    "Chequeo Gestante (+283d desde último servicio o fecha de chequeo).");
            }
            else if (chk.Resultado == ResultadoGestacion.NoGestante && crearAlertaReServicio)
            {
                var fechaReServicio = chk.FechaChequeo.Date.AddDays(21);
                await AsegurarAlerta(chk.AnimalId, TipoAlerta.Salud, fechaReServicio,
                    "Re-servicio sugerido (+21d) tras resultado No Gestante.");
            }
        }

        private async Task AsegurarAlerta(int animalId, TipoAlerta tipo, DateTime fechaObjetivo, string? nota = null)
        {
            var existe = await _db.Alertas.AnyAsync(a =>
                !a.IsDeleted &&
                a.AnimalId == animalId &&
                a.Tipo == tipo &&
                a.FechaObjetivo == fechaObjetivo);

            if (!existe)
            {
                _db.Alertas.Add(new Alerta
                {
                    AnimalId = animalId,
                    Tipo = tipo,
                    FechaObjetivo = fechaObjetivo.Date,
                    Estado = EstadoAlerta.Pendiente,
                    Notas = nota,
                    CreatedAt = DateTime.UtcNow, // UTC
                    UpdatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
        }
    }
}

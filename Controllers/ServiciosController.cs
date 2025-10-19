using System;
using System.Collections.Generic;
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
    public class ServiciosController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ServiciosController(ApplicationDbContext context) => _context = context;

        // LISTADO
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.Servicios
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .Include(s => s.Animal)
                .Where(s => s.Animal != null && !s.Animal.IsDeleted)
                .OrderByDescending(s => s.FechaServicio)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(s =>
                    (s.Animal.Arete != null && s.Animal.Arete.Contains(q)) ||
                    (s.Animal.Nombre != null && s.Animal.Nombre.Contains(q)));
            }

            ViewData["q"] = q;
            var list = await query.ToListAsync();
            return View(list);
        }

        // DETALLE
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var servicio = await _context.Servicios
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .Include(s => s.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (servicio == null || servicio.Animal == null || servicio.Animal.IsDeleted) return NotFound();
            return View(servicio);
        }

        // CREATE GET
        public async Task<IActionResult> Create()
        {
            await CargarAnimalesAsync();
            return View(new ServicioReproductivo { FechaServicio = DateTime.Today });
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServicioReproductivo s)
        {
            // FK válida
            var animalOk = await _context.Animales.AnyAsync(a => a.Id == s.AnimalId && !a.IsDeleted);
            if (!animalOk)
                ModelState.AddModelError(nameof(s.AnimalId), "Animal inválido.");

            s.FechaServicio = s.FechaServicio.Date;

            // duplicado mismo día
            if (await _context.Servicios.AnyAsync(x =>
                !x.IsDeleted &&
                x.AnimalId == s.AnimalId &&
                x.FechaServicio == s.FechaServicio))
            {
                ModelState.AddModelError(nameof(s.FechaServicio), "Ya existe un servicio para este animal en esa fecha.");
            }

            if (!ModelState.IsValid)
            {
                await CargarAnimalesAsync(s.AnimalId);
                return View(s);
            }

            try
            {
                _context.Servicios.Add(s);
                await _context.SaveChangesAsync();

                await CrearAlertasDerivadasAsync(s);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                ModelState.AddModelError(string.Empty, "No se pudo guardar: " + pex.MessageText);
                await CargarAnimalesAsync(s.AnimalId);
                return View(s);
            }
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var servicio = await _context.Servicios.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (servicio == null) return NotFound();

            await CargarAnimalesAsync(servicio.AnimalId);
            return View(servicio);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServicioReproductivo servicio)
        {
            if (id != servicio.Id) return NotFound();

            var animalOk = await _context.Animales.AnyAsync(a => a.Id == servicio.AnimalId && !a.IsDeleted);
            if (!animalOk)
                ModelState.AddModelError(nameof(servicio.AnimalId), "Animal inválido.");

            servicio.FechaServicio = servicio.FechaServicio.Date;

            if (await _context.Servicios.AnyAsync(s =>
                !s.IsDeleted &&
                s.AnimalId == servicio.AnimalId &&
                s.FechaServicio == servicio.FechaServicio &&
                s.Id != servicio.Id))
            {
                ModelState.AddModelError(nameof(servicio.FechaServicio), "Ya existe un servicio para este animal en esa fecha.");
            }

            if (!ModelState.IsValid)
            {
                await CargarAnimalesAsync(servicio.AnimalId);
                return View(servicio);
            }

            try
            {
                var entity = await _context.Servicios.FirstAsync(s => s.Id == servicio.Id && !s.IsDeleted);
                entity.AnimalId = servicio.AnimalId;
                entity.FechaServicio = servicio.FechaServicio;
                entity.Tipo = servicio.Tipo;
                entity.ToroOProveedor = servicio.ToroOProveedor;
                entity.Observaciones = servicio.Observaciones;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                ModelState.AddModelError(string.Empty, "Error al guardar: " + pex.MessageText);
                await CargarAnimalesAsync(servicio.AnimalId);
                return View(servicio);
            }
        }

        // DELETE GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var servicio = await _context.Servicios
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .Include(s => s.Animal)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (servicio == null || servicio.Animal == null || servicio.Animal.IsDeleted) return NotFound();
            return View(servicio);
        }

        // DELETE POST (soft)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (servicio == null) return NotFound();

            servicio.IsDeleted = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // HELPERS
        private async Task CargarAnimalesAsync(int? seleccionado = null)
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

            ViewBag.AnimalId = new SelectList(animales, "Id", "Texto", seleccionado);
        }

        private async Task CrearAlertasDerivadasAsync(ServicioReproductivo s)
        {
            var baseDate = s.FechaServicio.Date;

            var alertas = new List<Alerta>
            {
                new Alerta{ AnimalId=s.AnimalId, Tipo=TipoAlerta.ChequeoGestacion, Estado=EstadoAlerta.Pendiente, FechaObjetivo=baseDate.AddDays(32), Disparador=$"Servicio #{s.Id} del {baseDate:yyyy-MM-dd}", Notas="Chequeo gestación (~32d)" },
                new Alerta{ AnimalId=s.AnimalId, Tipo=TipoAlerta.Secado,            Estado=EstadoAlerta.Pendiente, FechaObjetivo=baseDate.AddDays(210),Disparador=$"Servicio #{s.Id} del {baseDate:yyyy-MM-dd}", Notas="Secado estimado (~210d)" },
                new Alerta{ AnimalId=s.AnimalId, Tipo=TipoAlerta.PartoProbable,    Estado=EstadoAlerta.Pendiente, FechaObjetivo=baseDate.AddDays(283),Disparador=$"Servicio #{s.Id} del {baseDate:yyyy-MM-dd}", Notas="Parto probable (~283d)" }
            };

            foreach (var a in alertas)
            {
                var existe = await _context.Alertas.AnyAsync(x =>
                    !x.IsDeleted &&
                    x.AnimalId == a.AnimalId &&
                    x.Tipo == a.Tipo &&
                    x.FechaObjetivo == a.FechaObjetivo);

                if (!existe) _context.Alertas.Add(a);
            }

            await _context.SaveChangesAsync();
        }
    }
}

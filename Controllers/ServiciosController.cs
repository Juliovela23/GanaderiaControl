using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GanaderiaControl.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiciosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =================== LISTADO ===================
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.Servicios
                .Include(s => s.Animal)
                .AsNoTracking()
                .OrderByDescending(s => s.FechaServicio)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(s =>
                    s.Animal.Arete.Contains(q) ||
                    (s.Animal.Nombre != null && s.Animal.Nombre.Contains(q)));
            }

            ViewData["q"] = q;
            var list = await query.ToListAsync();
            return View(list);
        }

        // =================== DETALLE ===================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var servicio = await _context.Servicios
                .Include(s => s.Animal)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (servicio == null) return NotFound();
            return View(servicio);
        }

        // =================== CREAR ===================
        // GET: Servicios/Create
        public async Task<IActionResult> Create()
        {
            await CargarAnimalesAsync();
            return View(new ServicioReproductivo
            {
                FechaServicio = DateTime.Today
            });
        }

        // POST: Servicios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServicioReproductivo s)
        {
            // Ejemplo de validación de duplicado de servicio mismo día
            if (await _context.Servicios.AnyAsync(x =>
                x.AnimalId == s.AnimalId &&
                x.FechaServicio.Date == s.FechaServicio.Date))
            {
                ModelState.AddModelError(nameof(s.FechaServicio),
                    "Ya existe un servicio para este animal en esa fecha.");
            }

            if (!ModelState.IsValid)
            {
                await CargarAnimalesAsync(s.AnimalId);
                return View(s);
            }

            // 1) Guardar el servicio para obtener Id
            _context.Servicios.Add(s);
            await _context.SaveChangesAsync(); // aquí s.Id ya tiene valor

            // 2) Crear alertas derivadas (evitando duplicados)
            await CrearAlertasDerivadasAsync(s);

            return RedirectToAction(nameof(Index));
        }

        // =================== EDITAR ===================
        // GET: Servicios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var servicio = await _context.Servicios.FindAsync(id);
            if (servicio == null) return NotFound();

            await CargarAnimalesAsync(servicio.AnimalId);
            return View(servicio);
        }

        // POST: Servicios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServicioReproductivo servicio)
        {
            if (id != servicio.Id) return NotFound();

            if (await _context.Servicios.AnyAsync(s =>
                s.AnimalId == servicio.AnimalId &&
                s.FechaServicio.Date == servicio.FechaServicio.Date &&
                s.Id != servicio.Id))
            {
                ModelState.AddModelError(nameof(servicio.FechaServicio),
                    "Ya existe un servicio para este animal en esa fecha.");
            }

            if (!ModelState.IsValid)
            {
                await CargarAnimalesAsync(servicio.AnimalId);
                return View(servicio);
            }

            var entity = await _context.Servicios.FirstAsync(s => s.Id == servicio.Id);
            entity.AnimalId = servicio.AnimalId;
            entity.FechaServicio = servicio.FechaServicio.Date;
            entity.Tipo = servicio.Tipo;
            entity.ToroOProveedor = servicio.ToroOProveedor;
            entity.Observaciones = servicio.Observaciones;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =================== ELIMINAR (soft) ===================
        // GET: Servicios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var servicio = await _context.Servicios
                .Include(s => s.Animal)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (servicio == null) return NotFound();
            return View(servicio);
        }

        // POST: Servicios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
            if (servicio == null) return NotFound();

            servicio.IsDeleted = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =================== HELPERS ===================

        private async Task CargarAnimalesAsync(int? seleccionado = null)
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

            ViewBag.AnimalId = new SelectList(animales, "Id", "Texto", seleccionado);
        }

        private async Task CrearAlertasDerivadasAsync(ServicioReproductivo s)
        {
            var baseDate = s.FechaServicio.Date;

            var alertas = new List<Alerta>
            {
                new Alerta
                {
                    AnimalId = s.AnimalId,
                    Tipo = TipoAlerta.ChequeoGestacion,
                    Estado = EstadoAlerta.Pendiente,
                    FechaObjetivo = baseDate.AddDays(32),
                    Disparador = $"Servicio #{s.Id} del {baseDate:yyyy-MM-dd}",
                    Notas = "Chequeo rutinario de gestación (≈32 días)"
                },
                new Alerta
                {
                    AnimalId = s.AnimalId,
                    Tipo = TipoAlerta.Secado,
                    Estado = EstadoAlerta.Pendiente,
                    FechaObjetivo = baseDate.AddDays(210),
                    Disparador = $"Servicio #{s.Id} del {baseDate:yyyy-MM-dd}",
                    Notas = "Secado estimado (≈210 días pos-servicio)"
                },
                new Alerta
                {
                    AnimalId = s.AnimalId,
                    Tipo = TipoAlerta.PartoProbable,
                    Estado = EstadoAlerta.Pendiente,
                    FechaObjetivo = baseDate.AddDays(283),
                    Disparador = $"Servicio #{s.Id} del {baseDate:yyyy-MM-dd}",
                    Notas = "Fecha probable de parto (≈283 días)"
                }
            };

            foreach (var a in alertas)
            {
                var existe = await _context.Alertas.AnyAsync(x =>
                    x.AnimalId == a.AnimalId &&
                    x.Tipo == a.Tipo &&
                    x.FechaObjetivo == a.FechaObjetivo);

                if (!existe)
                    _context.Alertas.Add(a);
            }

            await _context.SaveChangesAsync();
        }
    }
}

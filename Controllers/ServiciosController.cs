using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GanaderiaControl.Controllers
{
    [Authorize]
    public class ServiciosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ServiciosController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string? CurrentUserId() => _userManager.GetUserId(User);

        // LISTADO
        public async Task<IActionResult> Index(string? q)
        {
            var uid = CurrentUserId();

            var query = _context.Servicios
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.UserId == uid)
                .Include(s => s.Animal)
                .Where(s => s.Animal != null && !s.Animal.IsDeleted && s.Animal.userId == uid)
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
            var uid = CurrentUserId();

            var servicio = await _context.Servicios
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.UserId == uid)
                .Include(s => s.Animal)
                .Where(s => s.Animal != null && !s.Animal.IsDeleted && s.Animal.userId == uid)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (servicio == null) return NotFound();
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
            var uid = CurrentUserId();

            // evitar validación de navegación no posteada
            ModelState.Remove("Animal");

            // validar animal del usuario
            var animalOk = await _context.Animales.AnyAsync(a => a.Id == s.AnimalId && !a.IsDeleted && a.userId == uid);
            if (!animalOk)
                ModelState.AddModelError(nameof(s.AnimalId), "Animal inválido.");

            s.FechaServicio = s.FechaServicio.Date;

            // duplicado por usuario
            if (await _context.Servicios.AnyAsync(x =>
                !x.IsDeleted &&
                x.UserId == uid &&
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
                s.UserId = uid;
                s.CreatedAt = DateTime.UtcNow; // UTC
                s.UpdatedAt = DateTime.UtcNow;

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
            var uid = CurrentUserId();

            var servicio = await _context.Servicios
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.UserId == uid);
            if (servicio == null) return NotFound();

            await CargarAnimalesAsync(servicio.AnimalId);
            return View(servicio);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServicioReproductivo servicio)
        {
            var uid = CurrentUserId();
            if (id != servicio.Id) return NotFound();

            // evitar validación de navegación no posteada
            ModelState.Remove("Animal");

            // validar animal del usuario
            var animalOk = await _context.Animales.AnyAsync(a => a.Id == servicio.AnimalId && !a.IsDeleted && a.userId == uid);
            if (!animalOk)
                ModelState.AddModelError(nameof(servicio.AnimalId), "Animal inválido.");

            servicio.FechaServicio = servicio.FechaServicio.Date;

            // duplicado por usuario, excluyendo el mismo Id
            if (await _context.Servicios.AnyAsync(s =>
                !s.IsDeleted &&
                s.UserId == uid &&
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
                var entity = await _context.Servicios
                    .FirstAsync(s => s.Id == servicio.Id && !s.IsDeleted && s.UserId == uid);

                entity.AnimalId = servicio.AnimalId;
                entity.FechaServicio = servicio.FechaServicio;
                entity.Tipo = servicio.Tipo;
                entity.ToroOProveedor = servicio.ToroOProveedor;
                entity.Observaciones = servicio.Observaciones;
                entity.UserId = uid;              // quién actualiza
                entity.UpdatedAt = DateTime.UtcNow; // UTC

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
            var uid = CurrentUserId();

            var servicio = await _context.Servicios
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.UserId == uid)
                .Include(s => s.Animal)
                .Where(s => s.Animal != null && !s.Animal.IsDeleted && s.Animal.userId == uid)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (servicio == null) return NotFound();
            return View(servicio);
        }

        // DELETE POST (soft)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uid = CurrentUserId();

            var servicio = await _context.Servicios
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.UserId == uid);
            if (servicio == null) return NotFound();

            servicio.IsDeleted = true;
            servicio.UserId = uid;                 // quién elimina
            servicio.UpdatedAt = DateTime.UtcNow;  // UTC
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // HELPERS
        private async Task CargarAnimalesAsync(int? seleccionado = null)
        {
            var uid = CurrentUserId();

            var animales = await _context.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.userId == uid)
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
            var currentUserId = CurrentUserId(); // destinatario/auditoría

            var alertas = new List<Alerta>
            {
                new Alerta{
                    AnimalId=s.AnimalId, Tipo=TipoAlerta.ChequeoGestacion, Estado=EstadoAlerta.Pendiente,
                    FechaObjetivo=baseDate.AddDays(32), Disparador=$"Servicio #{s.Id} del {baseDate:yyyy-MM-dd}",
                    Notas="Chequeo gestación (~32d)", DestinatarioUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, userId = currentUserId
                },
                new Alerta{
                    AnimalId=s.AnimalId, Tipo=TipoAlerta.Secado, Estado=EstadoAlerta.Pendiente,
                    FechaObjetivo=baseDate.AddDays(210), Disparador=$"Servicio #{s.Id} del {baseDate:yyyy-MM-dd}",
                    Notas="Secado estimado (~210d)", DestinatarioUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, userId = currentUserId
                },
                new Alerta{
                    AnimalId=s.AnimalId, Tipo=TipoAlerta.PartoProbable, Estado=EstadoAlerta.Pendiente,
                    FechaObjetivo=baseDate.AddDays(283), Disparador=$"Servicio #{s.Id} del {baseDate:yyyy-MM-dd}",
                    Notas="Parto probable (~283d)", DestinatarioUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, userId = currentUserId
                }
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

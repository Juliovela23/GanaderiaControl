using System;
using System.Linq;
using System.Threading.Tasks;
using GanaderiaControl.Data;
using GanaderiaControl.Models;
using GanaderiaControl.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GanaderiaControl.Controllers
{
    [Authorize]
    public class PartosController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public PartosController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ================= PARTOS =================

        // LISTADO
        public async Task<IActionResult> Index(string? q)
        {
            var userId = _userManager.GetUserId(User);

            var query = _db.Partos
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.userId == userId)
                .Include(p => p.Madre)
                .OrderByDescending(p => p.FechaParto).ThenByDescending(p => p.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p =>
                    (p.Madre.Arete != null && p.Madre.Arete.Contains(q)) ||
                    (p.Madre.Nombre != null && p.Madre.Nombre.Contains(q)) ||
                    ((p.Observaciones ?? "").Contains(q)) ||
                    ((p.Asistencia ?? "").Contains(q)) ||
                    ((p.Complicaciones ?? "").Contains(q)));
            }

            var list = await query.Take(200).ToListAsync();
            ViewBag.Q = q;
            return View(list);
        }

        // DETALLE
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var model = await _db.Partos
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.userId == userId)
                .Include(p => p.Madre)
                .Include(p => p.Crias)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (model == null) return NotFound();
            return View(model);
        }

        // ===== CREATE (Parto + Cría + (opcional) Animal) =====

        // GET
        public async Task<IActionResult> Create(int? madreId)
        {
            await CargarMadres(madreId);
            return View(new PartoCreateVM
            {
                MadreId = madreId ?? 0,
                FechaParto = DateTime.Today,
                TipoParto = TipoParto.Normal,
                RetencionPlacenta = false,
                CrearCria = true,
                RegistrarCriaComoAnimal = true
            });
        }

        // POST
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PartoCreateVM vm)
        {
            var userId = _userManager.GetUserId(User);

            // Validación de madre
            var madreOk = await _db.Animales.AnyAsync(a => a.Id == vm.MadreId && !a.IsDeleted);
            if (!madreOk)
                ModelState.AddModelError(nameof(vm.MadreId), "Animal (madre) inválido.");

            vm.FechaParto = vm.FechaParto.Date;

            // Validaciones de cría si se desea crear
            if (vm.CrearCria)
            {
                if (vm.CriaSexo == null)
                    ModelState.AddModelError(nameof(vm.CriaSexo), "Seleccione el sexo de la cría.");

                if (vm.RegistrarCriaComoAnimal)
                {
                    if (string.IsNullOrWhiteSpace(vm.CriaAreteAsignado))
                        ModelState.AddModelError(nameof(vm.CriaAreteAsignado), "Indique el arete para registrar el animal.");
                    else
                    {
                        var yaExisteArete = await _db.Animales.AnyAsync(a => !a.IsDeleted && a.Arete == vm.CriaAreteAsignado);
                        if (yaExisteArete)
                            ModelState.AddModelError(nameof(vm.CriaAreteAsignado), "Ya existe un animal con ese arete.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarMadres(vm.MadreId);
                return View(vm);
            }

            // Transacción: Parto -> Cría -> Animal (opcional)
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var parto = new Parto
                {
                    MadreId = vm.MadreId,
                    FechaParto = vm.FechaParto,
                    TipoParto = vm.TipoParto,
                    RetencionPlacenta = vm.RetencionPlacenta,
                    Asistencia = vm.Asistencia,
                    Observaciones = vm.Observaciones,
                    Complicaciones = vm.Complicaciones,
                    userId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Partos.Add(parto);
                await _db.SaveChangesAsync();

                Animal? animalCria = null;

                // Crear cría
                if (vm.CrearCria)
                {
                    var cria = new Cria
                    {
                        PartoId = parto.Id,
                        Sexo = vm.CriaSexo!.Value,
                        PesoNacimientoKg = vm.CriaPesoNacimientoKg,
                        AreteAsignado = vm.CriaAreteAsignado,
                        Estado = vm.CriaEstado,
                        Observaciones = vm.CriaObservaciones,
                        userId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Crias.Add(cria);
                    await _db.SaveChangesAsync();

                    // Registrar Animal hijo (opcional)
                    if (vm.RegistrarCriaComoAnimal && !string.IsNullOrWhiteSpace(vm.CriaAreteAsignado))
                    {
                        animalCria = new Animal
                        {
                            Arete = vm.CriaAreteAsignado!.Trim(),
                            Nombre = null,
                            Raza = null,
                            FechaNacimiento = vm.FechaParto,
                            EstadoReproductivo = EstadoReproductivo.Abierta,
                            MadreId = vm.MadreId,
                            PadreId = null,
                            userId = userId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.Animales.Add(animalCria);
                        await _db.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();

                TempData["Ok"] = animalCria == null
                    ? "Parto y cría registrados correctamente."
                    : $"Parto, cría y animal '{animalCria.Arete}' registrados correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "No se pudo guardar: " + pex.MessageText);
                await CargarMadres(vm.MadreId);
                return View(vm);
            }
        }

        // ===== EDIT =====
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            var model = await _db.Partos
                .Where(p => !p.IsDeleted && p.userId == userId)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (model == null) return NotFound();

            await CargarMadres(model.MadreId);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MadreId,FechaParto,TipoParto,RetencionPlacenta,Asistencia,Observaciones,Complicaciones")] Parto model)
        {
            var userId = _userManager.GetUserId(User);
            if (id != model.Id) return NotFound();

            var madreOk = await _db.Animales.AnyAsync(a => a.Id == model.MadreId && !a.IsDeleted);
            if (!madreOk)
                ModelState.AddModelError(nameof(model.MadreId), "Animal (madre) inválido.");

            model.FechaParto = model.FechaParto.Date;

            if (!ModelState.IsValid)
            {
                await CargarMadres(model.MadreId);
                return View(model);
            }

            var current = await _db.Partos.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.userId == userId);
            if (current == null) return NotFound();

            current.MadreId = model.MadreId;
            current.FechaParto = model.FechaParto;
            current.TipoParto = model.TipoParto;
            current.RetencionPlacenta = model.RetencionPlacenta;
            current.Asistencia = model.Asistencia;
            current.Observaciones = model.Observaciones;
            current.Complicaciones = model.Complicaciones;
            current.userId = userId;
            current.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
                TempData["Ok"] = "Parto actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                ModelState.AddModelError(string.Empty, "Error al guardar: " + pex.MessageText);
                await CargarMadres(model.MadreId);
                return View(model);
            }
        }

        // ===== DELETE =====
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            var model = await _db.Partos
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.userId == userId)
                .Include(p => p.Madre)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

            var model = await _db.Partos.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.userId == userId);
            if (model == null) return NotFound();

            model.IsDeleted = true;
            model.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Parto eliminado.";
            return RedirectToAction(nameof(Index));
        }

        // ================= CRIAS (DENTRO DE PARTOS) =================

        // LISTAR CRIAS POR PARTO
        public async Task<IActionResult> Crias(int partoId)
        {
            var userId = _userManager.GetUserId(User);
            var parto = await _db.Partos
                .AsNoTracking()
                .Include(p => p.Madre)
                .FirstOrDefaultAsync(p => p.Id == partoId && !p.IsDeleted && p.userId == userId);
            if (parto == null) return NotFound();

            var list = await _db.Crias
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.PartoId == partoId && c.userId == userId)
                .OrderBy(c => c.Id)
                .ToListAsync();

            ViewBag.Parto = parto;
            return View(list);
        }

        // DETALLE CRIA
        public async Task<IActionResult> CriaDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var cria = await _db.Crias
                .AsNoTracking()
                .Include(c => c.Parto).ThenInclude(p => p.Madre)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.userId == userId);

            if (cria == null) return NotFound();
            return View(cria);
        }

        // CREATE CRIA (GET)
        public async Task<IActionResult> CriaCreate(int partoId)
        {
            var userId = _userManager.GetUserId(User);
            var parto = await _db.Partos
                .AsNoTracking()
                .Include(p => p.Madre)
                .FirstOrDefaultAsync(p => p.Id == partoId && !p.IsDeleted && p.userId == userId);
            if (parto == null) return NotFound();

            ViewBag.Parto = parto;
            return View(new Cria
            {
                PartoId = partoId
            });
        }

        // CREATE CRIA (POST)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CriaCreate([Bind("PartoId,Sexo,PesoNacimientoKg,AreteAsignado,Estado,Observaciones")] Cria model)
        {
            var userId = _userManager.GetUserId(User);

            // validar pertenencia del parto
            var partoOk = await _db.Partos.AnyAsync(p => p.Id == model.PartoId && !p.IsDeleted && p.userId == userId);
            if (!partoOk)
                ModelState.AddModelError(nameof(model.PartoId), "Parto inválido.");

            if (!ModelState.IsValid)
            {
                ViewBag.Parto = await _db.Partos.Include(p => p.Madre)
                    .FirstOrDefaultAsync(p => p.Id == model.PartoId);
                return View(model);
            }

            try
            {
                model.userId = userId;
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                _db.Crias.Add(model);
                await _db.SaveChangesAsync();

                TempData["Ok"] = "Cría registrada.";
                return RedirectToAction(nameof(Crias), new { partoId = model.PartoId });
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                ModelState.AddModelError(string.Empty, "No se pudo guardar: " + pex.MessageText);
                ViewBag.Parto = await _db.Partos.Include(p => p.Madre)
                    .FirstOrDefaultAsync(p => p.Id == model.PartoId);
                return View(model);
            }
        }

        // EDIT CRIA (GET)
        public async Task<IActionResult> CriaEdit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var model = await _db.Crias
                .Include(c => c.Parto)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.userId == userId);
            if (model == null) return NotFound();

            ViewBag.Parto = await _db.Partos.Include(p => p.Madre)
                .FirstAsync(p => p.Id == model.PartoId);
            return View(model);
        }

        // EDIT CRIA (POST)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CriaEdit(int id, [Bind("Id,PartoId,Sexo,PesoNacimientoKg,AreteAsignado,Estado,Observaciones")] Cria model)
        {
            var userId = _userManager.GetUserId(User);
            if (id != model.Id) return NotFound();

            // validar pertenencia del parto
            var partoOk = await _db.Partos.AnyAsync(p => p.Id == model.PartoId && !p.IsDeleted && p.userId == userId);
            if (!partoOk)
                ModelState.AddModelError(nameof(model.PartoId), "Parto inválido.");

            if (!ModelState.IsValid)
            {
                ViewBag.Parto = await _db.Partos.Include(p => p.Madre)
                    .FirstOrDefaultAsync(p => p.Id == model.PartoId);
                return View(model);
            }

            var current = await _db.Crias.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.userId == userId);
            if (current == null) return NotFound();

            current.Sexo = model.Sexo;
            current.PesoNacimientoKg = model.PesoNacimientoKg;
            current.AreteAsignado = model.AreteAsignado;
            current.Estado = model.Estado;
            current.Observaciones = model.Observaciones;
            current.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
                TempData["Ok"] = "Cría actualizada.";
                return RedirectToAction(nameof(Crias), new { partoId = current.PartoId });
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex)
            {
                ModelState.AddModelError(string.Empty, "Error al guardar: " + pex.MessageText);
                ViewBag.Parto = await _db.Partos.Include(p => p.Madre)
                    .FirstOrDefaultAsync(p => p.Id == model.PartoId);
                return View(model);
            }
        }

        // DELETE CRIA (GET)
        public async Task<IActionResult> CriaDelete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var model = await _db.Crias
                .AsNoTracking()
                .Include(c => c.Parto).ThenInclude(p => p.Madre)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.userId == userId);

            if (model == null) return NotFound();
            return View(model);
        }

        // DELETE CRIA (POST - soft)
        [HttpPost, ActionName("CriaDelete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> CriaDeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var model = await _db.Crias.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.userId == userId);
            if (model == null) return NotFound();

            model.IsDeleted = true;
            model.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Cría eliminada.";
            return RedirectToAction(nameof(Crias), new { partoId = model.PartoId });
        }

        // ================= HELPERS =================
        private async Task CargarMadres(int? madreId = null)
        {
            var animales = await _db.Animales
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.Arete)
                .Select(a => new { a.Id, Etiqueta = a.Arete + (a.Nombre != null ? " - " + a.Nombre : "") })
                .ToListAsync();

            ViewBag.MadreId = new SelectList(animales, "Id", "Etiqueta", madreId);
        }

        public async Task<IActionResult> CriasAll(string? q)
        {
            var userId = _userManager.GetUserId(User);

            var query = _db.Crias
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.userId == userId)
                .Include(c => c.Parto)
                    .ThenInclude(p => p.Madre)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(c =>
                    (c.AreteAsignado != null && c.AreteAsignado.Contains(q)) ||
                    ((c.Estado ?? "").Contains(q)) ||
                    ((c.Observaciones ?? "").Contains(q)) ||
                    (c.Parto.Madre.Arete != null && c.Parto.Madre.Arete.Contains(q)) ||
                    (c.Parto.Madre.Nombre != null && c.Parto.Madre.Nombre.Contains(q))
                );
            }

            var list = await query
                .OrderByDescending(c => c.Parto.FechaParto)
                .ThenByDescending(c => c.Id)
                .Take(300)
                .ToListAsync();

            ViewBag.Q = q;
            return View(list);
        }
    }
}

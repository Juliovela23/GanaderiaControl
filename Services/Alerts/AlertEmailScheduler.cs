using GanaderiaControl.Data;
using GanaderiaControl.Models;
using GanaderiaControl.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace GanaderiaControl.Services.Alerts;

public class AlertEmailScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertEmailScheduler> _logger;
    private readonly TimeSpan _runAtLocal; // 07:30 AM GT

    public AlertEmailScheduler(IServiceScopeFactory scopeFactory, ILogger<AlertEmailScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _runAtLocal = new TimeSpan(7, 30, 0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertEmailScheduler iniciado.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetDelayUntilNextRun();
                await Task.Delay(delay, stoppingToken);
                await RunOnceAsync(stoppingToken);
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AlertEmailScheduler.");
            }
        }
    }

    private static DateTime GetNowGuatemala()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch { return DateTime.UtcNow; }
    }

    private TimeSpan GetDelayUntilNextRun()
    {
        
        return TimeSpan.FromMinutes(1);
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mail = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var emailOpts = scope.ServiceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;
        var resolver = scope.ServiceProvider.GetRequiredService<IAlertRecipientResolver>();

        var hoyLocal = GetNowGuatemala().Date;

        var alertas = await db.Alertas
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.Estado == EstadoAlerta.Pendiente)
            .Include(a => a.Animal)
            .ToListAsync(ct);

        var pendientes = new List<(Models.Alerta alerta, int offset)>();
        foreach (var a in alertas)
        {
            var dias = (a.FechaObjetivo.Date - hoyLocal).Days;
            if (dias == 15 && !a.Aviso15Enviado) pendientes.Add((a, 15));
            if (dias == 7 && !a.Aviso7Enviado) pendientes.Add((a, 7));
            if (dias == 0 && !a.Aviso0Enviado) pendientes.Add((a, 0));
        }

        if (pendientes.Count == 0) return;

        // Rastrear para marcar flags
        var ids = pendientes.Select(p => p.alerta.Id).ToList();
        var track = await db.Alertas.Where(a => ids.Contains(a.Id)).ToListAsync(ct);

        foreach (var (alerta, offset) in pendientes)
        {
            try
            {
                var email = await resolver.GetEmailAsync(alerta, ct);

                if (string.IsNullOrWhiteSpace(email))
                {
                    // Fallback opcional (si configuraste DefaultTo)
                    if (!string.IsNullOrWhiteSpace(emailOpts.DefaultTo))
                    {
                        _logger.LogWarning("Alerta {Id} no tiene destinatario Identity. Usando fallback {Fallback}.", alerta.Id, emailOpts.DefaultTo);
                        email = emailOpts.DefaultTo!;
                    }
                    else
                    {
                        _logger.LogWarning("Alerta {Id} sin destinatario. Se omite el envío.", alerta.Id);
                        continue;
                    }
                }

                var subject = BuildSubject(alerta, offset);
                var body = BuildBodyHtml(alerta, offset);

                await mail.SendAsync(email, subject, body, ct);

                var t = track.First(x => x.Id == alerta.Id);
                var nowUtc = DateTime.UtcNow;
                if (offset == 15) { t.Aviso15Enviado = true; t.Aviso15EnviadoUtc = nowUtc; }
                if (offset == 7) { t.Aviso7Enviado = true; t.Aviso7EnviadoUtc = nowUtc; }
                if (offset == 0) { t.Aviso0Enviado = true; t.Aviso0EnviadoUtc = nowUtc; }
                t.UpdatedAt = nowUtc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando correo de alerta {AlertId} (offset {Offset}).", alerta.Id, offset);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static string BuildSubject(Models.Alerta a, int offset) =>
        offset switch
        {
            15 => $"[Alerta] {a.Tipo} en 15 días – {(a.Animal?.Arete ?? $"Animal {a.AnimalId}")}",
            7 => $"[Alerta] {a.Tipo} en 7 días – {(a.Animal?.Arete ?? $"Animal {a.AnimalId}")}",
            0 => $"[Alerta] ¡HOY! {a.Tipo} – {(a.Animal?.Arete ?? $"Animal {a.AnimalId}")}",
            _ => $"[Alerta] {a.Tipo} – {(a.Animal?.Arete ?? $"Animal {a.AnimalId}")}"
        };

    private static string BuildBodyHtml(Models.Alerta a, int offset)
    {
        var titulo = offset switch
        {
            15 => "Recordatorio a 15 días",
            7 => "Recordatorio a 7 días",
            0 => "¡La alerta es HOY!",
            _ => "Detalle de alerta"
        };

        var arete = a.Animal?.Arete ?? $"Animal {a.AnimalId}";
        var nombre = string.IsNullOrWhiteSpace(a.Animal?.Nombre) ? "" : $" - {a.Animal!.Nombre}";

        var sb = new StringBuilder();
        sb.Append("<div style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222'>");
        sb.Append($"<h2>{titulo}</h2>");
        sb.Append("<ul>");
        sb.Append($"<li><b>Animal:</b> {arete}{nombre}</li>");
        sb.Append($"<li><b>Tipo:</b> {a.Tipo}</li>");
        sb.Append($"<li><b>Fecha objetivo:</b> {a.FechaObjetivo:yyyy-MM-dd}</li>");
        sb.Append($"<li><b>Estado:</b> {a.Estado}</li>");
        if (!string.IsNullOrWhiteSpace(a.Disparador)) sb.Append($"<li><b>Disparador:</b> {a.Disparador}</li>");
        if (!string.IsNullOrWhiteSpace(a.Notas)) sb.Append($"<li><b>Notas:</b> {a.Notas}</li>");
        sb.Append("</ul>");
        sb.Append("<p>— GanaderiaControl</p>");
        sb.Append("</div>");
        return sb.ToString();
    }
}

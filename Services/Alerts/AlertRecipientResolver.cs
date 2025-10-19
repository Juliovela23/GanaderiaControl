using GanaderiaControl.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GanaderiaControl.Services.Alerts;

public class AlertRecipientResolver : IAlertRecipientResolver
{
    private readonly UserManager<IdentityUser> _userManager;

    public AlertRecipientResolver(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GetEmailAsync(Alerta alerta, CancellationToken ct = default)
    {
        // 1) Si la alerta tiene destinatario explícito en Identity, úsalo
        if (!string.IsNullOrWhiteSpace(alerta.DestinatarioUserId))
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == alerta.DestinatarioUserId, ct);

            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                return user.Email;
        }

        // 2) (Opcional futuro) si tu Animal tiene propietario con UserId, úsalo.
        // if (!string.IsNullOrWhiteSpace(alerta.Animal?.PropietarioUserId)) { ... }

        // 3) Sin destinatario válido -> null (scheduler podrá usar fallback si lo configuras)
        return null;
    }
}
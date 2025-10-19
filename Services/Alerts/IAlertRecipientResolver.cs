using GanaderiaControl.Models;

namespace GanaderiaControl.Services.Alerts
{
    public interface IAlertRecipientResolver
    {
        /// Devuelve el email del destinatario para esta alerta (o null si no hay).
        Task<string?> GetEmailAsync(Alerta alerta, CancellationToken ct = default);
    }
}

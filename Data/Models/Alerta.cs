using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GanaderiaControl.Models;

public class Alerta : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public TipoAlerta Tipo { get; set; }
    public DateTime FechaObjetivo { get; set; }
    public EstadoAlerta Estado { get; set; } = EstadoAlerta.Pendiente;
    [MaxLength(240)] public string? Disparador { get; set; }
    [MaxLength(240)] public string? Notas { get; set; }

    // === NUEVO: destinatario Identity ===
    [MaxLength(450)] // tamaño típico de PK en AspNetUsers
    public string? DestinatarioUserId { get; set; }

    [ForeignKey(nameof(DestinatarioUserId))]
    public IdentityUser? DestinatarioUser { get; set; }

    // === Flags anti-duplicados (si ya los agregaste, deja tal cual) ===
    public bool Aviso15Enviado { get; set; }
    public DateTime? Aviso15EnviadoUtc { get; set; }
    public bool Aviso7Enviado { get; set; }
    public DateTime? Aviso7EnviadoUtc { get; set; }
    public bool Aviso0Enviado { get; set; }
    public DateTime? Aviso0EnviadoUtc { get; set; }
    public string? userId { get; set; }
}

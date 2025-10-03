using GanaderiaControl.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GanaderiaControl.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    
    public DbSet<Animal> Animales => Set<Animal>();
    public DbSet<ServicioReproductivo> Servicios => Set<ServicioReproductivo>();
    public DbSet<ChequeoGestacion> Chequeos => Set<ChequeoGestacion>();
    public DbSet<Secado> Secados => Set<Secado>();
    public DbSet<Parto> Partos => Set<Parto>();
    public DbSet<Cria> Crias => Set<Cria>();
    public DbSet<Lactancia> Lactancias => Set<Lactancia>();
    public DbSet<RegistroLeche> RegistrosLeche => Set<RegistroLeche>();
    public DbSet<EventoSalud> EventosSalud => Set<EventoSalud>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<ChequeoGestacion> ChequeosGestacion => Set<ChequeoGestacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // (Opcional) Si quieres que todo vaya al esquema "public"
        // modelBuilder.HasDefaultSchema("public");

        // Filtros soft-delete
        modelBuilder.Entity<Animal>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ServicioReproductivo>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<ChequeoGestacion>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Secado>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Parto>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Cria>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Lactancia>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<RegistroLeche>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<EventoSalud>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Alerta>().HasQueryFilter(x => !x.IsDeleted);

        // ------- Animal -------
        modelBuilder.Entity<Animal>(e =>
        {
            e.HasIndex(x => x.Arete).IsUnique();
            e.Property(x => x.EstadoReproductivo).HasConversion<int>();

            e.HasOne(x => x.Madre)
                .WithMany()
                .HasForeignKey(x => x.MadreId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.Padre)
                .WithMany()
                .HasForeignKey(x => x.PadreId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ------- ServicioReproductivo -------
        modelBuilder.Entity<ServicioReproductivo>(e =>
        {
            e.HasIndex(x => new { x.AnimalId, x.FechaServicio });
            e.Property(x => x.Tipo).HasConversion<int>();
            e.Property(x => x.FechaServicio).HasColumnType("date"); // OK en Postgres

            e.HasOne(x => x.Animal)
                .WithMany(a => a.Servicios)
                .HasForeignKey(x => x.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ------- ChequeoGestacion -------
        modelBuilder.Entity<ChequeoGestacion>(e =>
        {
            e.Property(x => x.Resultado).HasConversion<int>();
            e.HasIndex(x => new { x.AnimalId, x.FechaChequeo });
            e.Property(x => x.FechaChequeo).HasColumnType("date");

            e.HasOne(x => x.Animal)
                .WithMany(a => a.Chequeos)
                .HasForeignKey(x => x.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ServicioReproductivo)
                .WithMany()
                .HasForeignKey(x => x.ServicioReproductivoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Secado -------
        modelBuilder.Entity<Secado>(e =>
        {
            e.HasIndex(x => new { x.AnimalId, x.FechaSecado });
            e.Property(x => x.FechaSecado).HasColumnType("date");

            e.HasOne(x => x.Animal)
                .WithMany(a => a.Secados)
                .HasForeignKey(x => x.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ------- Parto -------
        modelBuilder.Entity<Parto>(e =>
        {
            e.Property(x => x.TipoParto).HasConversion<int>();
            e.HasIndex(x => new { x.MadreId, x.FechaParto }).IsUnique();
            e.Property(x => x.FechaParto).HasColumnType("date");

            e.HasOne(x => x.Madre)
                .WithMany(a => a.Partos)
                .HasForeignKey(x => x.MadreId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ------- Cria -------
        modelBuilder.Entity<Cria>(e =>
        {
            e.Property(x => x.Sexo).HasConversion<int>();
            // Antes: HasColumnType("decimal(10,2)")  -> Postgres:
            e.Property(x => x.PesoNacimientoKg).HasPrecision(10, 2);

            e.HasOne(x => x.Parto)
                .WithMany(p => p.Crias)
                .HasForeignKey(x => x.PartoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ------- Lactancia -------
        modelBuilder.Entity<Lactancia>(e =>
        {
            e.Property(x => x.ProduccionPromedioDiaLitros).HasPrecision(10, 2);
            e.Property(x => x.FechaInicio).HasColumnType("date");
            e.Property(x => x.FechaFin).HasColumnType("date");

            e.HasOne(x => x.Animal)
                .WithMany(a => a.Lactancias)
                .HasForeignKey(x => x.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check constraint (elige UNA de las dos versiones):
            // Si NO usas UseSnakeCaseNamingConvention():
            e.HasCheckConstraint("CK_Lactancia_Fechas", "\"FechaFin\" IS NULL OR \"FechaFin\" >= \"FechaInicio\"");
            // Si SÍ usas UseSnakeCaseNamingConvention(), usa esta en cambio:
            // e.HasCheckConstraint("ck_lactancia_fechas", "fecha_fin IS NULL OR fecha_fin >= fecha_inicio");
        });

        // ------- RegistroLeche -------
        modelBuilder.Entity<RegistroLeche>(e =>
        {
            e.HasIndex(x => new { x.AnimalId, x.Fecha }).IsUnique();
            e.Property(x => x.LitrosDia).HasPrecision(10, 2);
            e.Property(x => x.Fecha).HasColumnType("date");

            e.HasOne(x => x.Animal)
                .WithMany(a => a.RegistrosLeche)
                .HasForeignKey(x => x.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ------- EventoSalud -------
        modelBuilder.Entity<EventoSalud>(e =>
        {
            e.Property(x => x.Fecha).HasColumnType("date");
            e.HasOne(x => x.Animal)
                .WithMany(a => a.EventosSalud)
                .HasForeignKey(x => x.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ------- Alerta -------
        modelBuilder.Entity<Alerta>(e =>
        {
            e.Property(x => x.Tipo).HasConversion<int>();
            e.Property(x => x.Estado).HasConversion<int>();
            e.Property(x => x.FechaObjetivo).HasColumnType("date");
            e.HasIndex(x => new { x.AnimalId, x.Tipo, x.FechaObjetivo });

            e.HasOne(x => x.Animal)
                .WithMany(a => a.Alertas)
                .HasForeignKey(x => x.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ------- Seed (valores constantes, NO DateTime.UtcNow) -------
        modelBuilder.Entity<Animal>().HasData(
            new Animal
            {
                Id = 1,
                Arete = "BV-0001",
                Nombre = "Luna",
                Raza = "Holstein",
                EstadoReproductivo = EstadoReproductivo.Abierta,
                // Fecha fija en UTC (constante) para que compile la migración:
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }

}

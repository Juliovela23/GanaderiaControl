using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GanaderiaControl.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GanaderiaControl.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Animal> Animales => Set<Animal>();
        public DbSet<ServicioReproductivo> Servicios => Set<ServicioReproductivo>();
        public DbSet<ChequeoGestacion> ChequeosGestacion => Set<ChequeoGestacion>();
        public DbSet<Secado> Secados => Set<Secado>();
        public DbSet<Parto> Partos => Set<Parto>();
        public DbSet<Cria> Crias => Set<Cria>();
        public DbSet<Lactancia> Lactancias => Set<Lactancia>();
        public DbSet<RegistroLeche> RegistrosLeche => Set<RegistroLeche>();
        public DbSet<EventoSalud> EventosSalud => Set<EventoSalud>();
        public DbSet<Alerta> Alertas => Set<Alerta>();

        // ================== NORMALIZA DateTime A UTC ANTES DE GUARDAR ==================
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            NormalizeDateTimesToUtc();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            NormalizeDateTimesToUtc();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void NormalizeDateTimesToUtc()
        {
            var nowUtc = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                // Seteo de auditoría si existen las propiedades
                if (entry.State == EntityState.Added)
                {
                    if (entry.CurrentValues.Properties.Any(p => p.Name == "CreatedAt"))
                        entry.CurrentValues["CreatedAt"] = nowUtc;
                    if (entry.CurrentValues.Properties.Any(p => p.Name == "UpdatedAt"))
                        entry.CurrentValues["UpdatedAt"] = nowUtc;
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (entry.CurrentValues.Properties.Any(p => p.Name == "UpdatedAt"))
                        entry.CurrentValues["UpdatedAt"] = nowUtc;
                }

                // Fuerza Kind=Utc para todo DateTime que NO sea columna 'date'
                foreach (var prop in entry.Properties.Where(p => p.Metadata.ClrType == typeof(DateTime)))
                {
                    var dt = (DateTime?)prop.CurrentValue;
                    if (!dt.HasValue) continue;

                    var isDateColumn = string.Equals(prop.Metadata.GetColumnType(), "date", StringComparison.OrdinalIgnoreCase);
                    if (isDateColumn) continue; // fechas de calendario, sin zona

                    if (dt.Value.Kind != DateTimeKind.Utc)
                        prop.CurrentValue = DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
                }
            }
        }

        // ================== CONVERTERS UTC PARA AUDITORÍA ==================
        private static readonly ValueConverter<DateTime, DateTime> UtcConverter =
            new ValueConverter<DateTime, DateTime>(
                v => v,                                           // write (ya viene normalizado a UTC)
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));  // read

        private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableConverter =
            new ValueConverter<DateTime?, DateTime?>(
                v => v,                                           // write
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v); // read

        // ================== MODELO / MAPEOS ==================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Soft-delete global ---
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
                e.HasIndex(x => x.Arete)
                 .IsUnique()
                 .HasFilter("\"IsDeleted\" = FALSE");

                e.Property(x => x.EstadoReproductivo).HasConversion<int>();
                e.Property(x => x.FechaNacimiento).HasColumnType("date"); // fecha de calendario

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
                e.Property(x => x.FechaServicio).HasColumnType("date");

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

                e.HasCheckConstraint("CK_Lactancia_Fechas", "\"FechaFin\" IS NULL OR \"FechaFin\" >= \"FechaInicio\"");
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

            // ------- Seed (opcional) -------
            modelBuilder.Entity<Animal>().HasData(
                new Animal
                {
                    Id = 1,
                    Arete = "BV-0001",
                    Nombre = "Luna",
                    Raza = "Holstein",
                    EstadoReproductivo = EstadoReproductivo.Abierta,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // ========= Config global: CreatedAt/UpdatedAt => timestamptz + Kind=Utc al leer =========
            ConfigureUtcAuditColumns(modelBuilder);
        }

        /// <summary>
        /// Recorre todas las entidades y, si existen propiedades CreatedAt/UpdatedAt
        /// (DateTime o DateTime?), las mapea a "timestamp with time zone" y asegura Kind=Utc al leer.
        /// </summary>
        private void ConfigureUtcAuditColumns(ModelBuilder modelBuilder)
        {
            foreach (var et in modelBuilder.Model.GetEntityTypes())
            {
                var entity = modelBuilder.Entity(et.ClrType);

                var created = et.FindProperty("CreatedAt");
                if (created != null)
                {
                    if (created.ClrType == typeof(DateTime))
                    {
                        entity.Property<DateTime>(created.Name)
                              .HasColumnType("timestamp with time zone")
                              .HasConversion(UtcConverter)
                              .HasDefaultValueSql("CURRENT_TIMESTAMP"); // opcional
                    }
                    else if (created.ClrType == typeof(DateTime?))
                    {
                        entity.Property<DateTime?>(created.Name)
                              .HasColumnType("timestamp with time zone")
                              .HasConversion(UtcNullableConverter)
                              .HasDefaultValueSql("CURRENT_TIMESTAMP"); // opcional
                    }
                }

                var updated = et.FindProperty("UpdatedAt");
                if (updated != null)
                {
                    if (updated.ClrType == typeof(DateTime))
                    {
                        entity.Property<DateTime>(updated.Name)
                              .HasColumnType("timestamp with time zone")
                              .HasConversion(UtcConverter)
                              .HasDefaultValueSql("CURRENT_TIMESTAMP"); // opcional
                    }
                    else if (updated.ClrType == typeof(DateTime?))
                    {
                        entity.Property<DateTime?>(updated.Name)
                              .HasColumnType("timestamp with time zone")
                              .HasConversion(UtcNullableConverter)
                              .HasDefaultValueSql("CURRENT_TIMESTAMP"); // opcional
                    }
                }
            }
        }
    }
}


using Microsoft.EntityFrameworkCore;
using MonitoreoAulas.Api.Domain.Entities;

namespace MonitoreoAulas.Api.Data;

public class MonitoreoDbContext : DbContext
{
    public MonitoreoDbContext(
        DbContextOptions<MonitoreoDbContext> options)
        : base(options)
    {
    }

    public DbSet<Aula> Aulas => Set<Aula>();

    public DbSet<EventoOcupacion> EventosOcupacion => Set<EventoOcupacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Aula>(entity =>
        {
            entity.ToTable("Aulas", "dbo", table =>
            {
                table.HasCheckConstraint(
                    "CK_Aulas_Capacidad",
                    "[Capacidad] > 0");
            });

            entity.HasKey(x => x.IdAula);

            entity.Property(x => x.IdAula)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            entity.HasIndex(x => x.Codigo)
                .IsUnique();

            entity.Property(x => x.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Edificio)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(x => x.Capacidad)
                .IsRequired();

            entity.Property(x => x.Activa)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<EventoOcupacion>(entity =>
        {
            entity.ToTable("EventosOcupacion", "dbo", table =>
            {
                table.HasCheckConstraint(
                    "CK_Eventos_Ocupacion",
                    "[Ocupacion] >= 0");

                table.HasCheckConstraint(
                    "CK_Eventos_Tipo",
                    "[TipoEvento] IN (" +
                    "'INICIO','ENTRADA','SALIDA'," +
                    "'ESTADO','CAPACIDAD_MAXIMA','AULA_VACIA')");

                table.HasCheckConstraint(
                    "CK_Eventos_TiempoArduino",
                    "[TiempoArduinoMs] IS NULL OR " +
                    "[TiempoArduinoMs] >= 0");

                table.HasCheckConstraint(
                    "CK_Eventos_Secuencia",
                    "[Secuencia] IS NULL OR " +
                    "[Secuencia] >= 0");
            });

            entity.HasKey(x => x.IdEvento);

            entity.Property(x => x.IdEvento)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.TipoEvento)
                .HasMaxLength(25)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(x => x.Ocupacion)
                .IsRequired();

            entity.Property(x => x.FechaHora)
                .HasColumnType("datetime2(3)")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(x => x.IdSesion)
                .HasMaxLength(36)
                .IsUnicode(false);

            entity.HasOne(x => x.Aula)
                .WithMany(x => x.EventosOcupacion)
                .HasForeignKey(x => x.IdAula)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.IdAula,
                x.FechaHora,
                x.IdEvento
            })
            .IsDescending(false, true, true)
            .HasDatabaseName(
                "IX_Eventos_Aula_FechaHora");

            entity.HasIndex(x => new
            {
                x.IdAula,
                x.IdSesion,
                x.Secuencia
            })
            .IsUnique()
            .HasFilter(
                "[IdSesion] IS NOT NULL AND " +
                "[Secuencia] IS NOT NULL")
            .HasDatabaseName(
                "UX_Eventos_Sesion_Secuencia");
        });
    }
}

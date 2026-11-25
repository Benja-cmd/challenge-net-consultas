using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Models;

namespace ConsultasPlan.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Afiliado> Afiliados { get; set; }
    public DbSet<Consulta> Consultas { get; set; }
    public DbSet<Profesional> Profesionales { get; set; }
    public DbSet<Sede> Sedes { get; set; }
    public DbSet<PoliticaCupo> PoliticasCupo { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Afiliado>()
            .HasIndex(a => a.DNI)
            .IsUnique();

        modelBuilder.Entity<Consulta>()
            .Property(c => c.Estado)
            .HasConversion<string>();

        modelBuilder.Entity<Consulta>()
            .Property(c => c.Modalidad)
            .HasConversion<string>();

        modelBuilder.Entity<Consulta>()
            .HasOne(c => c.Afiliado)
            .WithMany()
            .HasForeignKey(c => c.AfiliadoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ninguna baja debe arrastrar historia clínica: se impide borrar sedes con
        // profesionales o profesionales con consultas.
        modelBuilder.Entity<Consulta>()
            .HasOne(c => c.Profesional)
            .WithMany()
            .HasForeignKey(c => c.ProfesionalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Profesional>()
            .HasOne(p => p.Sede)
            .WithMany()
            .HasForeignKey(p => p.SedeId)
            .OnDelete(DeleteBehavior.Restrict);

        var politica = modelBuilder.Entity<PoliticaCupo>();
        politica.ToTable("PoliticasCupo");
        politica.Property(p => p.Modalidad).HasConversion<string>();
        politica.HasIndex(p => new { p.Plan, p.Especialidad, p.Modalidad }).IsUnique();

        // Política pedida en el challenge. Se carga por migración para que llegue también
        // a bases ya existentes en producción.
        politica.HasData(new PoliticaCupo
        {
            Id = 1,
            Plan = "Plan Integral",
            Especialidad = "Nutrición",
            Modalidad = ModalidadConsulta.Presencial,
            LimiteAnual = 6,
            LimiteMensual = 1,
            InasistenciaConsumeCupo = true
        });
    }
}

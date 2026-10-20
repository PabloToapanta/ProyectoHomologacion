using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var carrera = modelBuilder.Entity<Carrera>();
        carrera.ToTable("Carreras");
        carrera.HasKey(c => c.Id);
        carrera.Property(c => c.Id).ValueGeneratedNever();
        carrera.Property(c => c.Nombre).HasMaxLength(200).IsRequired();
        carrera.Property(c => c.Facultad).HasMaxLength(200).IsRequired();
        carrera.Property(c => c.Modalidad).HasMaxLength(100);
        carrera.Property(c => c.NombreNormalizado).HasMaxLength(200).IsRequired();
        carrera.Property(c => c.FacultadNormalizada).HasMaxLength(200).IsRequired();
        if (Database.IsSqlServer())
        {
            carrera.Property(c => c.NombreNormalizado).UseCollation("Latin1_General_100_BIN2");
            carrera.Property(c => c.FacultadNormalizada).UseCollation("Latin1_General_100_BIN2");
        }
        carrera.HasIndex(c => new { c.NombreNormalizado, c.FacultadNormalizada }).IsUnique();

        var asignatura = modelBuilder.Entity<Asignatura>();
        asignatura.ToTable("Asignaturas", t =>
        {
            t.HasCheckConstraint("CK_Asignaturas_NivelPositivo", "[Nivel] > 0");
            t.HasCheckConstraint("CK_Asignaturas_CreditosPositivos", "[Creditos] > 0");
            t.HasCheckConstraint("CK_Asignaturas_HorasPositivas", "[Horas] > 0");
        });
        asignatura.HasKey(a => a.Id);
        asignatura.Property(a => a.Id).ValueGeneratedNever();
        asignatura.Property(a => a.Codigo).HasMaxLength(50).IsRequired();
        asignatura.Property(a => a.CodigoNormalizado).HasMaxLength(50).IsRequired();
        if (Database.IsSqlServer()) asignatura.Property(a => a.CodigoNormalizado).UseCollation("Latin1_General_100_BIN2");
        asignatura.Property(a => a.Nombre).HasMaxLength(200).IsRequired();
        asignatura.Property(a => a.Creditos).HasPrecision(9, 2);
        asignatura.HasIndex(a => new { a.CarreraId, a.CodigoNormalizado }).IsUnique();
        asignatura.HasOne<Carrera>().WithMany().HasForeignKey(a => a.CarreraId).OnDelete(DeleteBehavior.Restrict);
    }
}

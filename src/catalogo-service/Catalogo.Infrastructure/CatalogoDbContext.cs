using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var perfil = modelBuilder.Entity<PerfilComparacion>();
        perfil.ToTable("PerfilesComparacion");
        perfil.HasKey(p => p.AsignaturaId);
        perfil.Property(p => p.AsignaturaId).ValueGeneratedNever();
        perfil.Property(p => p.PlanAnalitico).HasMaxLength(500).IsRequired();
        perfil.Property(p => p.UnidadHoras).HasMaxLength(100).IsRequired();
        perfil.Property(p => p.SistemaCreditos).HasMaxLength(100).IsRequired();
        perfil.HasOne<Asignatura>().WithMany().HasForeignKey(p => p.AsignaturaId).OnDelete(DeleteBehavior.Restrict);
        var revision = modelBuilder.Entity<RevisionComparacion>();
        revision.ToTable("RevisionesComparacion");
        revision.HasKey(r => new { r.OrigenId, r.ReceptoraId });
        revision.Property(r => r.FirmaOrigen).HasMaxLength(64).IsRequired();
        revision.Property(r => r.FirmaReceptora).HasMaxLength(64).IsRequired();
        revision.Property(r => r.DatosJson).IsRequired();
        revision.HasOne<Asignatura>().WithMany().HasForeignKey(r => r.OrigenId).OnDelete(DeleteBehavior.Restrict);
        revision.HasOne<Asignatura>().WithMany().HasForeignKey(r => r.ReceptoraId).OnDelete(DeleteBehavior.Restrict);
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
        var unidad = modelBuilder.Entity<UnidadCurricular>();
        unidad.ToTable("UnidadesCurriculares", t => {
            t.HasCheckConstraint("CK_Unidades_Numero", "[Numero] > 0");
            t.HasCheckConstraint("CK_Unidades_Horas", "[Horas] > 0");
        });
        unidad.HasKey(u => u.Id);
        unidad.Property(u => u.Id).ValueGeneratedNever();
        unidad.Property(u => u.Nombre).HasMaxLength(200).IsRequired();
        unidad.Property(u => u.ResultadoAprendizaje).HasMaxLength(4000).IsRequired();
        unidad.HasIndex(u => new { u.AsignaturaId, u.Numero }).IsUnique();
        unidad.HasOne<Asignatura>().WithMany().HasForeignKey(u => u.AsignaturaId).OnDelete(DeleteBehavior.Restrict);
        var tematica = modelBuilder.Entity<UnidadTematica>();
        tematica.ToTable("UnidadesTematicas");
        tematica.HasKey(t => t.Id);
        tematica.Property(t => t.Id).ValueGeneratedNever();
        tematica.Property(t => t.Nombre).HasMaxLength(200).IsRequired();
        tematica.HasIndex(t => new { t.UnidadCurricularId, t.Orden }).IsUnique();
        unidad.HasMany(u => u.Tematicas).WithOne().HasForeignKey(t => t.UnidadCurricularId).OnDelete(DeleteBehavior.Cascade);
        var contenido = modelBuilder.Entity<Contenido>();
        contenido.ToTable("Contenidos");
        contenido.HasOne<UnidadTematica>().WithMany().HasForeignKey(c => c.UnidadTematicaId).OnDelete(DeleteBehavior.Restrict);
        contenido.HasKey(c => c.Id);
        contenido.Property(c => c.Id).ValueGeneratedNever();
        contenido.Property(c => c.Tema).HasMaxLength(4000).IsRequired();
        contenido.HasIndex(c => new { c.UnidadCurricularId, c.Orden }).IsUnique();
        unidad.HasMany(u => u.Contenidos).WithOne().HasForeignKey(c => c.UnidadCurricularId).OnDelete(DeleteBehavior.Cascade);
    }
}

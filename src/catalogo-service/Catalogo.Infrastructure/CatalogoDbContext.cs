using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure;

public sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();
    public DbSet<Malla> Mallas => Set<Malla>();
    public DbSet<Normativa> Normativas => Set<Normativa>();
    public DbSet<EventoNormativa> EventosNormativa => Set<EventoNormativa>();

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

        var malla = modelBuilder.Entity<Malla>();
        malla.ToTable("Mallas");
        malla.HasKey(m => m.Id);
        malla.Property(m => m.Id).ValueGeneratedNever();
        malla.HasAlternateKey(m => new { m.Id, m.CarreraId });
        malla.Property(m => m.Nombre).HasMaxLength(100).IsRequired();
        malla.Property(m => m.NombreNormalizado).HasMaxLength(100).IsRequired();
        if (Database.IsSqlServer()) malla.Property(m => m.NombreNormalizado).UseCollation("Latin1_General_100_BIN2");
        malla.HasIndex(m => new { m.CarreraId, m.NombreNormalizado }).IsUnique();
        malla.HasIndex(m => m.CarreraId).IsUnique().HasFilter("[Vigente] = 1");
        malla.HasOne<Carrera>().WithMany().HasForeignKey(m => m.CarreraId).OnDelete(DeleteBehavior.Cascade);

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
        asignatura.HasIndex(a => new { a.MallaId, a.CodigoNormalizado }).IsUnique();
        asignatura.HasOne<Malla>().WithMany().HasForeignKey(a => new { a.MallaId, a.CarreraId })
            .HasPrincipalKey(m => new { m.Id, m.CarreraId }).OnDelete(DeleteBehavior.Restrict);
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
        // HU-08: PDF original del programa analítico confirmado, uno por asignatura, para trazabilidad.
        var programa = modelBuilder.Entity<ProgramaAnalitico>();
        programa.ToTable("ProgramasAnaliticos", t => t.HasCheckConstraint("CK_ProgramasAnaliticos_Tamano", "[Tamano] > 0"));
        programa.HasKey(p => p.Id);
        programa.Property(p => p.Id).ValueGeneratedNever();
        programa.Property(p => p.NombreArchivo).HasMaxLength(200).IsRequired();
        programa.Property(p => p.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
        programa.Property(p => p.Contenido).IsRequired();
        programa.Property(p => p.CargadoPor).HasMaxLength(256).IsRequired();
        programa.HasIndex(p => p.AsignaturaId).IsUnique();
        programa.HasOne<Asignatura>().WithMany().HasForeignKey(p => p.AsignaturaId).OnDelete(DeleteBehavior.Restrict);
        // HU-38: normativas institucionales con su PDF; el historial no tiene clave foránea para sobrevivir a la eliminación.
        var normativa = modelBuilder.Entity<Normativa>();
        normativa.ToTable("Normativas", t => t.HasCheckConstraint("CK_Normativas_Tamano", "[Tamano] > 0"));
        normativa.HasKey(n => n.Id);
        normativa.Property(n => n.Id).ValueGeneratedNever();
        normativa.Property(n => n.Codigo).HasMaxLength(50).IsRequired();
        normativa.Property(n => n.CodigoNormalizado).HasMaxLength(50).IsRequired();
        normativa.HasIndex(n => n.CodigoNormalizado).IsUnique();
        normativa.Property(n => n.Titulo).HasMaxLength(300).IsRequired();
        normativa.Property(n => n.Emisor).HasMaxLength(200).IsRequired();
        normativa.Property(n => n.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        normativa.Property(n => n.MotivoDerogacion).HasMaxLength(500);
        normativa.Property(n => n.NombreArchivo).HasMaxLength(200).IsRequired();
        normativa.Property(n => n.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
        normativa.Property(n => n.Contenido).IsRequired();
        normativa.Property(n => n.RegistradaPor).HasMaxLength(256).IsRequired();
        var evento = modelBuilder.Entity<EventoNormativa>();
        evento.ToTable("EventosNormativa");
        evento.HasKey(e => e.Id);
        evento.Property(e => e.Id).ValueGeneratedNever();
        evento.Property(e => e.CodigoNormativa).HasMaxLength(50).IsRequired();
        evento.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        evento.Property(e => e.Usuario).HasMaxLength(256).IsRequired();
        evento.Property(e => e.Detalle).HasMaxLength(600);
        evento.HasIndex(e => e.NormativaId);
    }
}

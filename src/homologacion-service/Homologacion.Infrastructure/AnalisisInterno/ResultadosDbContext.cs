using Homologacion.Application.AnalisisInterno;
using Homologacion.Infrastructure.AnalisisExterno;
using Homologacion.Infrastructure.RevisionDocente;
using Homologacion.Infrastructure.Informe;
using Homologacion.Infrastructure.Configuracion;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.Json;

namespace Homologacion.Infrastructure.AnalisisInterno;

// Modelo técnico propio de Homologación: no tiene FK ni acceso a bases ajenas.
public sealed class RegistroResultado : IRegistroAnalisis
{
    public Guid SolicitudId { get; set; }
    public Guid AprobadaId { get; set; }
    public Guid ReceptoraId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string Contenido { get; set; } = "";
}
public sealed class ResultadosDbContext(DbContextOptions<ResultadosDbContext> options) : DbContext(options)
{
    public DbSet<RegistroResultado> Resultados => Set<RegistroResultado>();
    /// <summary>HU-19: análisis externos en tabla propia; no altera AnalisisInternos.</summary>
    public DbSet<RegistroResultadoExterno> ResultadosExternos => Set<RegistroResultadoExterno>();
    /// <summary>HU-31: análisis propio de cada docente asignado por pareja.</summary>
    public DbSet<RegistroRevisionDocente> RevisionesDocentes => Set<RegistroRevisionDocente>();
    /// <summary>HU-23: un informe fijo por solicitud.</summary>
    public DbSet<RegistroInforme> Informes => Set<RegistroInforme>();
    /// <summary>HU-25: historial del informe, solo de inserción.</summary>
    public DbSet<RegistroEventoInforme> EventosInforme => Set<RegistroEventoInforme>();
    /// <summary>HU-33: decisión del coordinador por pareja «Sin acuerdo».</summary>
    public DbSet<RegistroDecisionCoordinador> DecisionesCoordinador => Set<RegistroDecisionCoordinador>();
    public DbSet<RegistroConfiguracionHomologacion> Configuraciones => Set<RegistroConfiguracionHomologacion>();
    public DbSet<ConfiguracionHomologacionVigente> ConfiguracionVigente => Set<ConfiguracionHomologacionVigente>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        Homologacion.Infrastructure.Agrupaciones.ModeloGruposHu21.Configurar(model);
        var r = model.Entity<RegistroResultado>();
        r.ToTable("AnalisisInternos");
        r.HasKey(x => new { x.SolicitudId, x.AprobadaId, x.ReceptoraId });
        r.Property(x => x.Contenido).IsRequired();
        var e = model.Entity<RegistroResultadoExterno>();
        e.ToTable("AnalisisExternos");
        e.HasKey(x => new { x.SolicitudId, x.AprobadaId, x.ReceptoraId });
        e.Property(x => x.Contenido).IsRequired();
        var d = model.Entity<RegistroRevisionDocente>();
        d.ToTable("RevisionesDocentes");
        d.HasKey(x => new { x.SolicitudId, x.AprobadaId, x.ReceptoraId, x.DocenteId });
        d.Property(x => x.Docente).HasMaxLength(256).IsRequired();
        d.Property(x => x.Postura).HasMaxLength(20).IsRequired();
        d.Property(x => x.Decision).HasMaxLength(20).IsRequired();
        d.Property(x => x.Observacion).HasMaxLength(2000);
        d.Property(x => x.EstadoAgente).HasMaxLength(20).IsRequired();
        d.Property(x => x.SugerenciaAgente).HasMaxLength(50);
        d.HasIndex(x => x.SolicitudId);
        var i = model.Entity<RegistroInforme>();
        i.ToTable("InformesHomologacion");
        i.HasKey(x => x.SolicitudId);
        i.Property(x => x.SolicitudId).ValueGeneratedNever();
        i.Property(x => x.Contenido).IsRequired();
        var v = model.Entity<RegistroEventoInforme>();
        v.ToTable("EventosInforme");
        v.HasKey(x => x.Id);
        v.Property(x => x.Id).ValueGeneratedNever();
        v.Property(x => x.Tipo).HasMaxLength(20).IsRequired();
        v.Property(x => x.Usuario).HasMaxLength(256).IsRequired();
        v.Property(x => x.Rol).HasMaxLength(30).IsRequired();
        v.HasIndex(x => new { x.SolicitudId, x.Tipo }).IsUnique();
        var c = model.Entity<RegistroDecisionCoordinador>();
        c.ToTable("DecisionesCoordinador");
        c.HasKey(x => new { x.SolicitudId, x.AprobadaId, x.ReceptoraId });
        c.Property(x => x.Resultado).HasMaxLength(20).IsRequired();
        c.Property(x => x.Justificacion).HasMaxLength(2000).IsRequired();
        c.Property(x => x.Coordinador).HasMaxLength(256).IsRequired();
        var cfg = model.Entity<RegistroConfiguracionHomologacion>();
        cfg.ToTable("ConfiguracionesHomologacion");
        cfg.HasKey(x => x.VersionId);
        cfg.HasOne<RegistroConfiguracionHomologacion>().WithMany()
            .HasForeignKey(x => x.VersionAnteriorId).OnDelete(DeleteBehavior.Restrict);
        cfg.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        cfg.Property(x => x.Respaldo).HasMaxLength(2000).IsRequired();
        cfg.Property(x => x.RowVersion).IsRequired().IsRowVersion();
        var vigente = model.Entity<ConfiguracionHomologacionVigente>();
        vigente.ToTable("ConfiguracionHomologacionVigente");
        vigente.HasKey(x => x.Id);
        vigente.Property(x => x.Id).ValueGeneratedNever();
        vigente.Property(x => x.VersionId).IsRequired();
        vigente.HasOne<RegistroConfiguracionHomologacion>().WithMany()
            .HasForeignKey(x => x.VersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class ResultadosAnalisisSql(ResultadosDbContext db) : IResultadosAnalisisInterno
{
    public Task GuardarAsync(AnalisisGuardado resultado, CancellationToken ct) => AlmacenPorPareja.GuardarAsync(db, db.Resultados, resultado, ct);
    public Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid solicitudId, CancellationToken ct) => AlmacenPorPareja.ListarAsync(db.Resultados, solicitudId, ct);
}

/// <summary>Fila técnica común: clave de pareja, fecha del último intento y resultado serializado.</summary>
public interface IRegistroAnalisis
{
    Guid SolicitudId { get; set; }
    Guid AprobadaId { get; set; }
    Guid ReceptoraId { get; set; }
    DateTimeOffset Fecha { get; set; }
    string Contenido { get; set; }
}

/// <summary>Política de HU-20 compartida por análisis internos y externos: el último intento por pareja reemplaza al anterior.</summary>
internal static class AlmacenPorPareja
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static async Task GuardarAsync<T>(DbContext db, DbSet<T> set, AnalisisGuardado resultado, CancellationToken ct) where T : class, IRegistroAnalisis, new()
    {
        if (db is not ResultadosDbContext contexto) throw new InvalidOperationException("Se requiere el contexto propio de Homologación.");
        try
        {
            await new InformesHomologacionSql(contexto).ConCierreAsync(resultado.Analisis.SolicitudId, async () =>
            {
                var a = resultado.Analisis;
                if (await contexto.Informes.AnyAsync(i => i.SolicitudId == a.SolicitudId, ct)) throw new Homologacion.Application.Informe.InformeYaGeneradoException();
                await Homologacion.Infrastructure.Agrupaciones.ReservasOrigenSql.ExigirParejaAsync(contexto, a.SolicitudId, a.AsignaturaAprobadaId, a.AsignaturaReceptoraId, ct);
                await GuardarParejaAsync(db, set, resultado, ct); return true;
            }, ct);
        }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (DbUpdateException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }
    private static async Task GuardarParejaAsync<T>(DbContext db, DbSet<T> set, AnalisisGuardado resultado, CancellationToken ct) where T : class, IRegistroAnalisis, new()
    {
        try
        {
            var a = resultado.Analisis;
            var row = await set.FindAsync([a.SolicitudId, a.AsignaturaAprobadaId, a.AsignaturaReceptoraId], ct);
            if (row is null)
            {
                row = new() { SolicitudId = a.SolicitudId, AprobadaId = a.AsignaturaAprobadaId, ReceptoraId = a.AsignaturaReceptoraId };
                set.Add(row);
            }
            row.Fecha = resultado.Fecha;
            row.Contenido = JsonSerializer.Serialize(a, JsonOptions);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }
    public static async Task<IReadOnlyList<AnalisisGuardado>> ListarAsync<T>(DbSet<T> set, Guid solicitudId, CancellationToken ct) where T : class, IRegistroAnalisis
    {
        try
        {
            var rows = await set.AsNoTracking().Where(x => EF.Property<Guid>(x, nameof(IRegistroAnalisis.SolicitudId)) == solicitudId).ToArrayAsync(ct);
            return rows.Select(row =>
            {
                var a = JsonSerializer.Deserialize<ResultadoInternoSolicitud>(row.Contenido, JsonOptions);
                if (a is null || a.SolicitudId != row.SolicitudId || a.AsignaturaAprobadaId != row.AprobadaId || a.AsignaturaReceptoraId != row.ReceptoraId)
                    throw new AlmacenAnalisisNoDisponibleException();
                return new AnalisisGuardado(row.Fecha, a);
            }).OrderByDescending(x => x.Fecha).ToArray();
        }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (JsonException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }
}

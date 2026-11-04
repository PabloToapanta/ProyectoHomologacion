using Homologacion.Application.AnalisisInterno;
using Homologacion.Infrastructure.AnalisisExterno;
using Homologacion.Infrastructure.RevisionDocente;
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
    protected override void OnModelCreating(ModelBuilder model)
    {
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

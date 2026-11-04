using Homologacion.Application.AnalisisInterno;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.Json;

namespace Homologacion.Infrastructure.AnalisisInterno;

// Modelo técnico propio de Homologación: no tiene FK ni acceso a bases ajenas.
public sealed class RegistroResultado
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
    protected override void OnModelCreating(ModelBuilder model)
    {
        var r = model.Entity<RegistroResultado>();
        r.ToTable("AnalisisInternos");
        r.HasKey(x => new { x.SolicitudId, x.AprobadaId, x.ReceptoraId });
        r.Property(x => x.Contenido).IsRequired();
    }
}
public sealed class ResultadosAnalisisSql(ResultadosDbContext db) : IResultadosAnalisisInterno
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task GuardarAsync(AnalisisGuardado resultado, CancellationToken ct)
    {
        try
        {
            var a = resultado.Analisis;
            var row = await db.Resultados.FindAsync([a.SolicitudId, a.AsignaturaAprobadaId, a.AsignaturaReceptoraId], ct);
            if (row is null)
            {
                row = new() { SolicitudId = a.SolicitudId, AprobadaId = a.AsignaturaAprobadaId, ReceptoraId = a.AsignaturaReceptoraId };
                db.Resultados.Add(row);
            }
            row.Fecha = resultado.Fecha;
            row.Contenido = JsonSerializer.Serialize(a, JsonOptions);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }
    public async Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid solicitudId, CancellationToken ct)
    {
        try
        {
            var rows = await db.Resultados.AsNoTracking().Where(x => x.SolicitudId == solicitudId).ToArrayAsync(ct);
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

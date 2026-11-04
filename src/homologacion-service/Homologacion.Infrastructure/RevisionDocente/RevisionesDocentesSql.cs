using System.Data.Common;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.RevisionDocente;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.EntityFrameworkCore;

namespace Homologacion.Infrastructure.RevisionDocente;

/// <summary>HU-31: fila de RevisionesDocentes, sin FK ni acceso a bases ajenas.</summary>
public sealed class RegistroRevisionDocente
{
    public Guid SolicitudId { get; set; }
    public Guid AprobadaId { get; set; }
    public Guid ReceptoraId { get; set; }
    public Guid DocenteId { get; set; }
    public string Docente { get; set; } = "";
    public string Postura { get; set; } = "";
    public string Decision { get; set; } = "";
    public string? Observacion { get; set; }
    public string EstadoAgente { get; set; } = "";
    public string? SugerenciaAgente { get; set; }
    public DateTimeOffset Fecha { get; set; }
}

public sealed class RevisionesDocentesSql(ResultadosDbContext db) : IRevisionesDocentes
{
    public async Task GuardarAsync(RevisionDocenteRegistrada r, CancellationToken ct)
    {
        try
        {
            var fila = await db.RevisionesDocentes.FindAsync([r.SolicitudId, r.AsignaturaAprobadaId, r.AsignaturaReceptoraId, r.DocenteId], ct);
            if (fila is null)
            {
                fila = new() { SolicitudId = r.SolicitudId, AprobadaId = r.AsignaturaAprobadaId, ReceptoraId = r.AsignaturaReceptoraId, DocenteId = r.DocenteId };
                db.RevisionesDocentes.Add(fila);
            }
            fila.Docente = r.Docente; fila.Postura = r.Postura; fila.Decision = r.Decision; fila.Observacion = r.Observacion;
            fila.EstadoAgente = r.EstadoAgente; fila.SugerenciaAgente = r.SugerenciaAgente; fila.Fecha = r.Fecha;
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }

    public async Task<IReadOnlyList<RevisionDocenteRegistrada>> ListarAsync(Guid solicitudId, CancellationToken ct)
    {
        try
        {
            return (await db.RevisionesDocentes.AsNoTracking().Where(x => x.SolicitudId == solicitudId).ToArrayAsync(ct))
                .Select(x => new RevisionDocenteRegistrada(x.SolicitudId, x.AprobadaId, x.ReceptoraId, x.DocenteId, x.Docente, x.Postura, x.Decision,
                    x.Observacion, x.EstadoAgente, x.SugerenciaAgente, x.Fecha)).ToArray();
        }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }
}

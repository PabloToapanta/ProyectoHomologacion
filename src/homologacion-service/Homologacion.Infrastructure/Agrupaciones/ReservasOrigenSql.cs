using Homologacion.Domain.Agrupaciones;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.EntityFrameworkCore;
namespace Homologacion.Infrastructure.Agrupaciones;

internal static class ReservasOrigenSql
{
    // Tanto el análisis automatizado como la primera revisión manual constituyen una pareja.
    public static async Task ExigirParejaAsync(ResultadosDbContext db, Guid solicitud, Guid origen, Guid receptora, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("La reserva requiere el cierre transaccional de la solicitud.");
        var reserva = await db.Set<ReservaOrigen>().FindAsync([solicitud, origen], ct);
        if (reserva is not null)
        {
            if (reserva.GrupoId is not null || reserva.ReceptoraParejaId != receptora) throw new ConflictoGrupoException("El origen está reservado en otra unidad de análisis.");
            return;
        }
        var propia = await db.Resultados.AnyAsync(x => x.SolicitudId == solicitud && x.AprobadaId == origen && x.ReceptoraId == receptora, ct)
            || await db.ResultadosExternos.AnyAsync(x => x.SolicitudId == solicitud && x.AprobadaId == origen && x.ReceptoraId == receptora, ct)
            || await db.RevisionesDocentes.AnyAsync(x => x.SolicitudId == solicitud && x.AprobadaId == origen && x.ReceptoraId == receptora, ct);
        if (propia) return; // Mantiene corregibles las parejas históricas, incluso si el legado repitió un origen.
        if (await db.Resultados.AnyAsync(x => x.SolicitudId == solicitud && x.AprobadaId == origen, ct)
            || await db.ResultadosExternos.AnyAsync(x => x.SolicitudId == solicitud && x.AprobadaId == origen, ct)
            || await db.RevisionesDocentes.AnyAsync(x => x.SolicitudId == solicitud && x.AprobadaId == origen, ct))
            throw new ConflictoGrupoException("El origen pertenece a una pareja histórica de la solicitud.");
        db.Add(new ReservaOrigen { SolicitudId = solicitud, AprobadaId = origen, ReceptoraParejaId = receptora });
    }
}

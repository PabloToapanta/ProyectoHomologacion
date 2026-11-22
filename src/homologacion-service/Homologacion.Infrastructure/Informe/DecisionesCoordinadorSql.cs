using System.Data.Common;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.EntityFrameworkCore;

namespace Homologacion.Infrastructure.Informe;

/// <summary>HU-33: fila de DecisionesCoordinador; una por pareja «Sin acuerdo» (clave solicitud + pareja).</summary>
public sealed class RegistroDecisionCoordinador
{
    public Guid SolicitudId { get; set; }
    public Guid AprobadaId { get; set; }
    public Guid ReceptoraId { get; set; }
    public string Resultado { get; set; } = "";
    public string Justificacion { get; set; } = "";
    public Guid CoordinadorId { get; set; }
    public string Coordinador { get; set; } = "";
    public DateTimeOffset Fecha { get; set; }
}

/// <summary>Comparte el DbContext con el informe: dentro de ConCierreAsync la decisión se confirma en la misma transacción.</summary>
public sealed class DecisionesCoordinadorSql(ResultadosDbContext db) : IDecisionesCoordinador
{
    public async Task GuardarAsync(DecisionCoordinador decision, CancellationToken ct)
    {
        try
        {
            var fila = await db.DecisionesCoordinador.FindAsync([decision.SolicitudId, decision.AsignaturaAprobadaId, decision.AsignaturaReceptoraId], ct);
            if (fila is null)
            {
                fila = new RegistroDecisionCoordinador
                    { SolicitudId = decision.SolicitudId, AprobadaId = decision.AsignaturaAprobadaId, ReceptoraId = decision.AsignaturaReceptoraId };
                db.DecisionesCoordinador.Add(fila);
            }
            (fila.Resultado, fila.Justificacion, fila.CoordinadorId, fila.Coordinador, fila.Fecha) =
                (decision.Resultado, decision.Justificacion, decision.CoordinadorId, decision.Coordinador, decision.Fecha);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception error) when (error is DbUpdateException or DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }

    public async Task<IReadOnlyList<DecisionCoordinador>> ListarAsync(Guid solicitudId, CancellationToken ct)
    {
        try
        {
            return (await db.DecisionesCoordinador.AsNoTracking().Where(x => x.SolicitudId == solicitudId).ToArrayAsync(ct))
                .OrderBy(x => x.Fecha).ThenBy(x => x.AprobadaId).ThenBy(x => x.ReceptoraId)
                .Select(x => new DecisionCoordinador(x.SolicitudId, x.AprobadaId, x.ReceptoraId, x.Resultado, x.Justificacion, x.CoordinadorId, x.Coordinador, x.Fecha))
                .ToArray();
        }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }
}

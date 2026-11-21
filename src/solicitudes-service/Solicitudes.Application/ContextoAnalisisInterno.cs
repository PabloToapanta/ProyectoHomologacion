namespace Solicitudes.Application;

public sealed record AsignaturaContextoInterno(Guid Id, Guid? AsignaturaOrigenId, decimal Creditos, int Horas, string Estado, DateOnly FechaAprobacion);
public sealed record SolicitudContextoInterno(Guid Id, string TipoCambio, Guid? CarreraOrigenId, Guid CarreraReceptoraId,
    IReadOnlyList<AsignaturaContextoInterno> AsignaturasAprobadas);

public static class ConsultarContextoAnalisisInterno
{
    public static async Task<SolicitudContextoInterno?> EjecutarAsync(ISolicitudRepository repository, Guid solicitudId, Guid docenteId, CancellationToken ct)
    {
        if (docenteId == Guid.Empty || !(await repository.DocentesDe(solicitudId, ct)).Any(d => d.DocenteId == docenteId))
            return null;
        var solicitud = await repository.Obtener(solicitudId, ct);
        if (solicitud is null) return null;
        var materias = await repository.AsignaturasDe(solicitudId, ct);
        return new(solicitud.Id, solicitud.TipoCambio, solicitud.CarreraOrigenId, solicitud.CarreraReceptoraId,
            materias.Select(a => new AsignaturaContextoInterno(a.Id, a.AsignaturaOrigenId, a.Creditos, a.Horas, a.Estado, a.FechaAprobacion)).ToArray());
    }
}

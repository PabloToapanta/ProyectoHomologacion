namespace Solicitudes.Application;

/// <summary>HU-19: datos académicos de la asignatura externa, sin matrícula ni nota.</summary>
public sealed record AsignaturaContextoExterno(Guid Id, string Nombre, int Horas, string? UnidadHoras, decimal Creditos, string? SistemaCreditos,
    string? ReferenciaPrograma, string Estado, DateOnly FechaAprobacion);
/// <summary>HU-19: sin Quipux, identificación ni nombre del estudiante. Períodos y cambios previos null = sin dato.</summary>
public sealed record SolicitudContextoExterno(Guid Id, string TipoCambio, Guid CarreraReceptoraId, string? InstitucionOrigen, string? CarreraOrigenExterna,
    int? PeriodosCursados, int? CambiosPreviosCarrera, IReadOnlyList<AsignaturaContextoExterno> AsignaturasAprobadas);

public static class ConsultarContextoAnalisisExterno
{
    public static async Task<SolicitudContextoExterno?> EjecutarAsync(ISolicitudRepository repository, Guid solicitudId, Guid docenteId, CancellationToken ct)
    {
        if (docenteId == Guid.Empty || !await repository.EstaAsignada(solicitudId, docenteId, ct)) return null;
        if (await repository.Obtener(solicitudId, ct) is not { } s) return null;
        var materias = await repository.AsignaturasDe(solicitudId, ct);
        return new(s.Id, s.TipoCambio, s.CarreraReceptoraId, s.InstitucionOrigen, s.CarreraOrigenExterna, s.PeriodosCursados, s.CambiosPreviosCarrera,
            materias.Select(a => new AsignaturaContextoExterno(a.Id, a.NombreAsignatura, a.Horas, a.UnidadHoras, a.Creditos, a.SistemaCreditos,
                a.ReferenciaPrograma, a.Estado, a.FechaAprobacion)).ToArray());
    }
}

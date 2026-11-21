using Homologacion.Application.AnalisisInterno;

namespace Homologacion.Application.AnalisisExterno;

/// <summary>HU-19: asignatura de otra universidad ecuatoriana (SC-01) tal como la registró el Coordinador en HU-11.</summary>
public sealed record AprobadaExternaParaAnalisis(Guid Id, string Nombre, int Horas, string? UnidadHoras, decimal Creditos, string? SistemaCreditos,
    string? ReferenciaPrograma, string Estado, DateOnly FechaAprobacion);
/// <summary>HU-19: contexto mínimo de Solicitudes. Períodos y cambios previos null = sin dato declarado (D4).</summary>
public sealed record SolicitudExternaParaAnalisis(Guid Id, string TipoCambio, Guid CarreraReceptoraId, string? InstitucionOrigen,
    string? CarreraOrigenExterna, int? PeriodosCursados, int? CambiosPreviosCarrera, IReadOnlyList<AprobadaExternaParaAnalisis> AsignaturasAprobadas);
public interface ISolicitudesAnalisisExterno
{
    /// <summary>Null si la solicitud no existe o no está asignada al docente autenticado.</summary>
    Task<SolicitudExternaParaAnalisis?> ObtenerSolicitudExternaAsync(Guid id, CancellationToken ct);
}
/// <summary>Último análisis externo por pareja, con la misma política de HU-20 y en una tabla propia.</summary>
public interface IResultadosAnalisisExterno
{
    Task GuardarAsync(AnalisisGuardado resultado, CancellationToken ct);
    Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid solicitudId, CancellationToken ct);
}
/// <summary>RN-05 externo. Estado: Cumple, NoCumple o Incompleto. Solo un dato declarado bajo el mínimo es incumplimiento.</summary>
public sealed record ResultadoRequisitosExternos(Guid SolicitudId, string Estado, int? PeriodosCursados, int? CambiosPreviosCarrera,
    int AsignaturasAprobadas, int AsignaturasConAnalisisCompleto, int AsignaturasHomologables,
    IReadOnlyList<string> Incumplimientos, IReadOnlyList<string> Faltantes, IReadOnlyList<string> Advertencias);

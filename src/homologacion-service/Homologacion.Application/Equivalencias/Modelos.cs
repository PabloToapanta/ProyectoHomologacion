using Homologacion.Domain.Equivalencias;

namespace Homologacion.Application.Equivalencias;

public sealed record DatosAsignatura(string? Id, string? PlanAnalitico, decimal? Horas, string? UnidadHoras,
    decimal? Creditos, string? SistemaCreditos, IReadOnlyList<string?>? Contenidos, IReadOnlyList<string?>? ResultadosAprendizaje);
public sealed record OrigenComparacion(DatosAsignatura? Asignatura, bool? Aprobada);
public sealed record EvidenciaAcademica(string? UbicacionOrigen, string? UbicacionReceptora);
public sealed record CorrespondenciaAcademica(string? AsignaturaOrigen, string? ElementoOrigen,
    string? ElementoReceptor, EvidenciaAcademica? Evidencia);
public sealed record SolicitudEquivalencia(IReadOnlyList<OrigenComparacion?>? Origenes, DatosAsignatura? Receptora,
    IReadOnlyList<CorrespondenciaAcademica?>? CorrespondenciasContenidos,
    IReadOnlyList<CorrespondenciaAcademica?>? CorrespondenciasResultadosAprendizaje);
public interface IUmbralEquivalencia { decimal Valor { get; } }
public sealed record EvaluacionEquivalencia(IReadOnlyList<string> AsignaturasOrigen, string? AsignaturaReceptora,
    decimal? CompatibilidadAcademicaPct, decimal? CompatibilidadHorariaPct, decimal? CompatibilidadCreditosPct,
    decimal UmbralPct, EstadoEquivalencia Estado, string Justificacion, IReadOnlyList<string> ContenidosSinCobertura,
    IReadOnlyList<string> ResultadosAprendizajeSinCobertura, IReadOnlyList<string> InformacionFaltante)
{
    public bool Homologable => Estado == EstadoEquivalencia.Homologable;
}
public sealed class DatosEvaluacionInvalidosException(string mensaje) : Exception(mensaje);

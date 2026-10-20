using Homologacion.Application.Equivalencias;

namespace Homologacion.Api.Contracts;

/// <summary>Tres criterios y estado explícito. Un porcentaje null no puede determinarse.</summary>
public sealed record EvaluarResponse(IReadOnlyList<string> AsignaturasOrigen, string? AsignaturaReceptora,
    decimal? CompatibilidadAcademicaPct, decimal? CompatibilidadHorariaPct, decimal? CompatibilidadCreditosPct,
    decimal UmbralPct, string Estado, bool Homologable, string Justificacion, IReadOnlyList<string> ContenidosSinCobertura,
    IReadOnlyList<string> ResultadosAprendizajeSinCobertura, IReadOnlyList<string> InformacionFaltante)
{
    public static EvaluarResponse FromApplication(EvaluacionEquivalencia r) => new(r.AsignaturasOrigen, r.AsignaturaReceptora,
        r.CompatibilidadAcademicaPct, r.CompatibilidadHorariaPct, r.CompatibilidadCreditosPct, r.UmbralPct,
        r.Estado.ToString(), r.Homologable, r.Justificacion, r.ContenidosSinCobertura, r.ResultadosAprendizajeSinCobertura, r.InformacionFaltante);
}

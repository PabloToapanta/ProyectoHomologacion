using Homologacion.Application.Equivalencias;

namespace Homologacion.Api.Contracts;

/// <summary>Conserva las claves del contrato propuesto. Un porcentaje null no está determinado.</summary>
public sealed record EvaluarResponse(string? AsignaturaOrigen, string? AsignaturaReceptora,
    decimal? ContenidoPct, decimal? ProfundidadPct, decimal? CargaHorariaPct, decimal? CreditosPct,
    decimal UmbralPct, bool Homologable, string Justificacion, IReadOnlyList<string> TemasSinCobertura)
{
    public static EvaluarResponse FromApplication(EvaluacionEquivalencia resultado) => new(
        resultado.AsignaturaOrigen, resultado.AsignaturaReceptora, resultado.ContenidoPct, resultado.ProfundidadPct,
        resultado.CargaHorariaPct, resultado.CreditosPct, resultado.UmbralPct, resultado.Homologable,
        resultado.Justificacion, resultado.TemasSinCobertura);
}

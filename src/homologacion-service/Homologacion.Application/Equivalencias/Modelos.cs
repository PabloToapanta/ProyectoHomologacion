using Homologacion.Domain.Equivalencias;

namespace Homologacion.Application.Equivalencias;

public sealed record DatosAsignatura(string? Id, decimal? Horas, decimal? Creditos, IReadOnlyList<string?>? Temas);
public sealed record CorrespondenciaTema(string? TemaOrigen, string? TemaReceptor);
public sealed record SolicitudEquivalencia(DatosAsignatura? Origen, DatosAsignatura? Receptora,
    IReadOnlyList<CorrespondenciaTema?>? Correspondencias);
public interface IUmbralEquivalencia { decimal Valor { get; } }
public sealed record EvaluacionEquivalencia(string? AsignaturaOrigen, string? AsignaturaReceptora,
    decimal? ContenidoPct, decimal? ProfundidadPct, decimal? CargaHorariaPct, decimal? CreditosPct,
    decimal UmbralPct, EstadoEquivalencia Estado, string Justificacion, IReadOnlyList<string> TemasSinCobertura)
{
    public bool Homologable => Estado == EstadoEquivalencia.Homologable;
}
public sealed class DatosEvaluacionInvalidosException(string mensaje) : Exception(mensaje);

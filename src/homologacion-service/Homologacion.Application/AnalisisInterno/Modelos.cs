using Homologacion.Application.Equivalencias;

namespace Homologacion.Application.AnalisisInterno;

public sealed record ReferenciaAnalisis(string Id, string Categoria, CorrespondenciaAcademica Correspondencia,
    string? TextoOrigen = null, string? TextoReceptor = null);
public sealed record TextoElementoAnalisis(string AsignaturaId, string Categoria, string ElementoId, string Texto);
public sealed record ContextoAgente(EvaluacionEquivalencia Evaluacion, IReadOnlyList<ReferenciaAnalisis> Evidencias);
public sealed record ExplicacionAgente(string Resumen, IReadOnlyList<string> EvidenciasCitadas);
public interface IAgenteAnalisisInterno
{
    Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken cancellationToken);
}
public enum TipoFalloAgente { NoDisponible, TiempoAgotado, RespuestaInvalida }
public sealed class FalloAgenteException(TipoFalloAgente tipo) : Exception("No se pudo validar la asistencia del agente.")
{
    public TipoFalloAgente Tipo { get; } = tipo;
}
public enum EstadoAsistencia { Completada, InformacionIncompleta, RevisionManual }
public sealed record AnalisisAsistido(EvaluacionEquivalencia Evaluacion, EstadoAsistencia Estado,
    ExplicacionAgente? Asistencia, TipoFalloAgente? Fallo, IReadOnlyList<ReferenciaAnalisis>? Referencias = null);


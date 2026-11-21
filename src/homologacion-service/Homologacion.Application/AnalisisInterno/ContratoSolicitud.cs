using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Antiguedad;

namespace Homologacion.Application.AnalisisInterno;

public sealed record AprobadaParaAnalisis(Guid Id, Guid? AsignaturaOrigenId, decimal Creditos, int Horas, string Estado, DateOnly FechaAprobacion);
public sealed record SolicitudParaAnalisis(Guid Id, string TipoCambio, Guid? CarreraOrigenId, Guid CarreraReceptoraId, IReadOnlyList<AprobadaParaAnalisis> AsignaturasAprobadas);
public sealed record ElementoParaAnalisis(Guid Id, string Texto);
public sealed record AsignaturaParaAnalisis(Guid Id, Guid CarreraId, string Facultad, bool CarreraVigente, decimal Creditos, int Horas,
    string? PlanAnalitico, string? UnidadHoras, string? SistemaCreditos, bool EsSintetico,
    IReadOnlyList<ElementoParaAnalisis> Contenidos, IReadOnlyList<ElementoParaAnalisis> Resultados);
public sealed record VinculoParaAnalisis(string ElementoOrigen, string ElementoReceptor, string UbicacionOrigen, string UbicacionReceptora);
public sealed record RevisionParaAnalisis(bool Registrada, bool Vigente, IReadOnlyList<VinculoParaAnalisis> Contenidos, IReadOnlyList<VinculoParaAnalisis> Resultados);
public interface ISolicitudesAnalisisInterno
{
    Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct);
}
public interface ICatalogoAnalisisInterno
{
    Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct);
    Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct);
}
public sealed record ResumenAnalisisInterno(decimal? PorcentajeAcademico, decimal? PorcentajeHoras, decimal? PorcentajeCreditos,
    string Sugerencia, string Justificacion, IReadOnlyList<string> Evidencias);
public sealed record ResultadoInternoSolicitud(Guid SolicitudId, Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId,
    string Estado, string Mensaje, ResumenAnalisisInterno? Resultado, EvaluacionEquivalencia Evaluacion,
    ResultadoAntiguedad? Antiguedad, bool DatosSinteticos, IReadOnlyList<string> Advertencias);
public sealed class SolicitudAnalisisNoEncontradaException() : Exception("Solicitud no encontrada entre sus asignaciones.");
public sealed class DatosAnalisisSolicitudException(string message) : Exception(message);
public sealed class FuenteAnalisisNoDisponibleException() : Exception("No fue posible verificar los datos académicos autorizados. Intente nuevamente.");

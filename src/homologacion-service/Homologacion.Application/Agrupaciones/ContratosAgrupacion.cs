using Homologacion.Domain.Agrupaciones;
using Homologacion.Application.Informe;
using Homologacion.Application.Equivalencias;
namespace Homologacion.Application.Agrupaciones;

public sealed record ComposicionGrupo(IReadOnlyList<Guid> Origenes, IReadOnlyList<Guid> Receptoras, IReadOnlyList<AporteGrupo> Aportes);
public sealed record DetalleGrupo(GrupoAgrupacion Grupo, AnalisisAgrupacionGuardado? UltimoAnalisis);
public sealed record AnalisisGrupoRespuesta(bool Guardado, ResultadoAgrupacion Evaluacion, DetalleGrupo Detalle,
    IReadOnlyList<string> Advertencias);
public sealed record OrigenOpcion(Guid Id, int Horas, decimal Creditos, string Estado, bool? Elegible, IReadOnlyList<string> Motivos);
public sealed record ReceptoraOpcion(Guid Id, string Codigo, string Nombre, int Nivel, int Horas, decimal Creditos);
public sealed record VinculoOpcion(Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, bool Habilitado, string Motivo);
public sealed record OpcionesAgrupacion(IReadOnlyList<OrigenOpcion> Origenes, IReadOnlyList<ReceptoraOpcion> Receptoras, IReadOnlyList<VinculoOpcion> Vinculos);
public interface IGruposAgrupacion
{
    Task<IReadOnlyList<DetalleGrupo>> ListarAsync(Guid solicitud, CancellationToken ct);
    Task<DetalleGrupo?> ObtenerAsync(Guid solicitud, Guid grupo, CancellationToken ct);
    Task GuardarAsync(GrupoAgrupacion grupo, AnalisisAgrupacionGuardado? analisis, CancellationToken ct);
    Task EliminarAsync(GrupoAgrupacion grupo, CancellationToken ct);
}
public sealed class GrupoNoEncontradoException() : Exception("Agrupación no encontrada en esta solicitud.");
public sealed class VersionGrupoRequeridaException() : Exception("Envíe If-Match con el ETag del grupo.");

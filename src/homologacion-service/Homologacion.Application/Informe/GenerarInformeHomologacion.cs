using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.RevisionDocente;

namespace Homologacion.Application.Informe;

public sealed record AgenteEnInforme(string Estado, string? Sugerencia, decimal? PorcentajeAcademico, decimal? PorcentajeHoras, decimal? PorcentajeCreditos);
public sealed record DocenteEnInforme(Guid DocenteId, string Docente, string Postura, string Decision, string? Observacion, DateTimeOffset Fecha);
/// <summary>SugerenciaFinal: Convalidar, NoConvalidar o SinAcuerdo (decide el coordinador).</summary>
public sealed record ParejaEnInforme(Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, string? ReceptoraCodigo, string? ReceptoraNombre,
    AgenteEnInforme Agente, IReadOnlyList<DocenteEnInforme> Docentes, string SugerenciaFinal);
/// <summary>HU-23: versión fija del informe; no es una resolución oficial.</summary>
public sealed record InformeHomologacion(Guid SolicitudId, string TipoCambio, DateTimeOffset Fecha, Guid GeneradoPorId, string GeneradoPor,
    IReadOnlyList<ParejaEnInforme> Parejas, IReadOnlyList<Guid> AprobadasSinAnalisis, int Convalidar, int NoConvalidar, int SinAcuerdo, IReadOnlyList<string> Advertencias);
public sealed class InformeYaGeneradoException() : Exception("El informe de esta solicitud ya fue generado; los análisis docentes están cerrados.");
public interface IInformesHomologacion
{
    Task<InformeHomologacion?> ObtenerAsync(Guid solicitudId, CancellationToken ct);
    /// <summary>Solo inserta: si ya existe un informe lanza <see cref="InformeYaGeneradoException"/>.</summary>
    Task GuardarAsync(InformeHomologacion informe, CancellationToken ct);
}

/// <summary>HU-23: cualquiera de los dos docentes asignados genera el informe cuando cada pareja analizada tiene el análisis de ambos.</summary>
public sealed class GenerarInformeHomologacion(ISolicitudesAnalisisInterno solicitudes, IReceptorasAnalisisInterno receptoras,
    IResultadosAnalisisInterno internos, IResultadosAnalisisExterno externos, IRevisionesDocentes revisiones, IInformesHomologacion informes, TimeProvider reloj)
{
    public const int DocentesAsignados = 2;

    public async Task<InformeHomologacion> GenerarAsync(Guid solicitudId, Guid docenteId, string docente, CancellationToken ct)
    {
        var solicitud = await Asignada(solicitudId, docenteId, ct);
        if (await informes.ObtenerAsync(solicitudId, ct) is not null) throw new InformeYaGeneradoException();
        var catalogo = (await receptoras.ListarAsync(solicitud.CarreraReceptoraId, ct)).ToDictionary(r => r.Id);
        string Etiqueta(Guid id) => catalogo.TryGetValue(id, out var r) ? r.Codigo : id.ToString();
        var parejas = (await revisiones.ListarAsync(solicitudId, ct)).Where(r => r.SolicitudId == solicitudId)
            .GroupBy(r => (r.AsignaturaAprobadaId, r.AsignaturaReceptoraId)).ToArray();
        if (parejas.Length == 0) throw new DatosAnalisisSolicitudException("No hay parejas con análisis docente: registre los análisis antes de generar el informe.");
        var pendientes = parejas.Where(p => p.Select(r => r.DocenteId).Distinct().Count() < DocentesAsignados)
            .Select(p => Etiqueta(p.Key.AsignaturaReceptoraId)).ToArray();
        if (pendientes.Length > 0)
            throw new DatosAnalisisSolicitudException("Falta el análisis de un docente en las parejas con receptora: " + string.Join(", ", pendientes) + ".");
        var agente = solicitud.TipoCambio == "Externo" ? await externos.ListarAsync(solicitudId, ct) : await internos.ListarAsync(solicitudId, ct);
        var lista = parejas.Select(p =>
        {
            var a = agente.Where(g => g.Analisis.SolicitudId == solicitudId && g.Analisis.AsignaturaAprobadaId == p.Key.AsignaturaAprobadaId
                && g.Analisis.AsignaturaReceptoraId == p.Key.AsignaturaReceptoraId).OrderByDescending(g => g.Fecha).FirstOrDefault()?.Analisis;
            var docentes = p.OrderBy(r => r.Docente, StringComparer.Ordinal)
                .Select(r => new DocenteEnInforme(r.DocenteId, r.Docente, r.Postura, r.Decision, r.Observacion, r.Fecha)).ToArray();
            var decisiones = docentes.Select(d => d.Decision).Distinct().ToArray();
            var final = decisiones.Length > 1 ? "SinAcuerdo" : decisiones[0] == "Homologable" ? "Convalidar" : "NoConvalidar";
            catalogo.TryGetValue(p.Key.AsignaturaReceptoraId, out var receptora);
            return new ParejaEnInforme(p.Key.AsignaturaAprobadaId, p.Key.AsignaturaReceptoraId, receptora?.Codigo, receptora?.Nombre,
                new(a?.Estado ?? "SinAnalisis", a?.Estado == "Completo" ? a.Resultado?.Sugerencia : null,
                    a?.Evaluacion.CompatibilidadAcademicaPct, a?.Evaluacion.CompatibilidadHorariaPct, a?.Evaluacion.CompatibilidadCreditosPct),
                docentes, final);
        }).OrderBy(p => p.ReceptoraCodigo ?? p.AsignaturaReceptoraId.ToString(), StringComparer.Ordinal).ThenBy(p => p.AsignaturaAprobadaId).ToArray();
        var analizadas = lista.Select(p => p.AsignaturaAprobadaId).ToHashSet();
        var sinAnalisis = solicitud.AsignaturasAprobadas.Select(a => a.Id).Where(id => !analizadas.Contains(id)).ToArray();
        var sinAcuerdo = lista.Count(p => p.SugerenciaFinal == "SinAcuerdo");
        var advertencias = new List<string> { "Informe técnico para el coordinador de carrera; no es una resolución oficial." };
        if (sinAcuerdo > 0) advertencias.Add("Hay parejas sin acuerdo entre los docentes: decide el coordinador de carrera.");
        if (sinAnalisis.Length > 0) advertencias.Add("Hay asignaturas aprobadas sin análisis docente; no tienen sugerencia.");
        var nombre = string.IsNullOrWhiteSpace(docente) ? docenteId.ToString() : docente.Trim();
        var informe = new InformeHomologacion(solicitudId, solicitud.TipoCambio, reloj.GetUtcNow(), docenteId, nombre.Length > 256 ? nombre[..256] : nombre,
            lista, sinAnalisis, lista.Count(p => p.SugerenciaFinal == "Convalidar"), lista.Count(p => p.SugerenciaFinal == "NoConvalidar"), sinAcuerdo, advertencias);
        await informes.GuardarAsync(informe, ct);
        return informe;
    }

    public async Task<InformeHomologacion?> ConsultarAsync(Guid solicitudId, CancellationToken ct)
    {
        var solicitud = await solicitudes.ObtenerSolicitudAsync(solicitudId, ct) ?? throw new SolicitudAnalisisNoEncontradaException();
        if (solicitud.Id != solicitudId) throw new FuenteAnalisisNoDisponibleException();
        return await informes.ObtenerAsync(solicitudId, ct);
    }

    private async Task<SolicitudParaAnalisis> Asignada(Guid solicitudId, Guid docenteId, CancellationToken ct)
    {
        if (docenteId == Guid.Empty) throw new SolicitudAnalisisNoEncontradaException();
        // Solicitudes valida la asignación con la identidad del token en cada petición.
        var solicitud = await solicitudes.ObtenerSolicitudAsync(solicitudId, ct) ?? throw new SolicitudAnalisisNoEncontradaException();
        if (solicitud.Id != solicitudId || solicitud.AsignaturasAprobadas is null) throw new FuenteAnalisisNoDisponibleException();
        return solicitud;
    }
}

using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Application.RevisionDocente;
using Homologacion.Domain.TerceraMatricula;
using Homologacion.Domain.Auditoria;
using Homologacion.Application.Configuracion;
using Homologacion.Domain.Configuracion;

namespace Homologacion.Application.Informe;

public sealed record AgenteEnInforme(string Estado, string? Sugerencia, decimal? PorcentajeAcademico, decimal? PorcentajeHoras, decimal? PorcentajeCreditos);
public sealed record DocenteEnInforme(Guid DocenteId, string Docente, string Postura, string Decision, string? Observacion, DateTimeOffset Fecha);
/// <summary>SugerenciaFinal: Convalidar, NoConvalidar o SinAcuerdo (decide el coordinador).
/// Exclusion: motivo de una regla que impide convalidar (HU-17 / RN-07); null si no aplica. Los informes anteriores no lo incluyen.</summary>
public sealed record ParejaEnInforme(Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, string? ReceptoraCodigo, string? ReceptoraNombre,
    AgenteEnInforme Agente, IReadOnlyList<DocenteEnInforme> Docentes, string SugerenciaFinal, string? Exclusion = null)
{
    public MallasAnalisis? Mallas { get; init; }
}
public sealed record GrupoEnInforme(Guid GrupoId, IReadOnlyList<Guid> AsignaturasAprobadas,
    IReadOnlyList<EvaluacionEquivalencia> Receptoras, string EstadoAnalisis, string? Sugerencia,
    IReadOnlyList<DocenteEnInforme> Docentes, string SugerenciaFinal,
    IReadOnlyList<AporteEvaluadoAgrupacion> Aportes, IReadOnlyList<EvidenciaAgrupacion> Evidencias, string Leyenda)
{
    public Guid? ConfiguracionVersionId { get; init; }
    public MallasAnalisis? Mallas { get; init; }
}
public sealed record AnalisisAgrupacionGuardado(Guid SolicitudId, Guid GrupoId, IReadOnlyList<Guid> AsignaturasAprobadas,
    ResultadoAgrupacion Evaluacion, DateTimeOffset Fecha)
{
    /// <summary>HU-07: versiones capturadas al analizar; null en históricos. El informe debe consumir esta fotografía.</summary>
    public MallasAnalisis? Mallas { get; init; }
}
public sealed record RevisionAgrupacionRegistrada(Guid SolicitudId, Guid GrupoId, Guid DocenteId,
    string Docente, string Postura, string Decision, string? Observacion, DateTimeOffset Fecha);
public interface IResultadosAgrupacionInforme
{
    Task<IReadOnlyList<AnalisisAgrupacionGuardado>> ListarAsync(Guid solicitudId, CancellationToken ct);
}
public interface IRevisionesAgrupacionInforme
{
    Task<IReadOnlyList<RevisionAgrupacionRegistrada>> ListarAsync(Guid solicitudId, CancellationToken ct);
}
/// <summary>HU-23: versión fija del informe; no es una resolución oficial.</summary>
public sealed record InformeHomologacion(Guid SolicitudId, string TipoCambio, DateTimeOffset Fecha, Guid GeneradoPorId, string GeneradoPor,
    IReadOnlyList<ParejaEnInforme> Parejas, IReadOnlyList<Guid> AprobadasSinAnalisis, int Convalidar, int NoConvalidar, int SinAcuerdo, IReadOnlyList<string> Advertencias)
{
    public IReadOnlyList<GrupoEnInforme> Grupos { get; init; } = [];
}
public sealed class InformeYaGeneradoException() : Exception("El informe de esta solicitud ya fue generado; los análisis docentes están cerrados.");
public interface IInformesHomologacion
{
    Task<InformeHomologacion?> ObtenerAsync(Guid solicitudId, CancellationToken ct);
    /// <summary>Solo inserta: si ya existe un informe lanza <see cref="InformeYaGeneradoException"/>.</summary>
    Task GuardarAsync(InformeHomologacion informe, CancellationToken ct);
    /// <summary>Ejecuta la acción con el cierre de la solicitud bloqueado: generar el informe y guardar un análisis docente
    /// no se intercalan, de modo que el informe incluye toda corrección confirmada y ninguna se confirma después de él.</summary>
    Task<T> ConCierreAsync<T>(Guid solicitudId, Func<Task<T>> accion, CancellationToken ct) => accion();
}

/// <summary>HU-23: cualquiera de los dos docentes asignados genera el informe cuando cada pareja analizada tiene el análisis de ambos.</summary>
public sealed class GenerarInformeHomologacion(ISolicitudesAnalisisInterno solicitudes, IReceptorasAnalisisInterno receptoras,
    IResultadosAnalisisInterno internos, IResultadosAnalisisExterno externos, IRevisionesDocentes revisiones, IInformesHomologacion informes,
    TimeProvider reloj, IResultadosAgrupacionInforme? analisisGrupales = null, IRevisionesAgrupacionInforme? revisionesGrupales = null,
    IAuditoriaInformes? auditoria = null, IParametrosHomologacionProvider? parametrosProvider = null,
    ISolicitudesAnalisisExterno? solicitudesExternas = null)
{
    public const int DocentesAsignados = 2;

    public async Task<InformeHomologacion> GenerarAsync(Guid solicitudId, Guid docenteId, string docente, CancellationToken ct)
    {
        var solicitud = await Asignada(solicitudId, docenteId, ct);
        if (await informes.ObtenerAsync(solicitudId, ct) is not null) throw new InformeYaGeneradoException();
        var catalogo = (await receptoras.ListarAsync(solicitud.CarreraReceptoraId, ct)).ToDictionary(r => r.Id);
        // Las fuentes HTTP se consultan fuera del cierre; la lectura de análisis y el guardado van dentro.
        return await informes.ConCierreAsync(solicitudId, () => Generar(solicitud, catalogo, docenteId, docente, ct), ct);
    }

    private async Task<InformeHomologacion> Generar(SolicitudParaAnalisis solicitud, Dictionary<Guid, ReceptoraAnalisis> catalogo,
        Guid docenteId, string docente, CancellationToken ct)
    {
        var solicitudId = solicitud.Id;
        if (await informes.ObtenerAsync(solicitudId, ct) is not null) throw new InformeYaGeneradoException();
        string Etiqueta(Guid id) => catalogo.TryGetValue(id, out var r) ? r.Codigo : id.ToString();
        var parejas = (await revisiones.ListarAsync(solicitudId, ct)).Where(r => r.SolicitudId == solicitudId)
            .GroupBy(r => (r.AsignaturaAprobadaId, r.AsignaturaReceptoraId)).ToArray();
        if ((analisisGrupales is null) != (revisionesGrupales is null))
            throw new FuenteAnalisisNoDisponibleException();
        var analisisGrupo = analisisGrupales is null ? [] : (await analisisGrupales.ListarAsync(solicitudId, ct))
            .Where(a => a.SolicitudId == solicitudId).ToArray();
        var revisionesGrupo = revisionesGrupales is null ? [] : (await revisionesGrupales.ListarAsync(solicitudId, ct))
            .Where(r => r.SolicitudId == solicitudId).GroupBy(r => r.GrupoId).ToArray();
        var gruposSinRevision = analisisGrupo.Where(a => !revisionesGrupo.Any(g => g.Key == a.GrupoId))
            .Select(a => a.GrupoId).ToArray();
        if (gruposSinRevision.Length > 0)
            throw new DatosAnalisisSolicitudException("Falta el análisis de los docentes en las agrupaciones: " + string.Join(", ", gruposSinRevision) + ".");
        if (parejas.Length == 0 && revisionesGrupo.Length == 0)
            throw new DatosAnalisisSolicitudException("No hay parejas ni agrupaciones con análisis docente: registre los análisis antes de generar el informe.");
        var pendientes = parejas.Where(p => p.Select(r => r.DocenteId).Distinct().Count() < DocentesAsignados)
            .Select(p => Etiqueta(p.Key.AsignaturaReceptoraId)).ToArray();
        if (pendientes.Length > 0)
            throw new DatosAnalisisSolicitudException("Falta el análisis de un docente en las parejas con receptora: " + string.Join(", ", pendientes) + ".");
        var pendientesGrupo = revisionesGrupo.Where(g => g.Select(r => r.DocenteId).Distinct().Count() < DocentesAsignados)
            .Select(g => g.Key.ToString()).ToArray();
        if (pendientesGrupo.Length > 0)
            throw new DatosAnalisisSolicitudException("Falta el análisis de un docente en las agrupaciones: " + string.Join(", ", pendientesGrupo) + ".");
        if (analisisGrupo.GroupBy(a => a.GrupoId).Any(g => g.Count() != 1)
            || revisionesGrupo.Any(g => !analisisGrupo.Any(a => a.GrupoId == g.Key)))
            throw new FuenteAnalisisNoDisponibleException();
        var agente = solicitud.TipoCambio == "Externo" ? await externos.ListarAsync(solicitudId, ct) : await internos.ListarAsync(solicitudId, ct);
        var excluidas = solicitud.AsignaturasAprobadas.Where(a => ReglaTerceraMatricula.Excluye(a.IntentoMatricula)).Select(a => a.Id).ToHashSet();
        var lista = parejas.Select(p =>
        {
            var a = agente.Where(g => g.Analisis.SolicitudId == solicitudId && g.Analisis.AsignaturaAprobadaId == p.Key.AsignaturaAprobadaId
                && g.Analisis.AsignaturaReceptoraId == p.Key.AsignaturaReceptoraId).OrderByDescending(g => g.Fecha).FirstOrDefault()?.Analisis;
            var docentes = p.OrderBy(r => r.Docente, StringComparer.Ordinal)
                .Select(r => new DocenteEnInforme(r.DocenteId, r.Docente, r.Postura, r.Decision, r.Observacion, r.Fecha)).ToArray();
            var decisiones = docentes.Select(d => d.Decision).Distinct().ToArray();
            var excluida = excluidas.Contains(p.Key.AsignaturaAprobadaId);
            var final = excluida ? "NoConvalidar" : decisiones.Length > 1 ? "SinAcuerdo" : decisiones[0] == "Homologable" ? "Convalidar" : "NoConvalidar";
            catalogo.TryGetValue(p.Key.AsignaturaReceptoraId, out var receptora);
            return new ParejaEnInforme(p.Key.AsignaturaAprobadaId, p.Key.AsignaturaReceptoraId, receptora?.Codigo, receptora?.Nombre,
                new(a?.Estado ?? "SinAnalisis", a?.Estado == "Completo" ? a.Resultado?.Sugerencia : null,
                    a?.Evaluacion.CompatibilidadAcademicaPct, a?.Evaluacion.CompatibilidadHorariaPct, a?.Evaluacion.CompatibilidadCreditosPct),
                docentes, final, excluida ? ReglaTerceraMatricula.Motivo : null)
            { Mallas = a?.Mallas };
        }).OrderBy(p => p.ReceptoraCodigo ?? p.AsignaturaReceptoraId.ToString(), StringComparer.Ordinal).ThenBy(p => p.AsignaturaAprobadaId).ToArray();
        var grupos = revisionesGrupo.Select(g =>
        {
            var a = analisisGrupo.Single(x => x.GrupoId == g.Key);
            var docentes = g.OrderBy(r => r.Docente, StringComparer.Ordinal)
                .Select(r => new DocenteEnInforme(r.DocenteId, r.Docente, r.Postura, r.Decision, r.Observacion, r.Fecha)).ToArray();
            var decisiones = docentes.Select(d => d.Decision).Distinct().ToArray();
            var final = decisiones.Length > 1 ? "SinAcuerdo" : decisiones[0] == "Homologable" ? "Convalidar" : "NoConvalidar";
            return new GrupoEnInforme(g.Key, a.AsignaturasAprobadas, a.Evaluacion.Receptoras,
                a.Evaluacion.Estado.ToString(), a.Evaluacion.Estado == Homologacion.Domain.Equivalencias.EstadoEquivalencia.InformacionIncompleta
                    ? null : a.Evaluacion.Estado.ToString(), docentes, final, a.Evaluacion.Aportes, a.Evaluacion.Evidencias,
                "Homologación por agrupación — regla de análisis pendiente de validación institucional")
            { ConfiguracionVersionId = a.Evaluacion.ConfiguracionVersionId, Mallas = a.Mallas };
        }).OrderBy(g => g.GrupoId).ToArray();
        var analizadas = lista.Select(p => p.AsignaturaAprobadaId).ToHashSet();
        foreach (var id in grupos.SelectMany(g => g.AsignaturasAprobadas)) analizadas.Add(id);
        var sinAnalisis = solicitud.AsignaturasAprobadas.Select(a => a.Id).Where(id => !analizadas.Contains(id)).ToArray();
        var sinAcuerdo = lista.Count(p => p.SugerenciaFinal == "SinAcuerdo") + grupos.Count(g => g.SugerenciaFinal == "SinAcuerdo");
        var advertencias = new List<string> { "Informe técnico para el coordinador de carrera; no es una resolución oficial." };
        if (sinAcuerdo > 0) advertencias.Add("Hay análisis sin acuerdo entre los docentes: decide el coordinador de carrera.");
        if (lista.Any(p => p.Exclusion is not null))
            advertencias.Add("Hay asignaturas aprobadas en tercera matrícula: RN-07 las excluye y su sugerencia final es «No convalidar»; las demás se evalúan normalmente.");
        if (sinAnalisis.Length > 0) advertencias.Add("Hay asignaturas aprobadas sin análisis docente; no tienen sugerencia.");
        // HU-07 / HU-22: exclusivamente la vigencia capturada al analizar, nunca Carrera.Vigencia ni una consulta actual.
        var mallasOrigen = lista.SelectMany(p => p.Mallas?.Origenes ?? [])
            .Concat(grupos.SelectMany(g => g.Mallas?.Origenes ?? []))
            .Select(o => o.Malla).OfType<InstantaneaMalla>().Where(m => !m.VigenteAlAnalizar).DistinctBy(m => m.Id);
        foreach (var malla in mallasOrigen)
            advertencias.Add($"Malla de origen no vigente al analizar: {malla.Nombre} ({malla.Id}). La carrera debe confirmar la ruta de cambio de plan de estudios y su tabla de homologación para este caso. Alerta informativa de HU-22; no bloquea ni modifica el veredicto. Regla de análisis del proyecto pendiente de confirmación institucional.");
        ParametrosHomologacion? parametros;
        try { parametros = parametrosProvider is null ? ParametrosHomologacion.Predeterminados() : await parametrosProvider.ObtenerVigentesAsync(ct); }
        catch (AlmacenAnalisisNoDisponibleException)
        {
            parametros = null;
            advertencias.Add("Información incompleta: no se pudieron consultar los parámetros vigentes de HU-03; no se evalúan los mínimos de HU-16 ni se sugiere una ruta alternativa.");
        }
        if (parametros is not null)
        {
            var minimoHomologables = solicitud.TipoCambio == "Externo"
                ? parametros.AsignaturasHomologablesMinimasExterno : parametros.AsignaturasHomologablesMinimasInterno;
            var homologablesConfirmadas = lista.Where(p => p.Agente.Estado == "Completo" && p.Agente.Sugerencia == "Homologable")
                .Select(p => p.AsignaturaAprobadaId).Distinct().Count();
            var analisisCompleto = sinAnalisis.Length == 0 && lista.All(p => p.Agente.Estado == "Completo");
            if (homologablesConfirmadas < minimoHomologables)
            {
                if (analisisCompleto)
                    advertencias.Add(AlertarRutaAlternativa($"asignaturas homologables: mínimo {minimoHomologables}, alcanzado {homologablesConfirmadas}"));
                else
                    advertencias.Add($"No se puede determinar el mínimo de asignaturas homologables ({minimoHomologables} requeridas): faltan análisis completos de todas las asignaturas aprobadas.");
            }
        }
        if (solicitud.TipoCambio == "Externo")
        {
            SolicitudExternaParaAnalisis? externa = null;
            try { if (solicitudesExternas is not null) externa = await solicitudesExternas.ObtenerSolicitudExternaAsync(solicitudId, ct); }
            catch (FuenteAnalisisNoDisponibleException) { }
            if (externa is null || externa.Id != solicitudId || externa.TipoCambio != "Externo")
                advertencias.Add("Información incompleta: no se pudieron consultar los períodos cursados ni los cambios previos de la solicitud externa.");
            else
            {
                if (parametros is null)
                    advertencias.Add("No se evalúan los períodos mínimos externos porque los parámetros vigentes no están disponibles.");
                else if (externa.PeriodosCursados is null)
                    advertencias.Add($"Información incompleta: no se declararon los períodos cursados; no se puede determinar si se alcanza el mínimo externo de {parametros.PeriodosMinimosExterno}.");
                else if (externa.PeriodosCursados < parametros.PeriodosMinimosExterno)
                    advertencias.Add(AlertarRutaAlternativa($"períodos cursados: mínimo {parametros.PeriodosMinimosExterno}, alcanzado {externa.PeriodosCursados}"));
                if (externa.CambiosPreviosCarrera is null)
                    advertencias.Add("Información incompleta: no se declararon los cambios previos de carrera; no se puede evaluar este requisito externo.");
                else if (externa.CambiosPreviosCarrera > 0)
                    advertencias.Add(AlertarRutaAlternativa($"cambios previos de carrera: se requieren 0, declarados {externa.CambiosPreviosCarrera}"));
            }
        }
        else
        {
            advertencias.Add("No se registran períodos cursados para solicitudes internas; este dato de HU-16 no puede verificarse en el expediente actual.");
        }
        var nombre = string.IsNullOrWhiteSpace(docente) ? docenteId.ToString() : docente.Trim();
        var informe = new InformeHomologacion(solicitudId, solicitud.TipoCambio, reloj.GetUtcNow(), docenteId, nombre.Length > 256 ? nombre[..256] : nombre,
            lista, sinAnalisis, lista.Count(p => p.SugerenciaFinal == "Convalidar") + grupos.Count(g => g.SugerenciaFinal == "Convalidar"),
            lista.Count(p => p.SugerenciaFinal == "NoConvalidar") + grupos.Count(g => g.SugerenciaFinal == "NoConvalidar"), sinAcuerdo, advertencias)
        { Grupos = grupos };
        await informes.GuardarAsync(informe, ct);
        // HU-25: dentro del mismo cierre y transacción que el informe; si el evento falla, el informe no se confirma.
        if (auditoria is not null)
        {
            var evento = HistorialInforme.Crear(solicitudId, TipoEventoInforme.Generado, informe.Fecha, docenteId, informe.GeneradoPor, "Docente");
            HistorialInforme.ValidarSiguiente((await auditoria.ListarAsync(solicitudId, ct)).ToArray(), evento.Tipo);
            await auditoria.RegistrarAsync(evento, ct);
        }
        return informe;
    }

    private static string AlertarRutaAlternativa(string requisito)
        => $"Alerta informativa de ruta alternativa: no se alcanza el requisito ({requisito}). Revise el proceso vigente de admisión y cambio de carrera de la UTA (RRA del CES, art. 79; Instructivo UTA RES CAU-P-005-2023); la ruta aplicable y la decisión corresponden a la institución. Regla de análisis del proyecto pendiente de confirmación institucional; no bloquea ni modifica el veredicto de homologación.";

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

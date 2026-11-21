using Homologacion.Application.Antiguedad;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Antiguedad;

namespace Homologacion.Application.AnalisisInterno;

public sealed class AnalizarSolicitudInterna(ISolicitudesAnalisisInterno solicitudes, ICatalogoAnalisisInterno catalogo,
    AsistirEvaluacionInterna asistencia, TimeProvider reloj)
{
    public async Task<ResultadoInternoSolicitud> EjecutarAsync(Guid solicitudId, Guid aprobadaId, Guid receptoraId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (solicitudId == Guid.Empty || aprobadaId == Guid.Empty || receptoraId == Guid.Empty)
            throw new DatosAnalisisSolicitudException("Seleccione la solicitud y ambas asignaturas.");
        var solicitud = await solicitudes.ObtenerSolicitudAsync(solicitudId, ct) ?? throw new SolicitudAnalisisNoEncontradaException();
        if (solicitud.Id != solicitudId || solicitud.AsignaturasAprobadas is null
            || solicitud.AsignaturasAprobadas.Any(a => a is null)) throw new FuenteAnalisisNoDisponibleException();
        if (solicitud.TipoCambio != "Interno") throw new DatosAnalisisSolicitudException("Esta operación solo admite solicitudes internas.");
        if (solicitud.CarreraOrigenId is not { } carreraOrigen || carreraOrigen == Guid.Empty
            || solicitud.CarreraReceptoraId == Guid.Empty || carreraOrigen == solicitud.CarreraReceptoraId)
            throw new FuenteAnalisisNoDisponibleException();
        var candidatas = solicitud.AsignaturasAprobadas.Where(a => a.Id == aprobadaId).ToArray();
        if (candidatas.Length == 0) throw new DatosAnalisisSolicitudException("La asignatura aprobada no pertenece a esta solicitud.");
        if (candidatas.Length != 1) throw new FuenteAnalisisNoDisponibleException();
        var aprobada = candidatas[0];
        if (aprobada.AsignaturaOrigenId is not { } origenId || origenId == Guid.Empty || aprobada.Horas <= 0 || aprobada.Creditos <= 0)
            throw new FuenteAnalisisNoDisponibleException();
        var origen = await catalogo.ObtenerAsignaturaAsync(origenId, ct) ?? throw new FuenteAnalisisNoDisponibleException();
        var receptora = await catalogo.ObtenerAsignaturaAsync(receptoraId, ct)
            ?? throw new DatosAnalisisSolicitudException("No se encontró la asignatura receptora.");
        ValidarContexto(origen, origenId); ValidarContexto(receptora, receptoraId);
        if (origen.CarreraId != carreraOrigen || receptora.CarreraId != solicitud.CarreraReceptoraId)
            throw new DatosAnalisisSolicitudException("Las asignaturas deben pertenecer a las carreras de la solicitud.");
        var faltantes = new List<string>();
        if (origen.Facultad != "FISEI" || receptora.Facultad != "FISEI")
            faltantes.Add("catálogo histórico de otra facultad: requiere revisión, sin reasignar registros");
        if (!origen.CarreraVigente || !receptora.CarreraVigente)
            faltantes.Add("vigencia de las carreras: requiere revisión del registro histórico");
        if (aprobada.Horas != origen.Horas || aprobada.Creditos != origen.Creditos)
            faltantes.Add("registro de aprobación y catálogo actual difieren: revisar la versión del programa");
        if (!Enum.TryParse<EstadoAsignatura>(aprobada.Estado, out var estado) || !Enum.IsDefined(estado)
            || aprobada.Estado != estado.ToString())
            throw new FuenteAnalisisNoDisponibleException();
        var hoy = DateOnly.FromDateTime(reloj.GetUtcNow().ToOffset(TimeSpan.FromHours(-5)).DateTime);
        ResultadoAntiguedad? antiguedad = null;
        if (aprobada.FechaAprobacion == default || aprobada.FechaAprobacion > hoy)
            faltantes.Add("fecha de aprobación ausente o futura: requiere revisión");
        else
        {
            antiguedad = new ValidarAntiguedad().Ejecutar(new(estado, aprobada.FechaAprobacion, hoy));
            if (antiguedad.Estado != EstadoAntiguedad.Elegible) faltantes.Add(antiguedad.Motivo);
        }
        var revision = await catalogo.ObtenerRevisionAsync(origenId, receptoraId, ct);
        if (!revision.Registrada || !revision.Vigente) faltantes.Add("faltan correspondencias explícitas revisadas y vigentes de ambos programas");
        else if (revision.Contenidos is null || revision.Resultados is null
            || revision.Contenidos.Concat(revision.Resultados).Any(v => v is null)) throw new FuenteAnalisisNoDisponibleException();
        var datos = new SolicitudEquivalencia([new(Datos(origen) with { Horas = aprobada.Horas, Creditos = aprobada.Creditos }, estado == EstadoAsignatura.Aprobada)], Datos(receptora),
            revision.Registrada && revision.Vigente ? Vinculos(origenId, revision.Contenidos) : null,
            revision.Registrada && revision.Vigente ? Vinculos(origenId, revision.Resultados) : null);
        var textos = Textos(origen).Concat(Textos(receptora)).ToArray();
        AnalisisAsistido resultado;
        try { resultado = await asistencia.EjecutarAsync(datos, textos, faltantes, ct); }
        catch (DatosEvaluacionInvalidosException) { throw new FuenteAnalisisNoDisponibleException(); }
        var estadoAnalisis = resultado.Estado switch
        {
            EstadoAsistencia.Completada => "Completo",
            EstadoAsistencia.InformacionIncompleta => "Incompleto",
            _ => "Fallido"
        };
        var mensaje = estadoAnalisis switch
        {
            "Completo" => "Evaluación de la pareja completada con asistencia; requiere revisión académica humana.",
            "Incompleto" => resultado.Evaluacion.Justificacion,
            _ => "No fue posible completar la asistencia. Se requiere revisión manual; no se presenta un resultado exitoso."
        };
        var resumen = estadoAnalisis == "Completo" ? Resumen(resultado) : null;
        return new(solicitudId, aprobadaId, receptoraId, estadoAnalisis, mensaje, resumen, resultado.Evaluacion, antiguedad,
            origen.EsSintetico || receptora.EsSintetico,
            ["Los mínimos globales del cambio interno no están evaluados: faltan períodos cursados y el cómputo completo de la solicitud. No es una aprobación oficial.",
                "Se consultó el catálogo actual; su versión histórica debe confirmarse durante la revisión académica."]);
    }

    private static DatosAsignatura Datos(AsignaturaParaAnalisis a) => new(a.Id.ToString(), a.PlanAnalitico, a.Horas,
        a.UnidadHoras, a.Creditos, a.SistemaCreditos, a.Contenidos.Select(e => e.Id.ToString()).ToArray(), a.Resultados.Select(e => e.Id.ToString()).ToArray());
    private static IReadOnlyList<CorrespondenciaAcademica?> Vinculos(Guid origen, IReadOnlyList<VinculoParaAnalisis> vinculos) =>
        vinculos.Select(v => (CorrespondenciaAcademica?)new CorrespondenciaAcademica(origen.ToString(), v.ElementoOrigen, v.ElementoReceptor, new(v.UbicacionOrigen, v.UbicacionReceptora))).ToArray();
    private static IEnumerable<TextoElementoAnalisis> Textos(AsignaturaParaAnalisis a) =>
        a.Contenidos.Select(e => new TextoElementoAnalisis(a.Id.ToString(), "Contenido", e.Id.ToString(), e.Texto))
            .Concat(a.Resultados.Select(e => new TextoElementoAnalisis(a.Id.ToString(), "ResultadoAprendizaje", e.Id.ToString(), e.Texto)));
    private static void ValidarContexto(AsignaturaParaAnalisis a, Guid esperado)
    {
        if (a.Id != esperado || a.CarreraId == Guid.Empty || a.Horas <= 0 || a.Creditos <= 0 || a.Contenidos is null || a.Resultados is null
            || a.Contenidos.Concat(a.Resultados).Any(e => e is null || e.Id == Guid.Empty || string.IsNullOrWhiteSpace(e.Texto))
            || a.Contenidos.Select(e => e.Id).Distinct().Count() != a.Contenidos.Count || a.Resultados.Select(e => e.Id).Distinct().Count() != a.Resultados.Count)
            throw new FuenteAnalisisNoDisponibleException();
    }
    private static ResumenAnalisisInterno Resumen(AnalisisAsistido resultado)
    {
        var r = resultado.Evaluacion;
        var referencias = resultado.Referencias!.ToDictionary(e => e.Id, StringComparer.Ordinal);
        var citas = resultado.Asistencia!.EvidenciasCitadas.Select(id =>
        {
            var referencia = referencias[id]; var c = referencia.Correspondencia;
            return $"{id}: {c.AsignaturaOrigen}/{c.ElementoOrigen} [{c.Evidencia!.UbicacionOrigen}] → {c.ElementoReceptor} [{c.Evidencia.UbicacionReceptora}]";
        }).ToArray();
        return new(r.CompatibilidadAcademicaPct, r.CompatibilidadHorariaPct, r.CompatibilidadCreditosPct,
            r.Homologable ? "Homologable" : "No homologable", r.Justificacion + " Asistencia: " + resultado.Asistencia.Resumen, citas);
    }
}

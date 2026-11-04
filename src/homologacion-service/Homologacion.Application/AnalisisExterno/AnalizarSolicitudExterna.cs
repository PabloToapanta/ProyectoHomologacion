using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Antiguedad;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Antiguedad;
using Homologacion.Domain.Equivalencias;

namespace Homologacion.Application.AnalisisExterno;

/// <summary>
/// HU-19: evalúa una asignatura de otra universidad ecuatoriana contra una receptora FISEI con el motor determinista.
/// D2: sin contenidos externos registrados el criterio académico es información incompleta y no se invoca al agente.
/// D3: horas y créditos solo se comparan con unidad y sistema idénticos (sin espacios ni distinción de mayúsculas).
/// </summary>
public sealed class AnalizarSolicitudExterna(ISolicitudesAnalisisExterno solicitudes, ICatalogoAnalisisInterno catalogo,
    EvaluarEquivalencia motor, TimeProvider reloj)
{
    public const string FaltaProgramaExterno =
        "contenidos y resultados de aprendizaje del programa externo: el sistema aún no los registra con su ubicación revisada";

    public async Task<ResultadoInternoSolicitud> EjecutarAsync(Guid solicitudId, Guid aprobadaId, Guid receptoraId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (solicitudId == Guid.Empty || aprobadaId == Guid.Empty || receptoraId == Guid.Empty)
            throw new DatosAnalisisSolicitudException("Seleccione la solicitud y ambas asignaturas.");
        var solicitud = await ObtenerExternaAsync(solicitudes, solicitudId, ct);
        var candidatas = solicitud.AsignaturasAprobadas.Where(a => a.Id == aprobadaId).ToArray();
        if (candidatas.Length == 0) throw new DatosAnalisisSolicitudException("La asignatura aprobada no pertenece a esta solicitud.");
        if (candidatas.Length != 1) throw new FuenteAnalisisNoDisponibleException();
        var aprobada = candidatas[0];
        if (aprobada.Horas <= 0 || aprobada.Creditos <= 0 || string.IsNullOrWhiteSpace(aprobada.Nombre))
            throw new FuenteAnalisisNoDisponibleException();
        if (!Enum.TryParse<EstadoAsignatura>(aprobada.Estado, out var estado) || !Enum.IsDefined(estado) || aprobada.Estado != estado.ToString())
            throw new FuenteAnalisisNoDisponibleException();

        var receptora = await catalogo.ObtenerAsignaturaAsync(receptoraId, ct)
            ?? throw new DatosAnalisisSolicitudException("No se encontró la asignatura receptora.");
        if (receptora.Id != receptoraId || receptora.CarreraId == Guid.Empty || receptora.Horas <= 0 || receptora.Creditos <= 0
            || receptora.Contenidos is null || receptora.Resultados is null || receptora.Contenidos.Concat(receptora.Resultados).Any(e => e is null))
            throw new FuenteAnalisisNoDisponibleException();
        if (receptora.CarreraId != solicitud.CarreraReceptoraId)
            throw new DatosAnalisisSolicitudException("La asignatura receptora debe pertenecer a la carrera de la solicitud.");

        var faltantes = new List<string> { FaltaProgramaExterno };
        if (receptora.Facultad != "FISEI") faltantes.Add("catálogo histórico de otra facultad: requiere revisión, sin reasignar registros");
        if (!receptora.CarreraVigente) faltantes.Add("vigencia de la carrera receptora: requiere revisión del registro histórico");
        var unidadHoras = Comparable("horas", aprobada.UnidadHoras, receptora.UnidadHoras, faltantes);
        var sistemaCreditos = Comparable("créditos", aprobada.SistemaCreditos, receptora.SistemaCreditos, faltantes);

        var hoy = DateOnly.FromDateTime(reloj.GetUtcNow().ToOffset(TimeSpan.FromHours(-5)).DateTime);
        ResultadoAntiguedad? antiguedad = null;
        if (aprobada.FechaAprobacion == default || aprobada.FechaAprobacion > hoy)
            faltantes.Add("fecha de aprobación ausente o futura: requiere revisión");
        else
        {
            antiguedad = new ValidarAntiguedad().Ejecutar(new(estado, aprobada.FechaAprobacion, hoy));
            if (antiguedad.Estado != EstadoAntiguedad.Elegible) faltantes.Add(antiguedad.Motivo);
        }

        // El origen externo no tiene inventarios (D2): el motor registra la falta y nunca calcula el criterio académico.
        var origen = new DatosAsignatura(aprobada.Id.ToString(), aprobada.ReferenciaPrograma, aprobada.Horas, unidadHoras,
            aprobada.Creditos, sistemaCreditos, null, null);
        var destino = new DatosAsignatura(receptora.Id.ToString(), receptora.PlanAnalitico, receptora.Horas, receptora.UnidadHoras,
            receptora.Creditos, receptora.SistemaCreditos, receptora.Contenidos.Select(e => e.Id.ToString()).ToArray(),
            receptora.Resultados.Select(e => e.Id.ToString()).ToArray());
        EvaluacionEquivalencia evaluacion;
        try { evaluacion = motor.Ejecutar(new([new(origen, estado == EstadoAsignatura.Aprobada)], destino, null, null)); }
        catch (DatosEvaluacionInvalidosException) { throw new FuenteAnalisisNoDisponibleException(); }
        var todas = faltantes.Concat(evaluacion.InformacionFaltante).Distinct().ToArray();
        evaluacion = evaluacion with
        {
            CompatibilidadAcademicaPct = null,
            Estado = EstadoEquivalencia.InformacionIncompleta,
            InformacionFaltante = todas,
            Justificacion = "Información incompleta: " + string.Join("; ", todas) + "."
        };
        return new(solicitudId, aprobadaId, receptoraId, "Incompleto",
            "Análisis externo con información incompleta: el criterio académico requiere el programa externo registrado. Requiere revisión académica humana.",
            null, evaluacion, antiguedad, receptora.EsSintetico,
            ["No se invocó al agente: no existe evidencia académica externa registrada que pueda citar.",
             "Horas y créditos solo se comparan con unidades idénticas a las de la receptora; no se aplican conversiones.",
             "No es una aprobación oficial."]);
    }

    internal static async Task<SolicitudExternaParaAnalisis> ObtenerExternaAsync(ISolicitudesAnalisisExterno solicitudes, Guid id, CancellationToken ct)
    {
        var solicitud = await solicitudes.ObtenerSolicitudExternaAsync(id, ct) ?? throw new SolicitudAnalisisNoEncontradaException();
        if (solicitud.Id != id || solicitud.AsignaturasAprobadas is null || solicitud.AsignaturasAprobadas.Any(a => a is null))
            throw new FuenteAnalisisNoDisponibleException();
        if (solicitud.TipoCambio != "Externo") throw new DatosAnalisisSolicitudException("Esta operación solo admite solicitudes externas.");
        if (solicitud.CarreraReceptoraId == Guid.Empty) throw new FuenteAnalisisNoDisponibleException();
        return solicitud;
    }

    /// <summary>Devuelve la unidad de la receptora si coincide (D3); si no, registra el motivo y deja que el motor no compare.</summary>
    private static string? Comparable(string nombre, string? origen, string? receptora, List<string> faltantes)
    {
        if (string.IsNullOrWhiteSpace(origen) || string.IsNullOrWhiteSpace(receptora))
        {
            faltantes.Add($"unidad de {nombre} no registrada en la asignatura externa o en la receptora: sin conversión aprobada");
            return null;
        }
        if (string.Equals(origen.Trim(), receptora.Trim(), StringComparison.OrdinalIgnoreCase)) return receptora;
        faltantes.Add($"unidad de {nombre} «{origen.Trim()}» distinta de «{receptora.Trim()}» de la receptora: sin conversión aprobada");
        return null;
    }
}

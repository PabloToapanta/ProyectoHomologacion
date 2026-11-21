using Homologacion.Application.AnalisisInterno;
using Homologacion.Domain.RequisitosCambio;

namespace Homologacion.Application.AnalisisExterno;

/// <summary>HU-19: ejecuta y guarda el análisis externo por pareja, lo recupera y calcula RN-05 a partir de lo guardado.</summary>
public sealed class GestionarAnalisisExterno(AnalizarSolicitudExterna analisis, ISolicitudesAnalisisExterno solicitudes,
    IResultadosAnalisisExterno resultados, TimeProvider reloj)
{
    public async Task<ResultadoInternoSolicitud> EjecutarAsync(Guid solicitud, Guid aprobada, Guid receptora, CancellationToken ct)
    {
        var resultado = await analisis.EjecutarAsync(solicitud, aprobada, receptora, ct);
        await resultados.GuardarAsync(new(reloj.GetUtcNow(), resultado), ct);
        return resultado;
    }

    /// <summary>Carrera receptora de la solicitud externa asignada, para ofrecer solo sus asignaturas.</summary>
    public async Task<Guid> CarreraReceptoraAsync(Guid solicitud, CancellationToken ct) =>
        (await AnalizarSolicitudExterna.ObtenerExternaAsync(solicitudes, solicitud, ct)).CarreraReceptoraId;

    public async Task<IReadOnlyList<AnalisisGuardado>> ConsultarAsync(Guid solicitud, CancellationToken ct)
    {
        await AnalizarSolicitudExterna.ObtenerExternaAsync(solicitudes, solicitud, ct);
        return await resultados.ListarAsync(solicitud, ct);
    }

    /// <summary>
    /// Homologables = asignaturas aprobadas distintas con algún último análisis guardado Completo y Homologable.
    /// Los datos ausentes o los análisis pendientes se informan como faltantes; el validador de HU-16 solo recibe
    /// valores neutros en su lugar, de modo que únicamente un dato declarado bajo el mínimo produce incumplimiento.
    /// </summary>
    public async Task<ResultadoRequisitosExternos> RequisitosAsync(Guid solicitudId, CancellationToken ct)
    {
        var solicitud = await AnalizarSolicitudExterna.ObtenerExternaAsync(solicitudes, solicitudId, ct);
        var aprobadas = solicitud.AsignaturasAprobadas.Select(a => a.Id).ToHashSet();
        var propios = (await resultados.ListarAsync(solicitudId, ct))
            .Where(g => g.Analisis.SolicitudId == solicitudId && aprobadas.Contains(g.Analisis.AsignaturaAprobadaId) && g.Analisis.Estado == "Completo")
            .ToArray();
        var conCompleto = propios.Select(g => g.Analisis.AsignaturaAprobadaId).Distinct().Count();
        var homologables = propios.Where(g => g.Analisis.Resultado?.Sugerencia == "Homologable")
            .Select(g => g.Analisis.AsignaturaAprobadaId).Distinct().Count();
        var minimo = 2;
        var todasAnalizadas = aprobadas.Count > 0 && conCompleto == aprobadas.Count;

        var faltantes = new List<string>();
        if (solicitud.PeriodosCursados is null) faltantes.Add("períodos cursados no declarados por el Coordinador");
        if (solicitud.CambiosPreviosCarrera is null) faltantes.Add("cambios previos de carrera no declarados por el Coordinador");
        var homologablesConocidas = homologables >= minimo || todasAnalizadas;
        if (!homologablesConocidas)
            faltantes.Add(aprobadas.Count == 0
                ? "asignaturas aprobadas registradas en la solicitud"
                : $"análisis completos de las asignaturas aprobadas: {conCompleto} de {aprobadas.Count}");
        var validacion = ValidadorRequisitosCambio.Validar(TipoCambio.Externo, solicitud.PeriodosCursados ?? minimo,
            homologablesConocidas ? homologables : minimo, solicitud.CambiosPreviosCarrera ?? 0);
        var estado = validacion.Incumplimientos.Count > 0 ? "NoCumple" : faltantes.Count > 0 ? "Incompleto" : "Cumple";
        return new(solicitudId, estado, solicitud.PeriodosCursados, solicitud.CambiosPreviosCarrera, aprobadas.Count, conCompleto, homologables,
            validacion.Incumplimientos, faltantes,
            ["Períodos y cambios previos son los declarados por el Coordinador desde el expediente.",
             "No verifica gratuidad ni autoriza el cambio de carrera; no es una aprobación oficial."]);
    }
}

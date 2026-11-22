using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Equivalencias;
using Homologacion.Domain.Configuracion;

namespace Homologacion.Application.AnalisisInterno;

/// <summary>HU-18 con la reutilización de HU-37: el motor se ejecuta siempre; solo se reutiliza la explicación del agente.</summary>
public sealed class AsistirEvaluacionInterna(EvaluarEquivalencia motor, IAgenteAnalisisInterno agente,
    IExplicacionesAgente? explicaciones = null, TimeProvider? reloj = null)
{
    /// <summary>HU-17: evaluación determinista sin agente, para casos que una regla resuelve antes de la asistencia.</summary>
    public EvaluacionEquivalencia EvaluarSinAgente(SolicitudEquivalencia datos) => motor.Ejecutar(datos);

    public Task<AnalisisAsistido> EjecutarAsync(SolicitudEquivalencia datos, CancellationToken cancellationToken = default) =>
        EjecutarAsync(datos, [], [], cancellationToken);

    public async Task<AnalisisAsistido> EjecutarAsync(SolicitudEquivalencia datos, IReadOnlyList<TextoElementoAnalisis> textos,
        IReadOnlyList<string> faltantesExternos, CancellationToken cancellationToken)
        => await EjecutarAsync(datos, textos, faltantesExternos, null, cancellationToken);

    public async Task<AnalisisAsistido> EjecutarAsync(SolicitudEquivalencia datos, IReadOnlyList<TextoElementoAnalisis> textos,
        IReadOnlyList<string> faltantesExternos, ParametrosHomologacion? parametros, CancellationToken cancellationToken)
        => await EjecutarAsync(datos, textos, faltantesExternos, parametros, null, cancellationToken);

    public async Task<AnalisisAsistido> EjecutarAsync(SolicitudEquivalencia datos, IReadOnlyList<TextoElementoAnalisis> textos,
        IReadOnlyList<string> faltantesExternos, ParametrosHomologacion? parametros, ReutilizacionAgente? reutilizacion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var evaluacion = parametros is null ? motor.Ejecutar(datos) : motor.Ejecutar(datos, parametros);
        if (faltantesExternos.Count > 0)
        {
            var faltantes = evaluacion.InformacionFaltante.Concat(faltantesExternos).Distinct().ToArray();
            evaluacion = evaluacion with
            {
                Estado = EstadoEquivalencia.InformacionIncompleta,
                InformacionFaltante = faltantes,
                Justificacion = "Información incompleta: " + string.Join("; ", faltantes) + "."
            };
        }
        if (evaluacion.Estado == EstadoEquivalencia.InformacionIncompleta)
            return new(evaluacion, EstadoAsistencia.InformacionIncompleta, null, null);
        var referencias = (datos.CorrespondenciasContenidos ?? [])
            .Select((c, i) => new ReferenciaAnalisis("C" + (i + 1), "Contenido", c!))
            .Concat((datos.CorrespondenciasResultadosAprendizaje ?? [])
                .Select((c, i) => new ReferenciaAnalisis("R" + (i + 1), "ResultadoAprendizaje", c!))).ToArray();
        referencias = referencias.Select(r => r with
        {
            TextoOrigen = textos.FirstOrDefault(t => t.AsignaturaId == r.Correspondencia.AsignaturaOrigen && t.Categoria == r.Categoria && t.ElementoId == r.Correspondencia.ElementoOrigen)?.Texto,
            TextoReceptor = textos.FirstOrDefault(t => t.AsignaturaId == datos.Receptora!.Id && t.Categoria == r.Categoria && t.ElementoId == r.Correspondencia.ElementoReceptor)?.Texto
        }).ToArray();
        var contexto = new ContextoAgente(evaluacion, referencias);
        var reutiliza = reutilizacion is not null && explicaciones is not null && agente is IDescripcionAgente;
        var descripcion = agente as IDescripcionAgente;
        var clave = reutiliza ? HuellaReutilizacion.Clave(reutilizacion!.FirmaPareja, parametros?.VersionId ?? Guid.Empty,
            descripcion!.Modelo, descripcion.VersionPlantilla, contexto) : null;
        if (reutiliza && reutilizacion!.Forzar is null && await BuscarAsync(clave!, contexto, cancellationToken) is { } guardada)
            return new(evaluacion, EstadoAsistencia.Completada, guardada.Explicacion, null, referencias)
            {
                Agente = new(guardada.Id, true, guardada.Id, guardada.Fecha, guardada.Modelo, guardada.VersionPlantilla)
            };
        try
        {
            var asistencia = await agente.JustificarAsync(contexto, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ValidarExplicacionAgente.Validar(contexto, asistencia);
            AgenteAnalisis? origen = null;
            if (reutiliza)
            {
                var nueva = new ExplicacionGuardada(Guid.NewGuid(), clave!, (reloj ?? TimeProvider.System).GetUtcNow(), descripcion!.Modelo,
                    descripcion.VersionPlantilla, parametros?.VersionId ?? Guid.Empty, asistencia,
                    reutilizacion!.Forzar?.DocenteId, reutilizacion.Forzar?.Motivo);
                // Si falla el guardado se aplica HU-20: 503 sin confirmar el análisis.
                await explicaciones!.GuardarAsync(nueva, cancellationToken);
                origen = new(nueva.Id, false, null, null, nueva.Modelo, nueva.VersionPlantilla);
            }
            return new(evaluacion, EstadoAsistencia.Completada, asistencia, null, referencias) { Agente = origen };
        }
        catch (FalloAgenteException error)
        {
            return new(evaluacion, EstadoAsistencia.RevisionManual, null, error.Tipo, referencias);
        }
    }

    /// <summary>La reutilización es una optimización: si el almacén no responde o lo guardado no supera la validación, se llama al agente.</summary>
    private async Task<ExplicacionGuardada?> BuscarAsync(string clave, ContextoAgente contexto, CancellationToken ct)
    {
        ExplicacionGuardada? guardada;
        try { guardada = await explicaciones!.BuscarAsync(clave, ct); }
        catch (AlmacenAnalisisNoDisponibleException) { return null; }
        if (guardada is null || guardada.Clave != clave) return null;
        try { ValidarExplicacionAgente.Validar(contexto, guardada.Explicacion); }
        catch (FalloAgenteException) { return null; }
        return guardada;
    }
}

using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Equivalencias;

namespace Homologacion.Application.AnalisisInterno;

public sealed class AsistirEvaluacionInterna(EvaluarEquivalencia motor, IAgenteAnalisisInterno agente)
{
    public Task<AnalisisAsistido> EjecutarAsync(SolicitudEquivalencia datos, CancellationToken cancellationToken = default) =>
        EjecutarAsync(datos, [], [], cancellationToken);

    public async Task<AnalisisAsistido> EjecutarAsync(SolicitudEquivalencia datos, IReadOnlyList<TextoElementoAnalisis> textos,
        IReadOnlyList<string> faltantesExternos, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var evaluacion = motor.Ejecutar(datos);
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
        try
        {
            var asistencia = await agente.JustificarAsync(contexto, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ValidarExplicacionAgente.Validar(contexto, asistencia);
            return new(evaluacion, EstadoAsistencia.Completada, asistencia, null, referencias);
        }
        catch (FalloAgenteException error)
        {
            return new(evaluacion, EstadoAsistencia.RevisionManual, null, error.Tipo, referencias);
        }
    }
}

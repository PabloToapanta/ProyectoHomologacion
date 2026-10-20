using Homologacion.Domain.Antiguedad;

namespace Homologacion.Application.Antiguedad;

public sealed record SolicitudAntiguedad(EstadoAsignatura EstadoAsignatura,
    DateOnly? FechaAprobacion, DateOnly FechaEvaluacion);

public sealed class ValidarAntiguedad
{
    public ResultadoAntiguedad Ejecutar(SolicitudAntiguedad solicitud)
        => ValidadorAntiguedad.Validar(solicitud.EstadoAsignatura,
            solicitud.FechaAprobacion, solicitud.FechaEvaluacion);
}

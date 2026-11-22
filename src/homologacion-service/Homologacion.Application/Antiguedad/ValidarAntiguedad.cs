using Homologacion.Domain.Antiguedad;
using Homologacion.Domain.Configuracion;

namespace Homologacion.Application.Antiguedad;

public sealed record SolicitudAntiguedad(EstadoAsignatura EstadoAsignatura,
    DateOnly? FechaAprobacion, DateOnly FechaEvaluacion);

public sealed class ValidarAntiguedad
{
    public ResultadoAntiguedad Ejecutar(SolicitudAntiguedad solicitud)
        => ValidadorAntiguedad.Validar(solicitud.EstadoAsignatura,
            solicitud.FechaAprobacion, solicitud.FechaEvaluacion);

    public ResultadoAntiguedad Ejecutar(SolicitudAntiguedad solicitud, ParametrosHomologacion parametros)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        return ValidadorAntiguedad.Validar(solicitud.EstadoAsignatura,
            solicitud.FechaAprobacion, solicitud.FechaEvaluacion, parametros.AntiguedadMaximaAnios);
    }
}

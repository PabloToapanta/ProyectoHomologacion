namespace Homologacion.Domain.Antiguedad;

public enum EstadoAsignatura { Aprobada, Reprobada, NoCursada }
public enum EstadoAntiguedad { Elegible, NoElegible, RequiereValidacionConocimientos }

public sealed record ResultadoAntiguedad(EstadoAntiguedad Estado, string Motivo);

public static class ValidadorAntiguedad
{
    private const int AntiguedadPredeterminadaAnios = 10;

    public static ResultadoAntiguedad Validar(EstadoAsignatura estado, DateOnly? fechaAprobacion,
        DateOnly fechaEvaluacion) => Validar(estado, fechaAprobacion, fechaEvaluacion,
            AntiguedadPredeterminadaAnios);

    public static ResultadoAntiguedad Validar(EstadoAsignatura estado, DateOnly? fechaAprobacion,
        DateOnly fechaEvaluacion, int antiguedadMaximaAnios)
    {
        if (antiguedadMaximaAnios is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(antiguedadMaximaAnios), "La antigüedad máxima debe estar entre 1 y 50 años.");
        if (!Enum.IsDefined(estado))
            throw new ArgumentException("Estado de asignatura desconocido.", nameof(estado));
        if (fechaAprobacion > fechaEvaluacion)
            throw new ArgumentException("La fecha de aprobación no puede ser posterior a la evaluación.", nameof(fechaAprobacion));
        if (estado != EstadoAsignatura.Aprobada)
            return new(EstadoAntiguedad.NoElegible, "La asignatura no consta como aprobada.");
        if (fechaAprobacion is not { } aprobacion)
            throw new ArgumentException("Información incompleta: falta la fecha de aprobación.", nameof(fechaAprobacion));

        // Evita desbordar DateOnly en los últimos años del calendario.
        var dentroDelPlazo = fechaEvaluacion.Year - aprobacion.Year < antiguedadMaximaAnios
            || fechaEvaluacion <= aprobacion.AddYears(antiguedadMaximaAnios);
        return dentroDelPlazo
            ? new(EstadoAntiguedad.Elegible, $"Asignatura aprobada con antigüedad de hasta {antiguedadMaximaAnios} años inclusive.")
            : new(EstadoAntiguedad.RequiereValidacionConocimientos,
                $"La aprobación supera {antiguedadMaximaAnios} años; requiere validación de conocimientos.");
    }
}

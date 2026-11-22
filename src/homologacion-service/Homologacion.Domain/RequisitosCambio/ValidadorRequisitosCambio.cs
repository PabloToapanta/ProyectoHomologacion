namespace Homologacion.Domain.RequisitosCambio;

using Homologacion.Domain.Configuracion;

public enum TipoCambio { Interno, Externo }
public sealed record ResultadoRequisitosCambio(bool Cumple, IReadOnlyList<string> Incumplimientos);

public static class ValidadorRequisitosCambio
{
    public static ResultadoRequisitosCambio Validar(TipoCambio tipo, int periodosCursados,
        int asignaturasHomologables, int cambiosPrevios)
        => Validar(tipo, periodosCursados, asignaturasHomologables, cambiosPrevios,
            ParametrosHomologacion.Predeterminados());

    public static ResultadoRequisitosCambio Validar(TipoCambio tipo, int periodosCursados,
        int asignaturasHomologables, int cambiosPrevios, ParametrosHomologacion parametros)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        if (!Enum.IsDefined(tipo))
            throw new ArgumentException("Tipo de cambio desconocido.", nameof(tipo));
        if (periodosCursados < 0 || asignaturasHomologables < 0 || cambiosPrevios < 0)
            throw new ArgumentException("Los períodos, asignaturas homologables y cambios previos no pueden ser negativos.");

        var (periodosMinimos, asignaturasMinimas) = tipo == TipoCambio.Interno
            ? (parametros.PeriodosMinimosInterno, parametros.AsignaturasHomologablesMinimasInterno)
            : (parametros.PeriodosMinimosExterno, parametros.AsignaturasHomologablesMinimasExterno);
        var incumplimientos = new List<string>();
        if (periodosCursados < periodosMinimos)
            incumplimientos.Add($"Se requieren al menos {periodosMinimos} períodos cursados.");
        if (asignaturasHomologables < asignaturasMinimas)
            incumplimientos.Add($"Se requieren al menos {asignaturasMinimas} asignaturas homologables.");
        if (tipo == TipoCambio.Externo && cambiosPrevios > 0)
            incumplimientos.Add("El cambio externo requiere no tener cambios previos de carrera.");
        return new(incumplimientos.Count == 0, incumplimientos.AsReadOnly());
    }
}

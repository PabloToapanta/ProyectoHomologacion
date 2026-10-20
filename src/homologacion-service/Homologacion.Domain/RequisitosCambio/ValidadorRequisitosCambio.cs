namespace Homologacion.Domain.RequisitosCambio;

public enum TipoCambio { Interno, Externo }
public sealed record ResultadoRequisitosCambio(bool Cumple, IReadOnlyList<string> Incumplimientos);

public static class ValidadorRequisitosCambio
{
    public static ResultadoRequisitosCambio Validar(TipoCambio tipo, int periodosCursados,
        int asignaturasHomologables, int cambiosPrevios)
    {
        if (!Enum.IsDefined(tipo))
            throw new ArgumentException("Tipo de cambio desconocido.", nameof(tipo));
        if (periodosCursados < 0 || asignaturasHomologables < 0 || cambiosPrevios < 0)
            throw new ArgumentException("Los períodos, asignaturas homologables y cambios previos no pueden ser negativos.");

        var minimo = tipo == TipoCambio.Interno ? 1 : 2;
        var incumplimientos = new List<string>();
        if (periodosCursados < minimo)
            incumplimientos.Add($"Se requieren al menos {minimo} períodos cursados.");
        if (asignaturasHomologables < minimo)
            incumplimientos.Add($"Se requieren al menos {minimo} asignaturas homologables.");
        if (tipo == TipoCambio.Externo && cambiosPrevios > 0)
            incumplimientos.Add("El cambio externo requiere no tener cambios previos de carrera.");
        return new(incumplimientos.Count == 0, incumplimientos.AsReadOnly());
    }
}

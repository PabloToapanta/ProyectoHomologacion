namespace Homologacion.Domain.Equivalencias;

public enum EstadoEquivalencia { InformacionIncompleta, NoHomologable, Homologable }

public sealed record ResultadoEquivalencia(EstadoEquivalencia Estado)
{
    public bool Homologable => Estado == EstadoEquivalencia.Homologable;
}

/// <summary>Decisión con los tres criterios confirmados por el equipo; no es una resolución institucional.</summary>
public static class MotorEquivalencia
{
    public static ResultadoEquivalencia Evaluar(decimal? academica, decimal? horas, decimal? creditos, decimal umbral)
    {
        if (umbral is <= 0 or > 100) throw new ArgumentOutOfRangeException(nameof(umbral));
        Porcentaje?[] criterios = [Porcentaje.DesdeValor(academica), Porcentaje.DesdeValor(horas), Porcentaje.DesdeValor(creditos)];
        if (criterios.Any(p => p is null)) return new(EstadoEquivalencia.InformacionIncompleta);
        return new(criterios.All(p => p!.Value.Valor >= umbral)
            ? EstadoEquivalencia.Homologable : EstadoEquivalencia.NoHomologable);
    }
}

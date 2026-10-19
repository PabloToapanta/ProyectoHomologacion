namespace Homologacion.Domain.Equivalencias;

public enum EstadoEquivalencia { InformacionIncompleta, NoHomologable, Homologable }

public sealed record ResultadoEquivalencia(EstadoEquivalencia Estado)
{
    public bool Homologable => Estado == EstadoEquivalencia.Homologable;
}

/// <summary>Decisión técnica según la propuesta del equipo; no constituye una resolución académica.</summary>
public static class MotorEquivalencia
{
    public static ResultadoEquivalencia Evaluar(decimal? contenido, decimal? profundidad,
        decimal? horas, decimal? creditos, decimal umbral)
    {
        if (umbral is <= 0 or > 100) throw new ArgumentOutOfRangeException(nameof(umbral));
        Porcentaje?[] dimensiones = [Porcentaje.DesdeValor(contenido), Porcentaje.DesdeValor(profundidad),
            Porcentaje.DesdeValor(horas), Porcentaje.DesdeValor(creditos)];
        if (dimensiones.Any(p => p is null)) return new(EstadoEquivalencia.InformacionIncompleta);
        return new(dimensiones.All(p => p!.Value.Valor >= umbral)
            ? EstadoEquivalencia.Homologable : EstadoEquivalencia.NoHomologable);
    }
}

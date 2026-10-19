namespace Homologacion.Domain.Equivalencias;

/// <summary>Valor entre 0 y 100. No redondea antes de comparar con el umbral.</summary>
public readonly record struct Porcentaje
{
    public decimal Valor { get; }
    private Porcentaje(decimal valor) => Valor = valor;

    public static Porcentaje? DesdeValor(decimal? valor)
        => valor is >= 0 and <= 100 ? new Porcentaje(valor.Value) : null;

    public static Porcentaje? DesdeRelacion(decimal? origen, decimal? receptora)
    {
        if (origen is null or <= 0 || receptora is null or <= 0) return null;
        // Comparar antes de dividir evita desbordar cuando el origen es mucho mayor.
        return new Porcentaje(origen >= receptora ? 100 : origen.Value / receptora.Value * 100);
    }

    public static Porcentaje? DesdeCobertura(int cubiertos, int totalReceptores)
        => totalReceptores <= 0 || cubiertos < 0 || cubiertos > totalReceptores
            ? null : new Porcentaje((decimal)cubiertos / totalReceptores * 100);
}

namespace Homologacion.Domain.Equivalencias;

public sealed record ResultadoCoberturaAcademica(decimal? Porcentaje,
    IReadOnlyList<string> ContenidosSinCobertura, IReadOnlyList<string> ResultadosSinCobertura);

/// <summary>Cada contenido y resultado receptor pesa un elemento; no promedia porcentajes separados.</summary>
public static class CoberturaAcademica
{
    public static ResultadoCoberturaAcademica Calcular(IReadOnlyCollection<string>? contenidosReceptores,
        IReadOnlyCollection<string>? resultadosReceptores, IReadOnlyCollection<string>? contenidosCubiertos,
        IReadOnlyCollection<string>? resultadosCubiertos)
    {
        var contenidos = contenidosCubiertos?.ToHashSet(StringComparer.Ordinal);
        var resultados = resultadosCubiertos?.ToHashSet(StringComparer.Ordinal);
        var contenidosPendientes = contenidosReceptores?.Where(c => !string.IsNullOrWhiteSpace(c) && contenidos?.Contains(c) != true).ToArray() ?? [];
        var resultadosPendientes = resultadosReceptores?.Where(r => !string.IsNullOrWhiteSpace(r) && resultados?.Contains(r) != true).ToArray() ?? [];
        if (!InventarioValido(contenidosReceptores) || !InventarioValido(resultadosReceptores)
            || contenidos is null || resultados is null
            || contenidos.Any(c => !contenidosReceptores!.Contains(c, StringComparer.Ordinal))
            || resultados.Any(r => !resultadosReceptores!.Contains(r, StringComparer.Ordinal)))
            return new(null, contenidosPendientes, resultadosPendientes);

        // Ampliar antes de sumar evita desbordar el total de las dos categorías.
        var total = (long)contenidosReceptores!.Count + resultadosReceptores!.Count;
        var cubiertos = (long)contenidos.Count + resultados.Count;
        return new(total == 0 ? null : (decimal)cubiertos / total * 100, contenidosPendientes, resultadosPendientes);
    }

    private static bool InventarioValido(IReadOnlyCollection<string>? elementos)
        => elementos is not null && !elementos.Any(string.IsNullOrWhiteSpace)
            && elementos.Distinct(StringComparer.Ordinal).Count() == elementos.Count;
}

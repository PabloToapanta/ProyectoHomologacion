namespace Homologacion.Application.AnalisisInterno;

public static class ValidarExplicacionAgente
{
    public static void Validar(ContextoAgente contexto, ExplicacionAgente explicacion)
    {
        if (explicacion is null || string.IsNullOrWhiteSpace(explicacion.Resumen)
            || explicacion.Resumen.Length > 4000 || explicacion.EvidenciasCitadas is null
            || explicacion.EvidenciasCitadas.Count > 200)
            throw new FalloAgenteException(TipoFalloAgente.RespuestaInvalida);
        var ids = contexto.Evidencias.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var citadas = explicacion.EvidenciasCitadas;
        if ((ids.Count > 0 && citadas.Count == 0)
            || citadas.Distinct(StringComparer.Ordinal).Count() != citadas.Count
            || citadas.Any(id => !ids.Contains(id)))
            throw new FalloAgenteException(TipoFalloAgente.RespuestaInvalida);
    }
}

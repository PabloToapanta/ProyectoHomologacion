namespace Homologacion.Domain.Finalizacion;

public sealed class DecisionCoordinadorInvalidaException(string mensaje) : Exception(mensaje);

/// <summary>
/// HU-33: en una pareja «Sin acuerdo» el coordinador registra una decisión académica propia y motivada (Convalidar o NoConvalidar,
/// con justificación). No elige automáticamente entre los docentes. Regla del flujo del proyecto (decisión del 20/11/2026),
/// pendiente de confirmación institucional; las demás parejas conservan la sugerencia coincidente de los docentes.
/// </summary>
public static class ReglaDecisionCoordinador
{
    public const int MaxJustificacion = 2000;
    public const string SinAcuerdo = "SinAcuerdo";
    public const string Convalidar = "Convalidar";
    public const string NoConvalidar = "NoConvalidar";

    public static (string Resultado, string Justificacion) Validar(string? sugerenciaFinal, string? resultado, string? justificacion)
    {
        if (sugerenciaFinal != SinAcuerdo)
            throw new DecisionCoordinadorInvalidaException("Solo las parejas «Sin acuerdo» requieren la decisión del coordinador; las demás conservan la sugerencia de los docentes.");
        if (resultado is not (Convalidar or NoConvalidar))
            throw new DecisionCoordinadorInvalidaException("Seleccione su decisión: Convalidar o NoConvalidar.");
        var texto = justificacion?.Trim();
        if (string.IsNullOrEmpty(texto)) throw new DecisionCoordinadorInvalidaException("La decisión requiere una justificación.");
        if (texto.Length > MaxJustificacion) throw new DecisionCoordinadorInvalidaException($"La justificación admite hasta {MaxJustificacion} caracteres.");
        if (texto.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
            throw new DecisionCoordinadorInvalidaException("La justificación contiene caracteres no permitidos.");
        return (resultado, texto);
    }
}

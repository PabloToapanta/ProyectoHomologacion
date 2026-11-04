namespace Homologacion.Domain.RevisionDocente;

public sealed class RevisionDocenteInvalidaException(string mensaje) : Exception(mensaje);

/// <summary>
/// HU-31: «De acuerdo» solo sobre un análisis completo del agente y toma su sugerencia;
/// «Ajuste» exige la decisión propia y una observación. Nunca acepta una decisión del cliente distinta de la sugerencia.
/// </summary>
public static class ReglaRevisionDocente
{
    public const int MaxObservacion = 2000;

    public static (string Postura, string Decision, string? Observacion) Resolver(string? postura, string? decision, string? observacion,
        string estadoAgente, string? sugerenciaAgente)
    {
        var texto = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        if (texto?.Length > MaxObservacion) throw new RevisionDocenteInvalidaException($"La observación admite hasta {MaxObservacion} caracteres.");
        switch (postura)
        {
            case "DeAcuerdo":
                var sugerida = Normalizar(sugerenciaAgente);
                if (estadoAgente != "Completo" || sugerida is null)
                    throw new RevisionDocenteInvalidaException("Solo puede estar de acuerdo con un análisis completo del agente; registre un ajuste con su decisión.");
                if (decision is not null && decision != sugerida)
                    throw new RevisionDocenteInvalidaException("Para discrepar de la sugerencia del agente registre un ajuste con observación.");
                return ("DeAcuerdo", sugerida, texto);
            case "Ajuste":
                if (decision is not ("Homologable" or "NoHomologable"))
                    throw new RevisionDocenteInvalidaException("Seleccione su decisión: Homologable o NoHomologable.");
                if (texto is null) throw new RevisionDocenteInvalidaException("El ajuste requiere una observación.");
                return ("Ajuste", decision, texto);
            default:
                throw new RevisionDocenteInvalidaException("Seleccione la postura: DeAcuerdo o Ajuste.");
        }
    }

    private static string? Normalizar(string? sugerencia) => sugerencia switch
    {
        "Homologable" => "Homologable",
        "No homologable" or "NoHomologable" => "NoHomologable",
        _ => null
    };
}

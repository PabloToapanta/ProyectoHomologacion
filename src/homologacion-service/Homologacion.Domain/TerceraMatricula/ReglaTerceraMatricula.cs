namespace Homologacion.Domain.TerceraMatricula;

public enum ResultadoTerceraMatricula { NoAplica, Excluida, IntentoDesconocido }

/// <summary>
/// HU-17 / RN-07. Criterio del 20/11/2026 (respuesta del ingeniero sobre el RRA del CES, art. 74, y el instructivo UTA
/// RES CAU-P-005-2023; regla de análisis del proyecto pendiente de confirmación institucional): la asignatura objeto de
/// tercera matrícula queda excluida de la homologación; la solicitud no se rechaza y las demás asignaturas se evalúan.
/// Un intento desconocido no se asume como primera matrícula.
/// </summary>
public static class ReglaTerceraMatricula
{
    public const int Tercera = 3;
    public const string Motivo =
        "Excluida por tercera matrícula (RN-07): la asignatura aprobada en tercera matrícula no se homologa; las demás asignaturas de la solicitud se evalúan normalmente.";
    public const string FaltaIntento = "intento de matrícula no disponible en el registro de la solicitud: requiere revisión (RN-07)";

    public static ResultadoTerceraMatricula Evaluar(int? intento) => intento switch
    {
        null => ResultadoTerceraMatricula.IntentoDesconocido,
        1 or 2 => ResultadoTerceraMatricula.NoAplica,
        Tercera => ResultadoTerceraMatricula.Excluida,
        _ => throw new ArgumentOutOfRangeException(nameof(intento), "El intento de matrícula debe ser primera, segunda o tercera.")
    };

    public static bool Excluye(int? intento) => intento == Tercera;
}

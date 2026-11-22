using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Domain.Antiguedad;
using Homologacion.Domain.Equivalencias;
using Homologacion.Domain.TerceraMatricula;

namespace Homologacion.Application.TerceraMatricula;

/// <summary>
/// HU-17 / RN-07: resultado determinista de una asignatura aprobada en tercera matrícula. No invoca al agente;
/// conserva los porcentajes del motor como información y fija «No homologable» con el motivo de la exclusión.
/// </summary>
public static class ExclusionTerceraMatricula
{
    public const string Mensaje =
        "Asignatura excluida por tercera matrícula (RN-07): no se homologa y no se invocó al agente. Las demás asignaturas se evalúan; requiere revisión académica humana.";

    /// <summary>Traduce el intento a la regla; un valor fuera de 1–3 indica una fuente malformada.</summary>
    public static ResultadoTerceraMatricula Evaluar(int? intento)
    {
        try { return ReglaTerceraMatricula.Evaluar(intento); }
        catch (ArgumentOutOfRangeException) { throw new FuenteAnalisisNoDisponibleException(); }
    }

    public static ResultadoInternoSolicitud Resultado(Guid solicitudId, Guid aprobadaId, Guid receptoraId, EvaluacionEquivalencia motor,
        ResultadoAntiguedad? antiguedad, bool sinteticos, IEnumerable<string> advertencias)
    {
        var evaluacion = motor with { Estado = EstadoEquivalencia.NoHomologable, Justificacion = ReglaTerceraMatricula.Motivo };
        var resumen = new ResumenAnalisisInterno(evaluacion.CompatibilidadAcademicaPct, evaluacion.CompatibilidadHorariaPct,
            evaluacion.CompatibilidadCreditosPct, "No homologable", ReglaTerceraMatricula.Motivo, []);
        return new(solicitudId, aprobadaId, receptoraId, "Completo", Mensaje, resumen, evaluacion, antiguedad, sinteticos,
            [ReglaTerceraMatricula.Motivo, .. advertencias]);
    }
}

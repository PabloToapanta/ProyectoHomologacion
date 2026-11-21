using Homologacion.Application.AnalisisInterno;

namespace Homologacion.Api.Contracts;

/// <summary>Resultado de una pareja de asignaturas; no declara aceptación de la solicitud.</summary>
public sealed record AnalisisInternoResponse(Guid SolicitudId, Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId,
    string Estado, string Mensaje, ResumenAnalisisInterno? Resultado, EvaluarResponse Evaluacion,
    ValidarAntiguedadResponse? Antiguedad, bool DatosSinteticos, IReadOnlyList<string> Advertencias)
{
    public static AnalisisInternoResponse Desde(ResultadoInternoSolicitud r) => new(r.SolicitudId, r.AsignaturaAprobadaId,
        r.AsignaturaReceptoraId, r.Estado, r.Mensaje, r.Resultado, EvaluarResponse.FromApplication(r.Evaluacion),
        r.Antiguedad is null ? null : ValidarAntiguedadResponse.FromDomain(r.Antiguedad), r.DatosSinteticos, r.Advertencias);
}

using System.Text.Json.Serialization;
using Homologacion.Application.Equivalencias;

namespace Homologacion.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AsignaturaRequest(string? Id, string? PlanAnalitico, decimal? Horas, string? UnidadHoras,
    decimal? Creditos, string? SistemaCreditos, IReadOnlyList<string?>? Contenidos, IReadOnlyList<string?>? ResultadosAprendizaje);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OrigenRequest(string? Id, bool? Aprobada, string? PlanAnalitico, decimal? Horas, string? UnidadHoras,
    decimal? Creditos, string? SistemaCreditos, IReadOnlyList<string?>? Contenidos, IReadOnlyList<string?>? ResultadosAprendizaje);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EvidenciaRequest(string? UbicacionOrigen, string? UbicacionReceptora);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CorrespondenciaRequest(string? AsignaturaOrigen, string? ElementoOrigen,
    string? ElementoReceptor, EvidenciaRequest? Evidencia);

/// <summary>Contrato incompatible con la versión de cuatro dimensiones. Solo evidencia estructurada explícita.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EvaluarRequest(IReadOnlyList<OrigenRequest?>? Origenes, AsignaturaRequest? Receptora,
    IReadOnlyList<CorrespondenciaRequest?>? CorrespondenciasContenidos,
    IReadOnlyList<CorrespondenciaRequest?>? CorrespondenciasResultadosAprendizaje)
{
    public SolicitudEquivalencia ToApplication() => new(
        Origenes?.Select(o => o is null ? null : new OrigenComparacion(
            new DatosAsignatura(o.Id, o.PlanAnalitico, o.Horas, o.UnidadHoras, o.Creditos,
                o.SistemaCreditos, o.Contenidos, o.ResultadosAprendizaje), o.Aprobada)).ToArray(),
        Receptora is null ? null : new DatosAsignatura(Receptora.Id, Receptora.PlanAnalitico,
            Receptora.Horas, Receptora.UnidadHoras, Receptora.Creditos, Receptora.SistemaCreditos,
            Receptora.Contenidos, Receptora.ResultadosAprendizaje),
        Mapear(CorrespondenciasContenidos), Mapear(CorrespondenciasResultadosAprendizaje));

    private static IReadOnlyList<CorrespondenciaAcademica?>? Mapear(IReadOnlyList<CorrespondenciaRequest?>? lista)
        => lista?.Select(c => c is null ? null : new CorrespondenciaAcademica(c.AsignaturaOrigen,
            c.ElementoOrigen, c.ElementoReceptor, c.Evidencia is null ? null :
                new EvidenciaAcademica(c.Evidencia.UbicacionOrigen, c.Evidencia.UbicacionReceptora))).ToArray();
}

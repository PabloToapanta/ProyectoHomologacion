using Homologacion.Application.Equivalencias;

namespace Homologacion.Api.Contracts;

/// <summary>Instantáneas estructuradas, independientes del almacenamiento del catálogo.</summary>
public sealed record AsignaturaRequest(string? Id, decimal? Horas, decimal? Creditos, IReadOnlyList<string?>? Temas)
{
    public DatosAsignatura ToApplication() => new(Id, Horas, Creditos, Temas);
}

/// <summary>Correspondencia declarada; el motor no deduce equivalencia semántica de los identificadores.</summary>
public sealed record CorrespondenciaRequest(string? TemaOrigen, string? TemaReceptor);

public sealed record EvaluarRequest(AsignaturaRequest? Origen, AsignaturaRequest? Receptora,
    IReadOnlyList<CorrespondenciaRequest?>? Correspondencias)
{
    public SolicitudEquivalencia ToApplication() => new(Origen?.ToApplication(), Receptora?.ToApplication(),
        Correspondencias?.Select(c => c is null ? null : new CorrespondenciaTema(c.TemaOrigen, c.TemaReceptor)).ToArray());
}

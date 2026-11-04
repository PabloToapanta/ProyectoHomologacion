using System.Text.Json.Serialization;

namespace Homologacion.Api.Contracts;

/// <summary>Solo identificadores. Las cantidades, aprobación y correspondencias se obtienen del servidor.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AnalisisInternoRequest(Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId);

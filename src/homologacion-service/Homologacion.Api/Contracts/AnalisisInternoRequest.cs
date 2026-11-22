using System.Text.Json.Serialization;

namespace Homologacion.Api.Contracts;

/// <summary>Solo identificadores. Las cantidades, aprobación y correspondencias se obtienen del servidor.</summary>
/// <param name="ForzarNuevo">HU-37: pide una explicación nueva del agente aunque exista una reutilizable.</param>
/// <param name="Motivo">HU-37: obligatorio con <c>forzarNuevo</c>; hasta 500 caracteres, sin caracteres de control.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AnalisisInternoRequest(Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, bool ForzarNuevo = false, string? Motivo = null);

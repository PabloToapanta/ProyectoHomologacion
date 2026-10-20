using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Homologacion.Application.Antiguedad;
using Homologacion.Domain.Antiguedad;

namespace Homologacion.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ValidarAntiguedadRequest
{
    [Required]
    [RegularExpression("^(Aprobada|Reprobada|NoCursada)$")]
    public string? EstadoAsignatura { get; init; }
    public DateOnly? FechaAprobacion { get; init; }
    [Required]
    public DateOnly? FechaEvaluacion { get; init; }

    public SolicitudAntiguedad ToApplication() => new(
        Enum.Parse<EstadoAsignatura>(EstadoAsignatura!), FechaAprobacion, FechaEvaluacion!.Value);
}

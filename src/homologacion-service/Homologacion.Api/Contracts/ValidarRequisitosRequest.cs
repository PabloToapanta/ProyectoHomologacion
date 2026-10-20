using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Homologacion.Application.RequisitosCambio;
using Homologacion.Domain.RequisitosCambio;

namespace Homologacion.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ValidarRequisitosRequest
{
    [Required, RegularExpression("^(Interno|Externo)$")]
    public string? TipoCambio { get; init; }
    [Required, Range(0, int.MaxValue)]
    public int? PeriodosCursados { get; init; }
    [Required, Range(0, int.MaxValue)]
    public int? AsignaturasHomologables { get; init; }
    [Required, Range(0, int.MaxValue)]
    public int? CambiosPrevios { get; init; }

    public SolicitudRequisitosCambio ToApplication() => new(
        Enum.Parse<TipoCambio>(TipoCambio!), PeriodosCursados!.Value,
        AsignaturasHomologables!.Value, CambiosPrevios!.Value);
}

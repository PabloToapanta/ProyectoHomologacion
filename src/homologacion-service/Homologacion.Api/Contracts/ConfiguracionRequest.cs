using System.Text.Json.Serialization;
using Homologacion.Application.Configuracion;

namespace Homologacion.Api.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ConfiguracionRequest
{
    public required int UmbralPct { get; init; }
    public required int AntiguedadMaximaAnios { get; init; }
    public required int PeriodosMinimosInterno { get; init; }
    public required int AsignaturasHomologablesMinimasInterno { get; init; }
    public required int PeriodosMinimosExterno { get; init; }
    public required int AsignaturasHomologablesMinimasExterno { get; init; }
    public required string Motivo { get; init; }
    public required string Respaldo { get; init; }

    public CambioConfiguracion ToApplication() => new(UmbralPct, AntiguedadMaximaAnios,
        PeriodosMinimosInterno, AsignaturasHomologablesMinimasInterno,
        PeriodosMinimosExterno, AsignaturasHomologablesMinimasExterno, Motivo, Respaldo);
}

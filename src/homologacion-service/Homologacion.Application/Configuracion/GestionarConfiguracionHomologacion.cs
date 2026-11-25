using Homologacion.Domain.Configuracion;

namespace Homologacion.Application.Configuracion;

public sealed record CambioConfiguracion(int UmbralPct, int AntiguedadMaximaAnios,
    int PeriodosMinimosInterno, int AsignaturasHomologablesMinimasInterno,
    int PeriodosMinimosExterno, int AsignaturasHomologablesMinimasExterno,
    string? Motivo, string? Respaldo);

public sealed record EntradaHistorialConfiguracion(Guid VersionId, Guid? VersionAnteriorId,
    DateTimeOffset FechaHoraUtc, Guid ActorId, ParametrosHomologacion? Anterior,
    ParametrosHomologacion Nueva, string Motivo, string Respaldo);
public sealed record PaginaHistorialConfiguracion(IReadOnlyList<EntradaHistorialConfiguracion> Entradas, Guid? CursorSiguiente);

public interface IConfiguracionHomologacionStore : IParametrosHomologacionProvider
{
    Task<ParametrosHomologacion> ActualizarAsync(Guid versionEsperada, ParametrosHomologacion nueva,
        Guid actorId, DateTimeOffset fechaHoraUtc, string motivo, string respaldo, CancellationToken ct);
    Task<PaginaHistorialConfiguracion> HistorialAsync(Guid? cursor, int limit, CancellationToken ct);
}

public sealed class ConfiguracionObsoletaException : Exception;
public sealed class ConfiguracionInvalidaException(string mensaje) : Exception(mensaje);

public sealed class GestionarConfiguracionHomologacion(IConfiguracionHomologacionStore almacen, TimeProvider reloj)
{
    public Task<ParametrosHomologacion> ObtenerAsync(CancellationToken ct) => almacen.ObtenerVigentesAsync(ct);

    public async Task<ParametrosHomologacion> ActualizarAsync(Guid versionEsperada, CambioConfiguracion cambio,
        Guid actorId, CancellationToken ct)
    {
        if (versionEsperada == Guid.Empty) throw new ConfiguracionInvalidaException("If-Match debe contener una versión válida.");
        if (actorId == Guid.Empty) throw new ConfiguracionInvalidaException("No se pudo identificar a quien modifica la configuración.");
        if (cambio is null) throw new ConfiguracionInvalidaException("Indique todos los parámetros.");
        var motivo = cambio.Motivo?.Trim();
        var respaldo = cambio.Respaldo?.Trim();
        if (string.IsNullOrEmpty(motivo) || motivo.Length > 500)
            throw new ConfiguracionInvalidaException("El motivo debe tener entre 1 y 500 caracteres.");
        if (string.IsNullOrEmpty(respaldo) || respaldo.Length > 2000)
            throw new ConfiguracionInvalidaException("El respaldo debe tener entre 1 y 2000 caracteres.");
        ParametrosHomologacion nuevos;
        try
        {
            nuevos = new(Guid.NewGuid(), cambio.UmbralPct, cambio.AntiguedadMaximaAnios,
                cambio.PeriodosMinimosInterno, cambio.AsignaturasHomologablesMinimasInterno,
                cambio.PeriodosMinimosExterno, cambio.AsignaturasHomologablesMinimasExterno);
        }
        catch (ArgumentOutOfRangeException)
        { throw new ConfiguracionInvalidaException("El umbral debe ser entero de 1 a 100; años y mínimos, enteros de 1 a 50."); }
        return await almacen.ActualizarAsync(versionEsperada, nuevos, actorId, reloj.GetUtcNow(), motivo, respaldo, ct);
    }

    public Task<PaginaHistorialConfiguracion> HistorialAsync(Guid? cursor, int limit, CancellationToken ct)
    {
        if (limit is < 1 or > 100) throw new ConfiguracionInvalidaException("El límite debe estar entre 1 y 100.");
        if (cursor == Guid.Empty) throw new ConfiguracionInvalidaException("El cursor es inválido.");
        return almacen.HistorialAsync(cursor, limit, ct);
    }
}

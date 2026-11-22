using Homologacion.Application.Configuracion;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Domain.Configuracion;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Homologacion.Infrastructure.Configuracion;

public sealed class RegistroConfiguracionHomologacion
{
    public Guid VersionId { get; set; }
    public Guid? VersionAnteriorId { get; set; }
    public int UmbralPct { get; set; }
    public int AntiguedadMaximaAnios { get; set; }
    public int PeriodosMinimosInterno { get; set; }
    public int AsignaturasHomologablesMinimasInterno { get; set; }
    public int PeriodosMinimosExterno { get; set; }
    public int AsignaturasHomologablesMinimasExterno { get; set; }
    public Guid ActorId { get; set; }
    public DateTimeOffset FechaHoraUtc { get; set; }
    public string Motivo { get; set; } = "";
    public string Respaldo { get; set; } = "";
    public byte[] RowVersion { get; set; } = [];

    public ParametrosHomologacion AParametros() => new(VersionId, UmbralPct, AntiguedadMaximaAnios,
        PeriodosMinimosInterno, AsignaturasHomologablesMinimasInterno,
        PeriodosMinimosExterno, AsignaturasHomologablesMinimasExterno);
}

public sealed class ConfiguracionHomologacionVigente
{
    public int Id { get; set; }
    public Guid VersionId { get; set; }
}

public sealed class ParametrosHomologacionSql(ResultadosDbContext db) : IConfiguracionHomologacionStore
{
    public async Task<ParametrosHomologacion> ObtenerVigentesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var vigente = await db.ConfiguracionVigente.AsNoTracking()
                .Where(x => x.Id == 1)
                .Join(db.Configuraciones.AsNoTracking(), x => x.VersionId, x => x.VersionId,
                    (_, configuracion) => configuracion)
                .SingleOrDefaultAsync(cancellationToken);

            return vigente?.AParametros()
                ?? throw new AlmacenAnalisisNoDisponibleException();
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            throw new AlmacenAnalisisNoDisponibleException();
        }
        catch (ArgumentException)
        {
            throw new AlmacenAnalisisNoDisponibleException();
        }
    }

    public async Task<ParametrosHomologacion> ActualizarAsync(Guid versionEsperada, ParametrosHomologacion nueva,
        Guid actorId, DateTimeOffset fechaHoraUtc, string motivo, string respaldo, CancellationToken ct)
    {
        try
        {
            await using var transaccion = await db.Database.BeginTransactionAsync(ct);
            var actual = await db.ConfiguracionVigente.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
                ?? throw new AlmacenAnalisisNoDisponibleException();
            if (actual.VersionId != versionEsperada) throw new ConfiguracionObsoletaException();
            db.Configuraciones.Add(new RegistroConfiguracionHomologacion
            {
                VersionId = nueva.VersionId, VersionAnteriorId = versionEsperada,
                UmbralPct = nueva.UmbralPct, AntiguedadMaximaAnios = nueva.AntiguedadMaximaAnios,
                PeriodosMinimosInterno = nueva.PeriodosMinimosInterno,
                AsignaturasHomologablesMinimasInterno = nueva.AsignaturasHomologablesMinimasInterno,
                PeriodosMinimosExterno = nueva.PeriodosMinimosExterno,
                AsignaturasHomologablesMinimasExterno = nueva.AsignaturasHomologablesMinimasExterno,
                ActorId = actorId, FechaHoraUtc = fechaHoraUtc, Motivo = motivo, Respaldo = respaldo
            });
            await db.SaveChangesAsync(ct);
            var filas = await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE ConfiguracionHomologacionVigente SET VersionId = {nueva.VersionId} WHERE Id = 1 AND VersionId = {versionEsperada}", ct);
            if (filas != 1) throw new ConfiguracionObsoletaException();
            await transaccion.CommitAsync(ct);
            return nueva;
        }
        catch (SqlException error) when (error.Number == 1205)
        { throw new ConfiguracionObsoletaException(); }
        catch (SqlException)
        { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (DbUpdateException)
        { throw new AlmacenAnalisisNoDisponibleException(); }
    }

    public async Task<PaginaHistorialConfiguracion> HistorialAsync(Guid? cursor, int limit, CancellationToken ct)
    {
        try
        {
            var vigente = await db.ConfiguracionVigente.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct)
                ?? throw new AlmacenAnalisisNoDisponibleException();
            var id = vigente.VersionId;
            if (cursor is { } solicitado)
            {
                while (id != solicitado)
                {
                    var paso = await db.Configuraciones.AsNoTracking().SingleOrDefaultAsync(x => x.VersionId == id, ct)
                        ?? throw new AlmacenAnalisisNoDisponibleException();
                    if (paso.VersionAnteriorId is not { } anterior)
                        throw new ConfiguracionInvalidaException("El cursor no pertenece al historial vigente.");
                    id = anterior;
                }
            }
            var entradas = new List<EntradaHistorialConfiguracion>();
            while (entradas.Count < limit + 1)
            {
                var fila = await db.Configuraciones.AsNoTracking().SingleOrDefaultAsync(x => x.VersionId == id, ct)
                    ?? throw new AlmacenAnalisisNoDisponibleException();
                RegistroConfiguracionHomologacion? anterior = null;
                if (fila.VersionAnteriorId is { } anteriorId)
                    anterior = await db.Configuraciones.AsNoTracking().SingleOrDefaultAsync(x => x.VersionId == anteriorId, ct)
                        ?? throw new AlmacenAnalisisNoDisponibleException();
                entradas.Add(new(fila.VersionId, fila.VersionAnteriorId, fila.FechaHoraUtc,
                    fila.ActorId, anterior?.AParametros(), fila.AParametros(), fila.Motivo, fila.Respaldo));
                if (fila.VersionAnteriorId is not { } siguiente) break;
                id = siguiente;
            }
            var mas = entradas.Count > limit;
            return new(entradas.Take(limit).ToArray(), mas ? entradas[limit].VersionId : null);
        }
        catch (SqlException)
        { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (ArgumentException)
        { throw new AlmacenAnalisisNoDisponibleException(); }
    }
}

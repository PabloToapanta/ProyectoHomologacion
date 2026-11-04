using System.Data.Common;
using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.EntityFrameworkCore;

namespace Homologacion.Infrastructure.Informe;

/// <summary>HU-23: fila de InformesHomologacion; la clave por solicitud impide un segundo informe.</summary>
public sealed class RegistroInforme
{
    public Guid SolicitudId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public Guid GeneradoPorId { get; set; }
    public string Contenido { get; set; } = "";
}

public sealed class InformesHomologacionSql(ResultadosDbContext db) : IInformesHomologacion
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<InformeHomologacion?> ObtenerAsync(Guid solicitudId, CancellationToken ct)
    {
        try
        {
            var fila = await db.Informes.AsNoTracking().SingleOrDefaultAsync(x => x.SolicitudId == solicitudId, ct);
            if (fila is null) return null;
            var informe = JsonSerializer.Deserialize<InformeHomologacion>(fila.Contenido, JsonOptions);
            if (informe is null || informe.SolicitudId != fila.SolicitudId) throw new AlmacenAnalisisNoDisponibleException();
            return informe;
        }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (JsonException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }

    /// <summary>Transacción con un bloqueo exclusivo de aplicación por solicitud (sp_getapplock en SQL Server). Lo usan la
    /// generación del informe y el guardado de análisis docentes con el mismo DbContext, así que sus escrituras van en la transacción.</summary>
    public async Task<T> ConCierreAsync<T>(Guid solicitudId, Func<Task<T>> accion, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null) return await accion();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaccion;
        try
        {
            transaccion = await db.Database.BeginTransactionAsync(ct);
            if (db.Database.IsSqlServer())
                await db.Database.ExecuteSqlInterpolatedAsync($@"DECLARE @r int;
EXEC @r = sp_getapplock @Resource = {"homologacion-informe:" + solicitudId.ToString("N")}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
IF @r < 0 THROW 50023, 'No se obtuvo el cierre del informe.', 1;", ct);
        }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
        await using (transaccion)
        {
            var resultado = await accion();
            try { await transaccion.CommitAsync(ct); }
            catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
            return resultado;
        }
    }

    public async Task GuardarAsync(InformeHomologacion informe, CancellationToken ct)
    {
        try
        {
            db.Informes.Add(new() { SolicitudId = informe.SolicitudId, Fecha = informe.Fecha, GeneradoPorId = informe.GeneradoPorId,
                Contenido = JsonSerializer.Serialize(informe, JsonOptions) });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception error) when (error is DbUpdateException or DbException)
        {
            // Si otra petición ya lo generó, es un conflicto; si no, la base no está disponible.
            db.ChangeTracker.Clear();
            bool existe;
            try { existe = await db.Informes.AsNoTracking().AnyAsync(x => x.SolicitudId == informe.SolicitudId, ct); }
            catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
            if (existe) throw new InformeYaGeneradoException();
            throw new AlmacenAnalisisNoDisponibleException();
        }
    }
}

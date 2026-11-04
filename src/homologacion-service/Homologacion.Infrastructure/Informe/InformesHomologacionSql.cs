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

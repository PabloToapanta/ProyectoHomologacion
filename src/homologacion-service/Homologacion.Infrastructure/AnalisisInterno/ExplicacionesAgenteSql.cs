using System.Data.Common;
using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Microsoft.EntityFrameworkCore;

namespace Homologacion.Infrastructure.AnalisisInterno;

/// <summary>HU-37: fila de ExplicacionesAgente. Índice no único por clave; en SQL Server un trigger impide UPDATE y DELETE.</summary>
public sealed class RegistroExplicacionAgente
{
    public Guid Id { get; set; }
    public string Clave { get; set; } = "";
    public DateTimeOffset Fecha { get; set; }
    public string Modelo { get; set; } = "";
    public string VersionPlantilla { get; set; } = "";
    public Guid VersionParametros { get; set; }
    public string Explicacion { get; set; } = "";
    public Guid? ForzadoPorId { get; set; }
    public string? Motivo { get; set; }
}

/// <summary>Solo inserta y lee. No guarda datos del estudiante: la explicación trata programas, evaluación y correspondencias.</summary>
public sealed class ExplicacionesAgenteSql(ResultadosDbContext db) : IExplicacionesAgente
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ExplicacionGuardada?> BuscarAsync(string clave, CancellationToken ct)
    {
        RegistroExplicacionAgente[] filas;
        try { filas = await db.ExplicacionesAgente.AsNoTracking().Where(x => x.Clave == clave).ToArrayAsync(ct); }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
        // Pocas filas por clave; se ordena en memoria (SQLite no ordena DateTimeOffset).
        var fila = filas.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).FirstOrDefault();
        if (fila is null) return null;
        ExplicacionAgente? explicacion;
        try { explicacion = JsonSerializer.Deserialize<ExplicacionAgente>(fila.Explicacion, Json); }
        catch (JsonException) { return null; }
        return explicacion is null ? null
            : new(fila.Id, fila.Clave, fila.Fecha, fila.Modelo, fila.VersionPlantilla, fila.VersionParametros, explicacion, fila.ForzadoPorId, fila.Motivo);
    }

    public async Task GuardarAsync(ExplicacionGuardada e, CancellationToken ct)
    {
        var fila = new RegistroExplicacionAgente
        {
            Id = e.Id, Clave = e.Clave, Fecha = e.Fecha, Modelo = e.Modelo, VersionPlantilla = e.VersionPlantilla,
            VersionParametros = e.VersionParametros, Explicacion = JsonSerializer.Serialize(e.Explicacion, Json),
            ForzadoPorId = e.ForzadoPorId, Motivo = e.Motivo
        };
        try
        {
            db.ExplicacionesAgente.Add(fila);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception error) when (error is DbUpdateException or DbException)
        {
            db.Entry(fila).State = EntityState.Detached;
            throw new AlmacenAnalisisNoDisponibleException();
        }
    }
}

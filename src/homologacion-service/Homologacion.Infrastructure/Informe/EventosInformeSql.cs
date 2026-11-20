using System.Data.Common;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Domain.Auditoria;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.EntityFrameworkCore;

namespace Homologacion.Infrastructure.Informe;

/// <summary>HU-25: fila de EventosInforme. Un tipo por solicitud (índice único); en SQL Server un trigger impide UPDATE y DELETE.</summary>
public sealed class RegistroEventoInforme
{
    public Guid Id { get; set; }
    public Guid SolicitudId { get; set; }
    public string Tipo { get; set; } = "";
    public DateTimeOffset Fecha { get; set; }
    public Guid UsuarioId { get; set; }
    public string Usuario { get; set; } = "";
    public string Rol { get; set; } = "";
}

/// <summary>Solo inserta y lee; comparte el DbContext con el informe para registrar el evento en su misma transacción.</summary>
public sealed class EventosInformeSql(ResultadosDbContext db) : IAuditoriaInformes
{
    public async Task RegistrarAsync(EventoInforme evento, CancellationToken ct)
    {
        var fila = new RegistroEventoInforme
        {
            Id = evento.Id, SolicitudId = evento.SolicitudId, Tipo = evento.Tipo.ToString(), Fecha = evento.Fecha,
            UsuarioId = evento.UsuarioId, Usuario = evento.Usuario, Rol = evento.Rol
        };
        try
        {
            db.EventosInforme.Add(fila);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception error) when (error is DbUpdateException or DbException)
        {
            db.Entry(fila).State = EntityState.Detached;
            bool existe;
            try { existe = await db.EventosInforme.AsNoTracking().AnyAsync(x => x.SolicitudId == fila.SolicitudId && x.Tipo == fila.Tipo, ct); }
            catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
            if (existe) throw new EventoInformeInvalidoException($"El evento «{fila.Tipo}» del informe ya está registrado.");
            throw new AlmacenAnalisisNoDisponibleException();
        }
    }

    public async Task<IReadOnlyList<EventoInforme>> ListarAsync(Guid solicitudId, CancellationToken ct)
    {
        try
        {
            var filas = await db.EventosInforme.AsNoTracking().Where(x => x.SolicitudId == solicitudId).ToArrayAsync(ct);
            return filas.Select(f => Enum.TryParse<TipoEventoInforme>(f.Tipo, out var tipo) && Enum.IsDefined(tipo)
                ? new EventoInforme(f.Id, f.SolicitudId, tipo, f.Fecha, f.UsuarioId, f.Usuario, f.Rol)
                : throw new AlmacenAnalisisNoDisponibleException()).ToArray();
        }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }
}

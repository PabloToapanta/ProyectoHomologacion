using Homologacion.Application.Agrupaciones;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Application.RevisionDocente;
using Homologacion.Domain.Agrupaciones;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.Json;
namespace Homologacion.Infrastructure.Agrupaciones;

public sealed class RegistroGrupo
{
    public Guid GrupoId { get; set; }
    public Guid SolicitudId { get; set; }
    public Guid Version { get; set; }
    public bool Congelado { get; set; }
    public string? Analisis { get; set; }
}
public sealed class OrigenGrupo { public Guid GrupoId { get; set; } public Guid AprobadaId { get; set; } }
public sealed class ReceptoraGrupo { public Guid GrupoId { get; set; } public Guid ReceptoraId { get; set; } }
public sealed class RegistroAporteGrupo
{
    public Guid GrupoId { get; set; }
    public Guid AprobadaId { get; set; }
    public Guid ReceptoraId { get; set; }
    public decimal? Horas { get; set; }
    public decimal? Creditos { get; set; }
}
public sealed class ReservaOrigen
{
    public Guid SolicitudId { get; set; }
    public Guid AprobadaId { get; set; }
    public Guid? GrupoId { get; set; }
    public Guid? ReceptoraParejaId { get; set; }
}
public sealed class RegistroRevisionGrupo
{
    public Guid GrupoId { get; set; }
    public Guid DocenteId { get; set; }
    public Guid SolicitudId { get; set; }
    public string Contenido { get; set; } = "";
}
public static class ModeloGruposHu21
{
    public static void Configurar(ModelBuilder model)
    {
        var g = model.Entity<RegistroGrupo>(); g.ToTable("Agrupaciones"); g.HasKey(x => x.GrupoId); g.Property(x => x.GrupoId).ValueGeneratedNever();
        g.Property(x => x.Version).IsConcurrencyToken(); g.HasIndex(x => x.SolicitudId);
        var o = model.Entity<OrigenGrupo>(); o.ToTable("OrigenesAgrupacion"); o.HasKey(x => new { x.GrupoId, x.AprobadaId });
        o.HasOne<RegistroGrupo>().WithMany().HasForeignKey(x => x.GrupoId).OnDelete(DeleteBehavior.Cascade);
        var r = model.Entity<ReceptoraGrupo>(); r.ToTable("ReceptorasAgrupacion"); r.HasKey(x => new { x.GrupoId, x.ReceptoraId });
        r.HasOne<RegistroGrupo>().WithMany().HasForeignKey(x => x.GrupoId).OnDelete(DeleteBehavior.Cascade);
        var a = model.Entity<RegistroAporteGrupo>(); a.ToTable("AportesAgrupacion"); a.HasKey(x => new { x.GrupoId, x.AprobadaId, x.ReceptoraId });
        a.Property(x => x.Horas).HasPrecision(12, 2); a.Property(x => x.Creditos).HasPrecision(6, 2);
        a.HasOne<OrigenGrupo>().WithMany().HasForeignKey(x => new { x.GrupoId, x.AprobadaId }).OnDelete(DeleteBehavior.Cascade);
        a.HasOne<ReceptoraGrupo>().WithMany().HasForeignKey(x => new { x.GrupoId, x.ReceptoraId }).OnDelete(DeleteBehavior.NoAction);
        var reserva = model.Entity<ReservaOrigen>(); reserva.ToTable("ReservasOrigen"); reserva.HasKey(x => new { x.SolicitudId, x.AprobadaId });
        var revision = model.Entity<RegistroRevisionGrupo>(); revision.ToTable("RevisionesAgrupacion"); revision.HasKey(x => new { x.GrupoId, x.DocenteId });
        revision.Property(x => x.Contenido).IsRequired(); revision.HasIndex(x => x.SolicitudId);
        revision.HasOne<RegistroGrupo>().WithMany().HasForeignKey(x => x.GrupoId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class GruposAgrupacionSql(ResultadosDbContext db) : IGruposAgrupacion, IResultadosAgrupacionInforme, IRevisionesAgrupacion
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static T Leer<T>(string json) => JsonSerializer.Deserialize<T>(json, Json) ?? throw new AlmacenAnalisisNoDisponibleException();
    private static async Task<T> Seguro<T>(Func<Task<T>> accion)
    {
        try { return await accion(); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoGrupoException("La agrupación cambió; recargue su versión."); }
        catch (DbUpdateException) { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (DbException) { throw new AlmacenAnalisisNoDisponibleException(); }
        catch (JsonException) { throw new AlmacenAnalisisNoDisponibleException(); }
    }
    public Task<IReadOnlyList<DetalleGrupo>> ListarAsync(Guid solicitud, CancellationToken ct) => Seguro<IReadOnlyList<DetalleGrupo>>(async () =>
    {
        var ids = await db.Set<RegistroGrupo>().AsNoTracking().Where(g => g.SolicitudId == solicitud).Select(g => g.GrupoId).ToArrayAsync(ct);
        var lista = new List<DetalleGrupo>(); foreach (var id in ids) lista.Add((await ObtenerAsync(solicitud, id, ct))!); return lista;
    });
    public Task<DetalleGrupo?> ObtenerAsync(Guid solicitud, Guid grupo, CancellationToken ct) => Seguro<DetalleGrupo?>(async () =>
    {
        var row = await db.Set<RegistroGrupo>().AsNoTracking().SingleOrDefaultAsync(g => g.SolicitudId == solicitud && g.GrupoId == grupo, ct);
        if (row is null) return null;
        var os = await db.Set<OrigenGrupo>().AsNoTracking().Where(o => o.GrupoId == grupo).Select(o => o.AprobadaId).ToArrayAsync(ct);
        var rs = await db.Set<ReceptoraGrupo>().AsNoTracking().Where(r => r.GrupoId == grupo).Select(r => r.ReceptoraId).ToArrayAsync(ct);
        var aportes = await db.Set<RegistroAporteGrupo>().AsNoTracking().Where(a => a.GrupoId == grupo).ToArrayAsync(ct);
        var a = row.Analisis is null ? null : Leer<AnalisisAgrupacionGuardado>(row.Analisis);
        if (a is not null && (a.GrupoId != grupo || a.SolicitudId != solicitud)) throw new AlmacenAnalisisNoDisponibleException();
        return new(new(grupo, solicitud, row.Version, row.Congelado, os, rs, aportes.Select(a => new AporteGrupo(a.AprobadaId, a.ReceptoraId, a.Horas, a.Creditos)).ToArray()), a);
    });
    public async Task GuardarAsync(GrupoAgrupacion grupo, AnalisisAgrupacionGuardado? analisis, CancellationToken ct)
    {
        await Seguro(async () =>
        {
            if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("El grupo y las reservas requieren ConCierreAsync.");
            foreach (var origen in grupo.Origenes)
            {
                var reservado = await db.Set<ReservaOrigen>().FindAsync([grupo.SolicitudId, origen], ct);
                var historico = await db.Resultados.AsNoTracking().AnyAsync(x => x.SolicitudId == grupo.SolicitudId && x.AprobadaId == origen, ct)
                    || await db.ResultadosExternos.AsNoTracking().AnyAsync(x => x.SolicitudId == grupo.SolicitudId && x.AprobadaId == origen, ct)
                        || await db.RevisionesDocentes.AsNoTracking().AnyAsync(x => x.SolicitudId == grupo.SolicitudId && x.AprobadaId == origen, ct);
                if (historico || reservado is not null && reservado.GrupoId != grupo.GrupoId)
                    throw new ConflictoGrupoException($"Origen {origen}: reservado en otra pareja o agrupación de la solicitud.");
            }
            var row = await db.Set<RegistroGrupo>().FindAsync([grupo.GrupoId], ct);
            if (row is null) { row = new() { GrupoId = grupo.GrupoId, SolicitudId = grupo.SolicitudId }; db.Add(row); }
            if (row.SolicitudId != grupo.SolicitudId) throw new GrupoNoEncontradoException();
            // Composición congelada no se reescribe al reanalizar.
            if (!row.Congelado)
            {
                await db.Set<RegistroAporteGrupo>().Where(x => x.GrupoId == grupo.GrupoId).ExecuteDeleteAsync(ct);
                await db.Set<OrigenGrupo>().Where(x => x.GrupoId == grupo.GrupoId).ExecuteDeleteAsync(ct);
                await db.Set<ReceptoraGrupo>().Where(x => x.GrupoId == grupo.GrupoId).ExecuteDeleteAsync(ct);
                await db.Set<ReservaOrigen>().Where(x => x.GrupoId == grupo.GrupoId).ExecuteDeleteAsync(ct);
                // Evita entidades hijas obsoletas rastreadas por una operación anterior de esta petición.
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.Entity is OrigenGrupo or ReceptoraGrupo or RegistroAporteGrupo or ReservaOrigen).ToArray()) entry.State = EntityState.Detached;
                db.AddRange(grupo.Origenes.Select(id => new OrigenGrupo { GrupoId = grupo.GrupoId, AprobadaId = id }));
                db.AddRange(grupo.Receptoras.Select(id => new ReceptoraGrupo { GrupoId = grupo.GrupoId, ReceptoraId = id }));
                db.AddRange(grupo.Aportes.Select(a => new RegistroAporteGrupo { GrupoId = grupo.GrupoId, AprobadaId = a.AsignaturaAprobadaId, ReceptoraId = a.AsignaturaReceptoraId, Horas = a.HorasAportadas, Creditos = a.CreditosAportados }));
                db.AddRange(grupo.Origenes.Select(id => new ReservaOrigen { SolicitudId = grupo.SolicitudId, AprobadaId = id, GrupoId = grupo.GrupoId }));
            }
            row.Version = grupo.Version; row.Congelado = grupo.Congelado;
            if (analisis is not null) row.Analisis = JsonSerializer.Serialize(analisis, Json);
            await db.SaveChangesAsync(ct); return true;
        });
    }
    public async Task EliminarAsync(GrupoAgrupacion grupo, CancellationToken ct)
    {
        await Seguro(async () =>
        {
            if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Eliminar requiere ConCierreAsync.");
            if (grupo.Congelado) throw new ConflictoGrupoException("El primer análisis guardado congeló el grupo.");
            await db.Set<ReservaOrigen>().Where(r => r.GrupoId == grupo.GrupoId).ExecuteDeleteAsync(ct);
            await db.Set<RegistroGrupo>().Where(g => g.GrupoId == grupo.GrupoId && g.SolicitudId == grupo.SolicitudId).ExecuteDeleteAsync(ct);
            db.ChangeTracker.Clear(); return true;
        });
    }
    async Task<IReadOnlyList<AnalisisAgrupacionGuardado>> IResultadosAgrupacionInforme.ListarAsync(Guid solicitud, CancellationToken ct) =>
        (await ListarAsync(solicitud, ct)).Where(x => x.UltimoAnalisis is not null).Select(x => x.UltimoAnalisis!).ToArray();
    Task<IReadOnlyList<RevisionAgrupacionRegistrada>> IRevisionesAgrupacionInforme.ListarAsync(Guid solicitud, CancellationToken ct) => Seguro<IReadOnlyList<RevisionAgrupacionRegistrada>>(async () =>
    {
        var rows = await db.Set<RegistroRevisionGrupo>().AsNoTracking().Where(x => x.SolicitudId == solicitud).ToArrayAsync(ct);
        return rows.Select(x => Leer<RevisionAgrupacionRegistrada>(x.Contenido)).ToArray();
    });
    public async Task GuardarAsync(RevisionAgrupacionRegistrada revision, CancellationToken ct)
    {
        await Seguro(async () =>
        {
            if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Revisión requiere ConCierreAsync.");
            var row = await db.Set<RegistroRevisionGrupo>().FindAsync([revision.GrupoId, revision.DocenteId], ct);
            if (row is null) { row = new() { GrupoId = revision.GrupoId, DocenteId = revision.DocenteId, SolicitudId = revision.SolicitudId }; db.Add(row); }
            row.Contenido = JsonSerializer.Serialize(revision, Json); await db.SaveChangesAsync(ct); return true;
        });
    }
}

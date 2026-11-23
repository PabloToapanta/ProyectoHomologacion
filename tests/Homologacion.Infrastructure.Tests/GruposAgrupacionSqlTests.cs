using Homologacion.Domain.Agrupaciones;
using Homologacion.Application.Agrupaciones;
using Homologacion.Infrastructure.Agrupaciones;
using Homologacion.Infrastructure.AnalisisInterno;
using Homologacion.Infrastructure.Informe;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Homologacion.Infrastructure.Tests;

public class GruposAgrupacionSqlTests
{
    [Fact]
    public async Task Grupo_conserva_mallas_al_reabrir_y_admite_historico_sin_metadatos()
    {
        using var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(c).Options;
        var s = Guid.NewGuid(); var o = Guid.NewGuid(); var b = Guid.NewGuid(); var r = Guid.NewGuid();
        var g = GrupoAgrupacion.Crear(s, [o, b], [r], [new(o, r, 40, 4), new(b, r, 40, 4)]);
        var mallas = new Homologacion.Application.AnalisisInterno.MallasAnalisis(
            [new(Guid.NewGuid(), o, new(Guid.NewGuid(), Guid.NewGuid(), "Plan anterior", false)), new(null, b, null)],
            [new(r, null, new(Guid.NewGuid(), Guid.NewGuid(), "Plan receptor", true))]);
        var evaluacion = new Homologacion.Application.Equivalencias.ResultadoAgrupacion([], 80,
            Homologacion.Domain.Equivalencias.EstadoEquivalencia.InformacionIncompleta, "Prueba de persistencia", [], [], []);
        var analisis = new Homologacion.Application.Informe.AnalisisAgrupacionGuardado(s, g.GrupoId, g.Origenes, evaluacion, DateTimeOffset.UtcNow) { Mallas = mallas };
        await using (var db = new ResultadosDbContext(options))
        {
            await db.Database.EnsureCreatedAsync(); var repo = new GruposAgrupacionSql(db); var cierre = new InformesHomologacionSql(db);
            await cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(g, null, default); return true; }, default);
            g.Congelar();
            await cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(g, analisis, default); return true; }, default);
        }
        await using (var db = new ResultadosDbContext(options))
        {
            var detalle = await new GruposAgrupacionSql(db).ObtenerAsync(s, g.GrupoId, default);
            Assert.True(detalle!.Grupo.Congelado);
            Assert.Equal(mallas.Origenes, detalle.UltimoAnalisis!.Mallas!.Origenes);
            Assert.Equal(mallas.Receptoras, detalle.UltimoAnalisis.Mallas.Receptoras);
            var fila = await db.Set<RegistroGrupo>().SingleAsync();
            var json = System.Text.Json.Nodes.JsonNode.Parse(fila.Analisis!)!.AsObject(); json.Remove("mallas");
            fila.Analisis = json.ToJsonString(); await db.SaveChangesAsync();
        }
        await using (var db = new ResultadosDbContext(options))
        {
            var historico = await new GruposAgrupacionSql(db).ObtenerAsync(s, g.GrupoId, default);
            Assert.Null(historico!.UltimoAnalisis!.Mallas);
            Assert.Equal(analisis.Evaluacion.Justificacion, historico.UltimoAnalisis.Evaluacion.Justificacion);
            Assert.True(historico.Grupo.Congelado);
        }
    }

    [Fact]
    public async Task Revision_manual_1a1_sin_agente_tambien_reserva_la_unidad_y_no_roba_un_grupo()
    {
        using var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(c).Options);
        await db.Database.EnsureCreatedAsync(); var repo = new GruposAgrupacionSql(db); var cierre = new InformesHomologacionSql(db);
        var s = Guid.NewGuid(); var o = Guid.NewGuid(); var r = Guid.NewGuid(); var t = Guid.NewGuid();
        var g = GrupoAgrupacion.Crear(s, [o], [r, t], [new(o, r, 1, 1), new(o, t, 2, 2)]);
        await cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(g, null, default); return true; }, default);
        var revisiones = new Homologacion.Infrastructure.RevisionDocente.RevisionesDocentesSql(db);
        await Assert.ThrowsAsync<ConflictoGrupoException>(() => revisiones.GuardarAsync(new(s, o, r, Guid.NewGuid(), "Docente", "Ajuste", "NoHomologable", "Sin agente", "SinAnalisis", null, DateTimeOffset.UtcNow), default));
        var otro = Guid.NewGuid(); await revisiones.GuardarAsync(new(s, otro, r, Guid.NewGuid(), "Docente", "Ajuste", "NoHomologable", "Sin agente", "SinAnalisis", null, DateTimeOffset.UtcNow), default);
        var nuevo = GrupoAgrupacion.Crear(s, [otro], [r, t], [new(otro, r, 1, 1), new(otro, t, 2, 2)]);
        await Assert.ThrowsAsync<ConflictoGrupoException>(() => cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(nuevo, null, default); return true; }, default));
    }
    [Fact]
    public async Task Corregir_borrador_intercambia_reservas_sin_modificar_otro_grupo_y_conserva_null()
    {
        using var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(c).Options);
        await db.Database.EnsureCreatedAsync(); var repo = new GruposAgrupacionSql(db); var cierre = new InformesHomologacionSql(db);
        var s = Guid.NewGuid(); var a = Guid.NewGuid(); var b = Guid.NewGuid(); var r = Guid.NewGuid(); var t = Guid.NewGuid();
        var g = GrupoAgrupacion.Crear(s, [a], [r, t], [new(a, r, 1, 1), new(a, t, null, 0)]);
        await cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(g, null, default); return true; }, default);
        g.Reemplazar([b], [r, t], [new(b, r, null, 0), new(b, t, 2.25m, 1.5m)]);
        await cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(g, null, default); return true; }, default);
        var reservas = await db.Set<ReservaOrigen>().AsNoTracking().ToArrayAsync(); Assert.Equal(b, Assert.Single(reservas).AprobadaId);
        var recuperado = await repo.ObtenerAsync(s, g.GrupoId, default); Assert.Equal([b], recuperado!.Grupo.Origenes);
        Assert.Null(recuperado.Grupo.Aportes.Single(x => x.AsignaturaReceptoraId == r).HorasAportadas);
        var segundo = GrupoAgrupacion.Crear(s, [a], [r, t], [new(a, r, 1, 1), new(a, t, 2, 2)]);
        await cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(segundo, null, default); return true; }, default);
        Assert.Equal(2, (await repo.ListarAsync(s, default)).Count);
    }
    [Fact]
    public async Task Reserva_origen_libera_borrador_y_rechaza_otra_unidad_sin_escritura_parcial()
    {
        using var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(c).Options);
        await db.Database.EnsureCreatedAsync(); var repo = new GruposAgrupacionSql(db); var cierre = new InformesHomologacionSql(db);
        var s = Guid.NewGuid(); var o = Guid.NewGuid(); var r = Guid.NewGuid(); var t = Guid.NewGuid();
        GrupoAgrupacion Crear() => GrupoAgrupacion.Crear(s, [o], [r, t], [new(o, r, 1, 1), new(o, t, null, 0)]);
        var g = Crear(); await cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(g, null, default); return true; }, default);
        await Assert.ThrowsAsync<ConflictoGrupoException>(() => cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(Crear(), null, default); return true; }, default));
        db.ChangeTracker.Clear(); Assert.Single(await repo.ListarAsync(s, default));
        Assert.Single(await db.Set<ReservaOrigen>().ToArrayAsync());
        await cierre.ConCierreAsync(s, async () => { await repo.EliminarAsync(g, default); return true; }, default);
        Assert.Empty(await db.Set<ReservaOrigen>().ToArrayAsync());
        await cierre.ConCierreAsync(s, async () => { await repo.GuardarAsync(Crear(), null, default); return true; }, default);
        Assert.Single(await repo.ListarAsync(s, default));
    }
}

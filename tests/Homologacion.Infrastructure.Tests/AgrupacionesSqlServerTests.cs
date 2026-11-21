using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Agrupaciones;
using Homologacion.Application.Equivalencias;
using Homologacion.Application.Informe;
using Homologacion.Application.RevisionDocente;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Domain.Agrupaciones;
using Homologacion.Infrastructure.Agrupaciones;
using Homologacion.Infrastructure.AnalisisInterno;
using Homologacion.Infrastructure.Informe;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using Xunit;
namespace Homologacion.Infrastructure.Tests;

public sealed class Hu21SqlFactAttribute : FactAttribute
{ public Hu21SqlFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HU21_TEST_SQL"))) Skip = "Requiere HU21_TEST_SQL hacia SQL Server aislado creado por hu21-smoke.py."; } }
public class AgrupacionesSqlServerTests
{
    private sealed class Fuentes(Guid solicitud, Guid origen) : ISolicitudesAnalisisInterno, IReceptorasAnalisisInterno, IResultadosAnalisisInterno, IResultadosAnalisisExterno, IRevisionesDocentes
    {
        public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct) => Task.FromResult<SolicitudParaAnalisis?>(id == solicitud ? new(id, "Interno", Guid.NewGuid(), Guid.NewGuid(), [new(origen, null, 4, 100, "Aprobada", new DateOnly(2025, 1, 1))]) : null);
        Task<IReadOnlyList<ReceptoraAnalisis>> IReceptorasAnalisisInterno.ListarAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<ReceptoraAnalisis>>([]);
        Task<IReadOnlyList<AnalisisGuardado>> IResultadosAnalisisInterno.ListarAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<AnalisisGuardado>>([]);
        Task<IReadOnlyList<AnalisisGuardado>> IResultadosAnalisisExterno.ListarAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<AnalisisGuardado>>([]);
        Task<IReadOnlyList<RevisionDocenteRegistrada>> IRevisionesDocentes.ListarAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<RevisionDocenteRegistrada>>([]);
        public Task GuardarAsync(AnalisisGuardado a, CancellationToken ct) => throw new NotSupportedException();
        public Task GuardarAsync(RevisionDocenteRegistrada r, CancellationToken ct) => throw new NotSupportedException();
    }
    [Hu21SqlFact]
    public async Task Revision_y_generacion_concurrentes_comparten_cierre_y_no_confirman_cambios_despues_del_informe()
    {
        var options = Options(); await using var init = new ResultadosDbContext(options);
        try
        {
            await init.Database.MigrateAsync(); var s = Guid.NewGuid(); var o = Guid.NewGuid(); var g = Crear(s, o); g.Congelar();
            var d1 = Guid.NewGuid(); var d2 = Guid.NewGuid(); var fuentes = new Fuentes(s, o); var repo = new GruposAgrupacionSql(init); var cierre = new InformesHomologacionSql(init);
            await cierre.ConCierreAsync(s, async () =>
            {
                await repo.GuardarAsync(g, new(s, g.GrupoId, [o], new([], 80, Homologacion.Domain.Equivalencias.EstadoEquivalencia.InformacionIncompleta, "Sintético", ["Programa"], [], []), DateTimeOffset.UtcNow), default);
                foreach (var docente in new[] { d1, d2 }) await repo.GuardarAsync(new RevisionAgrupacionRegistrada(s, g.GrupoId, docente, "Docente", "Ajuste", "NoHomologable", "Anterior", DateTimeOffset.UtcNow), default);
                return true;
            }, default);
            async Task<bool> Corregir()
            {
                await using var db = new ResultadosDbContext(options); var r = new GruposAgrupacionSql(db);
                try { await new RegistrarRevisionAgrupacion(fuentes, r, r, new InformesHomologacionSql(db), TimeProvider.System).RegistrarAsync(s, g.GrupoId, d1, "Docente", new("Ajuste", "NoHomologable", "Corregida"), default); return true; }
                catch (InformeYaGeneradoException) { return false; }
            }
            async Task<InformeHomologacion> Generar()
            { await using var db = new ResultadosDbContext(options); var r = new GruposAgrupacionSql(db); return await new GenerarInformeHomologacion(fuentes, fuentes, fuentes, fuentes, fuentes, new InformesHomologacionSql(db), TimeProvider.System, r, r).GenerarAsync(s, d2, "Docente", default); }
            var correccion = Corregir(); var generar = Generar(); await Task.WhenAll((Task)correccion, generar);
            var informe = await generar; Assert.Single(informe.Grupos); Assert.Equal(await correccion ? "Corregida" : "Anterior", informe.Grupos[0].Docentes.Single(x => x.DocenteId == d1).Observacion);
            Assert.False(await Corregir());
        }
        finally { await init.Database.EnsureDeletedAsync(); }
    }
    private static DbContextOptions<ResultadosDbContext> Options()
    {
        var c = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU21_TEST_SQL")) { InitialCatalog = "Hu21Tests_" + Guid.NewGuid().ToString("N") };
        return new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlServer(c.ConnectionString).Options;
    }
    private static GrupoAgrupacion Crear(Guid s, Guid o)
    { var r = Guid.NewGuid(); var t = Guid.NewGuid(); return GrupoAgrupacion.Crear(s, [o], [r, t], [new(o, r, 10, 1), new(o, t, 20, 2)]); }
    private sealed class Umbral : IUmbralEquivalencia { public decimal Valor => 80; }
    private static AnalisisGuardado Pareja(Guid s, Guid o, Guid r) => new(DateTimeOffset.UtcNow, new(s, o, r, "Incompleto", "Sintético", null,
        new EvaluarEquivalencia(new Umbral()).Ejecutar(OllamaRealTests.DatosSinteticos()), null, true, []));
    [Hu21SqlFact]
    public async Task Migracion_de_develop_conserva_parejas_repetidas_e_informe_historico_sin_inventar_grupos()
    {
        var options = Options(); await using var db = new ResultadosDbContext(options); var s = Guid.NewGuid(); var o = Guid.NewGuid();
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261121000200_AuditoriaConfiguracionHu03");
            // Inserción de historia representativa previa a HU-21, sin utilizar el nuevo reservador.
            foreach (var r in new[] { Guid.NewGuid(), Guid.NewGuid() }) db.Resultados.Add(new() { SolicitudId = s, AprobadaId = o, ReceptoraId = r, Fecha = DateTimeOffset.UtcNow, Contenido = System.Text.Json.JsonSerializer.Serialize(Pareja(s, o, r).Analisis, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)) });
            db.Informes.Add(new() { SolicitudId = s, GeneradoPorId = Guid.NewGuid(), Fecha = DateTimeOffset.UtcNow, Contenido = $"{{\"solicitudId\":\"{s}\",\"tipoCambio\":\"Interno\",\"parejas\":[],\"aprobadasSinAnalisis\":[],\"advertencias\":[]}}" });
            await db.SaveChangesAsync(); await db.Database.MigrateAsync();
            Assert.Equal(2, await db.Resultados.CountAsync()); Assert.Empty(await db.Set<RegistroGrupo>().ToArrayAsync()); Assert.Empty(await db.Set<ReservaOrigen>().ToArrayAsync());
            var informe = await new InformesHomologacionSql(db).ObtenerAsync(s, default); Assert.Empty(informe!.Grupos);
            var repo = new GruposAgrupacionSql(db); await Assert.ThrowsAsync<ConflictoGrupoException>(() => new InformesHomologacionSql(db).ConCierreAsync(s, async () => { await repo.GuardarAsync(Crear(s, o), null, default); return true; }, default));
            Assert.Equal(2, await db.Resultados.CountAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
    [Hu21SqlFact]
    public async Task Dos_contextos_compiten_por_un_origen_solo_uno_reserva_sin_grupos_parciales()
    {
        var options = Options(); await using var init = new ResultadosDbContext(options);
        try
        {
            await init.Database.MigrateAsync(); var s = Guid.NewGuid(); var o = Guid.NewGuid();
            async Task<bool> Reservar()
            { await using var db = new ResultadosDbContext(options); try { return await new InformesHomologacionSql(db).ConCierreAsync(s, async () => { await new GruposAgrupacionSql(db).GuardarAsync(Crear(s, o), null, default); return true; }, default); } catch (ConflictoGrupoException) { return false; } }
            var r = await Task.WhenAll(Reservar(), Reservar()); Assert.Single(r.Where(x => x));
            Assert.Equal(1, await init.Set<RegistroGrupo>().CountAsync()); Assert.Equal(1, await init.Set<ReservaOrigen>().CountAsync()); Assert.Equal(2, await init.Set<RegistroAporteGrupo>().CountAsync());
        }
        finally { await init.Database.EnsureDeletedAsync(); }
    }
    [Hu21SqlFact]
    public async Task Pareja_y_grupo_concurrentes_no_comparten_origen_y_rollback_libera_todo()
    {
        var options = Options(); await using var init = new ResultadosDbContext(options);
        try
        {
            await init.Database.MigrateAsync(); var s = Guid.NewGuid(); var o = Guid.NewGuid();
            async Task<bool> Escribir(bool grupo)
            {
                await using var db = new ResultadosDbContext(options); try
                {
                    if (grupo) await new InformesHomologacionSql(db).ConCierreAsync(s, async () => { await new GruposAgrupacionSql(db).GuardarAsync(Crear(s, o), null, default); return true; }, default);
                    else await new ResultadosAnalisisSql(db).GuardarAsync(Pareja(s, o, Guid.NewGuid()), default); return true;
                }
                catch (ConflictoGrupoException) { return false; }
            }
            var r = await Task.WhenAll(Escribir(true), Escribir(false)); Assert.Single(r.Where(x => x)); Assert.Equal(1, await init.Set<ReservaOrigen>().CountAsync());
            var otro = Guid.NewGuid(); await using (var db = new ResultadosDbContext(options))
            { await Assert.ThrowsAsync<InvalidOperationException>(() => new InformesHomologacionSql(db).ConCierreAsync(s, async () => { await new GruposAgrupacionSql(db).GuardarAsync(Crear(s, otro), null, default); return await Task.FromException<bool>(new InvalidOperationException("Fallo sintético después del guardado")); }, default)); }
            Assert.False(await init.Set<ReservaOrigen>().AnyAsync(x => x.AprobadaId == otro));
            Assert.Equal(1, await init.Set<RegistroGrupo>().CountAsync() + await init.Resultados.CountAsync());
        }
        finally { await init.Database.EnsureDeletedAsync(); }
    }
}

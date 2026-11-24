using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Infrastructure.AnalisisInterno;
using Homologacion.Infrastructure.Configuracion;
using Homologacion.Application.Configuracion;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HU20_TEST_SQL")))
            Skip = "Requiere HU20_TEST_SQL con un SQL Server aislado; no usa bases del equipo.";
    }
}

/// <summary>HU-20 PA1–PA4 sobre SQL Server real: migración, durabilidad, aislamiento y primera inserción concurrente.</summary>
public class ResultadosAnalisisSqlServerTests
{
    [SqlServerFact]
    public Task HU07_SQL_conserva_mallas_de_parejas_grupos_informe_y_lee_historicos() => EnBaseTemporal(async options =>
    {
        var s = Guid.NewGuid(); var a = Guid.NewGuid(); var r = Guid.NewGuid(); var ag = Guid.NewGuid(); var rg = Guid.NewGuid(); var rg2 = Guid.NewGuid();
        var malla = new InstantaneaMalla(Guid.NewGuid(), Guid.NewGuid(), "Plan guardado en SQL", false);
        var mallas = new MallasAnalisis([new(Guid.NewGuid(), a, malla)], [new(r, null, malla with { VigenteAlAnalizar = true })]);
        var pareja = Resultado(s, a, r, "Completo", DateTimeOffset.UtcNow);
        pareja = pareja with { Analisis = pareja.Analisis with { Mallas = mallas } };
        var grupo = Homologacion.Domain.Agrupaciones.GrupoAgrupacion.Crear(s, [ag], [rg, rg2], [new(ag, rg, 1, 1), new(ag, rg2, 1, 1)]);
        grupo.Congelar();
        var mg = new MallasAnalisis([new(Guid.NewGuid(), ag, malla)], [new(rg, null, malla), new(rg2, null, malla)]);
        var evaluacion = new ResultadoAgrupacion([pareja.Analisis.Evaluacion], 80,
            Homologacion.Domain.Equivalencias.EstadoEquivalencia.Homologable, "Sin cambios", [], [], []);
        var analisisGrupo = new Homologacion.Application.Informe.AnalisisAgrupacionGuardado(s, grupo.GrupoId, [ag], evaluacion, DateTimeOffset.UtcNow) { Mallas = mg };
        var informe = new Homologacion.Application.Informe.InformeHomologacion(s, "Interno", DateTimeOffset.UtcNow, Guid.NewGuid(), "Docente", [
            new(a, r, "R", "Receptora", new("Completo", "Homologable", 100, 80, 100), [], "Convalidar") { Mallas = mallas }], [], 1, 0, 0, [])
        { Grupos = [new(grupo.GrupoId, [ag], evaluacion.Receptoras, "Homologable", "Homologable", [], "Convalidar", [], [], "Agrupación") { Mallas = mg }] };
        var historico = Resultado(Guid.NewGuid(), Guid.NewGuid(), r, "Completo", DateTimeOffset.UtcNow);
        var externo = Resultado(Guid.NewGuid(), Guid.NewGuid(), r, "Incompleto", DateTimeOffset.UtcNow);
        externo = externo with { Analisis = externo.Analisis with { Mallas = new([new(null, externo.Analisis.AsignaturaAprobadaId, null)], mallas.Receptoras) } };
        var json = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        await using (var db = new ResultadosDbContext(options))
        {
            await new ResultadosAnalisisSql(db).GuardarAsync(pareja, default);
            await new Homologacion.Infrastructure.AnalisisExterno.ResultadosAnalisisExternoSql(db).GuardarAsync(externo, default);
            await new ResultadosAnalisisSql(db).GuardarAsync(historico, default);
            // AlmacenPorPareja guarda el resultado directamente; la fecha está en su propia columna.
            var viejo = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(historico.Analisis, json))!;
            viejo.AsObject().Remove("mallas");
            var contenidoHistorico = viejo.ToJsonString();
            await db.Resultados.Where(x => x.SolicitudId == historico.Analisis.SolicitudId).ExecuteUpdateAsync(set => set.SetProperty(x => x.Contenido, contenidoHistorico));
            var cierre = new Homologacion.Infrastructure.Informe.InformesHomologacionSql(db);
            await cierre.ConCierreAsync(s, async () => { await new Homologacion.Infrastructure.Agrupaciones.GruposAgrupacionSql(db).GuardarAsync(grupo, analisisGrupo, default); return true; }, default);
            await cierre.GuardarAsync(informe, default);
        }
        await using var lectura = new ResultadosDbContext(options);
        var leida = Assert.Single(await new ResultadosAnalisisSql(lectura).ListarAsync(s, default));
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(mallas, json), System.Text.Json.JsonSerializer.Serialize(leida.Analisis.Mallas, json));
        Assert.False(leida.Analisis.Mallas!.Origenes[0].Malla!.VigenteAlAnalizar);
        var agrupada = await new Homologacion.Infrastructure.Agrupaciones.GruposAgrupacionSql(lectura).ObtenerAsync(s, grupo.GrupoId, default);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(mg, json), System.Text.Json.JsonSerializer.Serialize(agrupada!.UltimoAnalisis!.Mallas, json));
        var fijo = await new Homologacion.Infrastructure.Informe.InformesHomologacionSql(lectura).ObtenerAsync(s, default);
        Assert.Equal(malla, fijo!.Parejas[0].Mallas!.Origenes[0].Malla);
        Assert.Equal(malla, fijo.Grupos[0].Mallas!.Origenes[0].Malla);
        Assert.Null(Assert.Single(await new ResultadosAnalisisSql(lectura).ListarAsync(historico.Analisis.SolicitudId, default)).Analisis.Mallas);
        var guardadoExterno = Assert.Single(await new Homologacion.Infrastructure.AnalisisExterno.ResultadosAnalisisExternoSql(lectura).ListarAsync(externo.Analisis.SolicitudId, default));
        Assert.Null(guardadoExterno.Analisis.Mallas!.Origenes[0].Malla);
        return true;
    });

    [SqlServerFact]
    public Task Configuracion_hu03_actualiza_atomica_y_audita_en_sql_server() => EnBaseTemporal(async options =>
    {
        await using var db = new ResultadosDbContext(options);
        var almacen = new ParametrosHomologacionSql(db);
        var gestion = new GestionarConfiguracionHomologacion(almacen, TimeProvider.System);
        var inicial = await gestion.ObtenerAsync(default);
        Assert.Equal(80, inicial.UmbralPct);
        var actor = Guid.NewGuid();
        var nuevo = await gestion.ActualizarAsync(inicial.VersionId,
            new(85, 12, 2, 2, 3, 3, "Acta", "Expediente"), actor, default);
        Assert.Equal(nuevo.VersionId, (await gestion.ObtenerAsync(default)).VersionId);
        await Assert.ThrowsAsync<ConfiguracionObsoletaException>(() => gestion.ActualizarAsync(inicial.VersionId,
            new(90, 10, 1, 1, 2, 2, "Otro", "Otro"), actor, default));
        Assert.Equal(2, await db.Configuraciones.CountAsync());
        var fila = await db.Configuraciones.SingleAsync(x => x.VersionId == nuevo.VersionId);
        Assert.Equal(inicial.VersionId, fila.VersionAnteriorId);
        Assert.Equal(actor, fila.ActorId);
        Assert.NotEmpty(fila.RowVersion);
        var pagina = await gestion.HistorialAsync(null, 1, default);
        var entrada = Assert.Single(pagina.Entradas);
        Assert.Equal(80, entrada.Anterior!.UmbralPct);
        Assert.Equal(85, entrada.Nueva.UmbralPct);
        Assert.Equal(actor, entrada.ActorId);
        Assert.NotNull(pagina.CursorSiguiente);
        Assert.Single((await gestion.HistorialAsync(pagina.CursorSiguiente, 1, default)).Entradas);
        return true;
    });

    [SqlServerFact]
    public Task Dos_actualizaciones_simultaneas_no_crean_dos_versiones_vigentes() => EnBaseTemporal(async options =>
    {
        Guid inicial;
        await using (var db = new ResultadosDbContext(options))
            inicial = (await new ParametrosHomologacionSql(db).ObtenerVigentesAsync(default)).VersionId;
        async Task<Exception?> Intentar(int umbral)
        {
            await using var db = new ResultadosDbContext(options);
            var gestion = new GestionarConfiguracionHomologacion(new ParametrosHomologacionSql(db), TimeProvider.System);
            try
            {
                await gestion.ActualizarAsync(inicial, new(umbral, 10, 1, 1, 2, 2, "Acta", "Expediente"), Guid.NewGuid(), default);
                return null;
            }
            catch (Exception error) { return error; }
        }
        var errores = await Task.WhenAll(Task.Run(() => Intentar(85)), Task.Run(() => Intentar(90)));
        Assert.Single(errores, error => error is null);
        Assert.Single(errores, error => error is ConfiguracionObsoletaException);
        await using var lectura = new ResultadosDbContext(options);
        Assert.Equal(2, await lectura.Configuraciones.CountAsync());
        Assert.Equal(1, await lectura.ConfiguracionVigente.CountAsync());
        return true;
    });

    private sealed class Umbral : IUmbralEquivalencia { public decimal Valor => 80; }

    private static AnalisisGuardado Resultado(Guid solicitud, Guid aprobada, Guid receptora, string estado, DateTimeOffset fecha)
    {
        var resumen = estado == "Completo"
            ? new ResumenAnalisisInterno(100, 80, 100, "Homologable", "Justificación sintética.", ["plan-sintetico-O/C ↔ plan-sintetico-D/C"])
            : null;
        return new(fecha, new(solicitud, aprobada, receptora, estado, "Mensaje sintético", resumen,
            new EvaluarEquivalencia(new Umbral()).Ejecutar(OllamaRealTests.DatosSinteticos()), null, true, ["No es una aprobación oficial."]));
    }

    private static async Task<T> EnBaseTemporal<T>(Func<DbContextOptions<ResultadosDbContext>, Task<T>> prueba)
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU20_TEST_SQL"))
        { InitialCatalog = "Hu20Tests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var admin = new ResultadosDbContext(options);
        try
        {
            await admin.Database.MigrateAsync();
            return await prueba(options);
        }
        finally
        {
            if (connection.InitialCatalog.StartsWith("Hu20Tests_", StringComparison.Ordinal)) await admin.Database.EnsureDeletedAsync();
        }
    }

    [SqlServerFact]
    public Task Migracion_guarda_y_recupera_por_solicitud_con_conexiones_nuevas() => EnBaseTemporal(async options =>
    {
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());
        var (aprobada, receptora) = (Guid.NewGuid(), Guid.NewGuid());
        var antes = new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.FromHours(-5));
        await using (var db = new ResultadosDbContext(options))
        {
            var repo = new ResultadosAnalisisSql(db);
            await repo.GuardarAsync(Resultado(a, aprobada, receptora, "Completo", antes), default);
            // HU-21: unidades nuevas usan orígenes diferentes; el legado repetido se verifica en AgrupacionesSqlServerTests.
            await repo.GuardarAsync(Resultado(a, Guid.NewGuid(), Guid.NewGuid(), "Incompleto", antes.AddMinutes(5)), default);
            await repo.GuardarAsync(Resultado(b, aprobada, receptora, "Fallido", antes), default);
        }
        // Contexto y conexión nuevos: lo leído proviene de SQL Server, no del seguimiento de EF.
        await using var lectura = new ResultadosDbContext(options);
        var repoLectura = new ResultadosAnalisisSql(lectura);
        var deA = await repoLectura.ListarAsync(a, default);
        Assert.Equal(["Incompleto", "Completo"], deA.Select(x => x.Analisis.Estado));
        Assert.All(deA, x => Assert.Equal(a, x.Analisis.SolicitudId));
        var completo = deA[1];
        Assert.Equal(antes, completo.Fecha);
        Assert.Equal(100, completo.Analisis.Resultado!.PorcentajeAcademico);
        Assert.Equal(80, completo.Analisis.Resultado.PorcentajeHoras);
        Assert.Equal(100, completo.Analisis.Resultado.PorcentajeCreditos);
        Assert.Equal("Homologable", completo.Analisis.Resultado.Sugerencia);
        Assert.Equal("Justificación sintética.", completo.Analisis.Resultado.Justificacion);
        Assert.Equal(["plan-sintetico-O/C ↔ plan-sintetico-D/C"], completo.Analisis.Resultado.Evidencias);
        Assert.Null(deA[0].Analisis.Resultado);
        var deB = Assert.Single(await repoLectura.ListarAsync(b, default));
        Assert.Equal("Fallido", deB.Analisis.Estado);
        Assert.Null(deB.Analisis.Resultado);
        Assert.Empty(await repoLectura.ListarAsync(Guid.NewGuid(), default));
        return true;
    });

    [SqlServerFact]
    public Task Reanalisis_reemplaza_la_pareja_sin_duplicar() => EnBaseTemporal(async options =>
    {
        var r = Resultado(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Completo", DateTimeOffset.UtcNow);
        await using (var db = new ResultadosDbContext(options)) await new ResultadosAnalisisSql(db).GuardarAsync(r, default);
        await using (var db = new ResultadosDbContext(options))
            await new ResultadosAnalisisSql(db).GuardarAsync(r with { Fecha = r.Fecha.AddMinutes(1), Analisis = r.Analisis with { Estado = "Fallido", Resultado = null } }, default);
        await using var lectura = new ResultadosDbContext(options);
        var guardado = Assert.Single(await new ResultadosAnalisisSql(lectura).ListarAsync(r.Analisis.SolicitudId, default));
        Assert.Equal("Fallido", guardado.Analisis.Estado);
        Assert.Null(guardado.Analisis.Resultado);
        Assert.Equal(1, await lectura.Resultados.CountAsync());
        return true;
    });

    [SqlServerFact]
    public Task Primeras_inserciones_concurrentes_dejan_una_fila_y_fallan_de_forma_controlada() => EnBaseTemporal(async options =>
    {
        var r = Resultado(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Incompleto", DateTimeOffset.UtcNow);
        // Varias rondas para aumentar la probabilidad de que dos peticiones vean la pareja ausente a la vez.
        for (var ronda = 0; ronda < 5; ronda++)
        {
            var pareja = r with { Analisis = r.Analisis with { AsignaturaAprobadaId = Guid.NewGuid(), AsignaturaReceptoraId = Guid.NewGuid() } };
            async Task<Exception?> Guardar()
            {
                await using var db = new ResultadosDbContext(options);
                try { await new ResultadosAnalisisSql(db).GuardarAsync(pareja, default); return null; }
                catch (Exception error) { return error; }
            }
            var errores = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(Guardar)));
            // Quien pierde la carrera recibe el error controlado (503 en la API), nunca una excepción SQL.
            Assert.All(errores.OfType<Exception>(), e => Assert.IsType<AlmacenAnalisisNoDisponibleException>(e));
            Assert.Contains(errores, e => e is null);
            await using var lectura = new ResultadosDbContext(options);
            Assert.Equal(1, await lectura.Resultados.CountAsync(x => x.ReceptoraId == pareja.Analisis.AsignaturaReceptoraId));
        }
        return true;
    });
}

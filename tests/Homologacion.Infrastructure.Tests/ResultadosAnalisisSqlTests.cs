using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Infrastructure.AnalisisInterno;
using Homologacion.Infrastructure.Configuracion;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

public class ResultadosAnalisisSqlTests
{
    [Fact]
    public void Migracion_y_snapshot_corresponden_al_modelo_sql_server()
    {
        using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>()
            .UseSqlServer("Server=unused;Database=design_only;Integrated Security=true").Options);
        Assert.Contains("20261104000100_AnalisisInternosHu20", db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }
    [Fact]
    public void Migracion_hu03_genera_sql_con_semilla_y_referencia_vigente()
    {
        using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>()
            .UseSqlServer("Server=unused;Database=design_only;Integrated Security=true").Options);
        var sql = db.GetService<IMigrator>().GenerateScript();
        Assert.Contains("CREATE TABLE [ConfiguracionesHomologacion]", sql);
        Assert.Contains("CREATE TABLE [ConfiguracionHomologacionVigente]", sql);
        Assert.Contains("FK_ConfiguracionHomologacionVigente_ConfiguracionesHomologacion_VersionId", sql);
        Assert.Contains("9e0ce28d-e93a-4cad-b650-48ad9b260301", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'Configuración predeterminada inicial de HU-03'", sql);
        Assert.Contains("VersionAnteriorId", sql);
        Assert.Contains("FK_ConfiguracionesHomologacion_ConfiguracionesHomologacion_VersionAnteriorId", sql);
    }

    [Fact]
    public async Task Proveedor_falla_si_no_existe_configuracion_vigente()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new ParametrosHomologacionSql(db).ObtenerVigentesAsync(default));
    }
    private static AnalisisGuardado Resultado(string estado = "Incompleto") => new(DateTimeOffset.UtcNow,
        new(Guid.Parse("30000000-0000-0000-0000-000000000001"), Guid.Parse("30000000-0000-0000-0000-000000000002"), Guid.Parse("30000000-0000-0000-0000-000000000003"),
            estado, "Mensaje sintetico", null, new EvaluarEquivalencia(new Umbral()).Ejecutar(OllamaRealTests.DatosSinteticos()), null, true, ["No es una aprobación oficial."]));
    private sealed class Umbral : IUmbralEquivalencia { public decimal Valor => 80; }
    [Fact]
    public async Task Conserva_estado_y_aislamiento_tras_reabrir_contexto_sin_duplicar_pareja()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options;
        var r = Resultado();
        await using (var db = new ResultadosDbContext(options))
        {
            await db.Database.EnsureCreatedAsync(); var repo = new ResultadosAnalisisSql(db);
            await repo.GuardarAsync(r, default);
            await repo.GuardarAsync(r with { Analisis = r.Analisis with { Estado = "Fallido" } }, default);
        }
        await using var reopened = new ResultadosDbContext(options);
        var saved = Assert.Single(await new ResultadosAnalisisSql(reopened).ListarAsync(r.Analisis.SolicitudId, default));
        Assert.Equal("Fallido", saved.Analisis.Estado); Assert.Null(saved.Analisis.Resultado);
        Assert.Equal(r.Analisis.AsignaturaAprobadaId, saved.Analisis.AsignaturaAprobadaId);
        Assert.Empty(await new ResultadosAnalisisSql(reopened).ListarAsync(Guid.NewGuid(), default));
        Assert.Equal(1, await reopened.Resultados.CountAsync());
    }
    [Fact]
    public async Task Cambio_de_configuracion_vigente_no_modifica_resultado_guardado()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options;
        var version = Guid.NewGuid();
        var original = Resultado();
        var resultado = original with { Analisis = original.Analisis with
            { Evaluacion = original.Analisis.Evaluacion with { ConfiguracionVersionId = version } } };
        await using (var db = new ResultadosDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            await new ResultadosAnalisisSql(db).GuardarAsync(resultado, default);
            var contenido = Assert.Single(await db.Resultados.ToListAsync()).Contenido;
            Assert.Contains(version.ToString(), contenido);
            await InsertarConfiguracion(db, version);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ConfiguracionHomologacionVigente (Id, VersionId) VALUES (1, {version})");
            Assert.Equal(version, (await new ParametrosHomologacionSql(db).ObtenerVigentesAsync(default)).VersionId);
            var nuevaVersion = Guid.NewGuid();
            await InsertarConfiguracion(db, nuevaVersion);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ConfiguracionHomologacionVigente SET VersionId = {nuevaVersion} WHERE Id = 1");
            Assert.Equal(nuevaVersion, (await new ParametrosHomologacionSql(db).ObtenerVigentesAsync(default)).VersionId);
        }
        await using (var db = new ResultadosDbContext(options))
        {
            var guardado = Assert.Single(await new ResultadosAnalisisSql(db).ListarAsync(resultado.Analisis.SolicitudId, default));
            Assert.Equal(version, guardado.Analisis.Evaluacion.ConfiguracionVersionId);
            Assert.Equal(resultado.Analisis.Evaluacion.UmbralPct, guardado.Analisis.Evaluacion.UmbralPct);
        }
    }

    private static Task<int> InsertarConfiguracion(ResultadosDbContext db, Guid version) => db.Database.ExecuteSqlInterpolatedAsync(
        $"INSERT INTO ConfiguracionesHomologacion (VersionId, UmbralPct, AntiguedadMaximaAnios, PeriodosMinimosInterno, AsignaturasHomologablesMinimasInterno, PeriodosMinimosExterno, AsignaturasHomologablesMinimasExterno, ActorId, FechaHoraUtc, Motivo, Respaldo, RowVersion) VALUES ({version}, 80, 10, 1, 1, 2, 2, {Guid.Empty}, '2026-01-01T00:00:00Z', 'prueba', 'prueba', x'00')");
    [Fact]
    public async Task Base_no_disponible_no_anuncia_guardado_ni_filtra_error_sql()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options);
        var error = await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new ResultadosAnalisisSql(db).GuardarAsync(Resultado(), default));
        Assert.DoesNotContain("Sqlite", error.Message);
    }
}

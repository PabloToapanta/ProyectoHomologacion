using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
    public async Task Base_no_disponible_no_anuncia_guardado_ni_filtra_error_sql()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options);
        var error = await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new ResultadosAnalisisSql(db).GuardarAsync(Resultado(), default));
        Assert.DoesNotContain("Sqlite", error.Message);
    }
}

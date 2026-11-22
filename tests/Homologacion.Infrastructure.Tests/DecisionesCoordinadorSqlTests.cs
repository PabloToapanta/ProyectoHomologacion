using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Infrastructure.AnalisisInterno;
using Homologacion.Infrastructure.Informe;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

/// <summary>HU-33: tabla DecisionesCoordinador, una decisión por pareja «Sin acuerdo».</summary>
public class DecisionesCoordinadorSqlTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.FromHours(-5));
    private static DecisionCoordinador Decision(Guid solicitud, Guid aprobada, Guid receptora, string resultado, string justificacion, DateTimeOffset fecha) =>
        new(solicitud, aprobada, receptora, resultado, justificacion, Guid.NewGuid(), "coordinadora.sintetica", fecha);

    private static async Task<DbContextOptions<ResultadosDbContext>> Sqlite(SqliteConnection connection)
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options;
        await using var db = new ResultadosDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return options;
    }

    [Fact]
    public void Migracion_y_snapshot_incluyen_decisiones()
    {
        using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>()
            .UseSqlServer("Server=unused;Database=design_only;Integrated Security=true").Options);
        Assert.Contains("20261123000100_DecisionesCoordinadorHu33", db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Guarda_lista_y_reemplaza_la_decision_de_la_misma_pareja()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = await Sqlite(connection);
        var (solicitud, otra, aprobada, receptora) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using (var db = new ResultadosDbContext(options))
        {
            var decisiones = new DecisionesCoordinadorSql(db);
            await decisiones.GuardarAsync(Decision(solicitud, aprobada, receptora, "NoConvalidar", "Primera.", T0), default);
            await decisiones.GuardarAsync(Decision(solicitud, aprobada, receptora, "Convalidar", "Corregida.", T0.AddMinutes(5)), default);
            await decisiones.GuardarAsync(Decision(otra, aprobada, receptora, "NoConvalidar", "Otra solicitud.", T0), default);
        }
        await using var lectura = new ResultadosDbContext(options);
        var guardada = Assert.Single(await new DecisionesCoordinadorSql(lectura).ListarAsync(solicitud, default));
        Assert.Equal(("Convalidar", "Corregida.", T0.AddMinutes(5), "coordinadora.sintetica"),
            (guardada.Resultado, guardada.Justificacion, guardada.Fecha, guardada.Coordinador));
    }

    [Fact]
    public async Task Si_falla_la_accion_del_cierre_la_decision_no_se_confirma()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = await Sqlite(connection);
        var solicitud = Guid.NewGuid();
        await using (var db = new ResultadosDbContext(options))
        {
            await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => new InformesHomologacionSql(db).ConCierreAsync<bool>(solicitud, async () =>
            {
                await new DecisionesCoordinadorSql(db).GuardarAsync(Decision(solicitud, Guid.NewGuid(), Guid.NewGuid(), "Convalidar", "Motivo.", T0), default);
                throw new FuenteAnalisisNoDisponibleException();
            }, default));
        }
        await using var lectura = new ResultadosDbContext(options);
        Assert.Empty(await new DecisionesCoordinadorSql(lectura).ListarAsync(solicitud, default));
    }

    [SqlServerExternoFact]
    public async Task SQL_Server_real_aplica_la_migracion_y_reemplaza_la_decision()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU20_TEST_SQL"))
            { InitialCatalog = "Hu33Tests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var admin = new ResultadosDbContext(options);
        try
        {
            await admin.Database.MigrateAsync();
            var (solicitud, aprobada, receptora) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await new DecisionesCoordinadorSql(admin).GuardarAsync(Decision(solicitud, aprobada, receptora, "NoConvalidar", "Primera.", T0), default);
            await using var segunda = new ResultadosDbContext(options);
            await new DecisionesCoordinadorSql(segunda).GuardarAsync(Decision(solicitud, aprobada, receptora, "Convalidar", new string('j', 2000), T0), default);
            await using var lectura = new ResultadosDbContext(options);
            var guardada = Assert.Single(await new DecisionesCoordinadorSql(lectura).ListarAsync(solicitud, default));
            Assert.Equal(("Convalidar", 2000), (guardada.Resultado, guardada.Justificacion.Length));
        }
        finally { await admin.Database.EnsureDeletedAsync(); }
    }
}

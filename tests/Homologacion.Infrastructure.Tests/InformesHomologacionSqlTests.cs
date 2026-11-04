using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Infrastructure.AnalisisInterno;
using Homologacion.Infrastructure.Informe;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

/// <summary>HU-23: tabla InformesHomologacion, un informe fijo por solicitud.</summary>
public class InformesHomologacionSqlTests
{
    private static InformeHomologacion Informe(Guid solicitud) => new(solicitud, "Interno", new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.FromHours(-5)),
        Guid.NewGuid(), "docente.sintetico",
        [new(Guid.NewGuid(), Guid.NewGuid(), "R-01", "Receptora sintética", new("Completo", "Homologable", 90, 85, 100),
            [new(Guid.NewGuid(), "docente.ana", "DeAcuerdo", "Homologable", null, DateTimeOffset.UnixEpoch),
             new(Guid.NewGuid(), "docente.luis", "Ajuste", "NoHomologable", "Discrepo.", DateTimeOffset.UnixEpoch)], "SinAcuerdo")],
        [Guid.NewGuid()], 0, 0, 1, ["No es una resolución oficial."]);

    [Fact]
    public void Migracion_y_snapshot_incluyen_informes()
    {
        using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>()
            .UseSqlServer("Server=unused;Database=design_only;Integrated Security=true").Options);
        Assert.Contains("20261104000400_InformesHomologacionHu23", db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Guarda_una_vez_y_conserva_el_contenido_completo()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options;
        var informe = Informe(Guid.NewGuid());
        await using (var db = new ResultadosDbContext(options)) { await db.Database.EnsureCreatedAsync(); await new InformesHomologacionSql(db).GuardarAsync(informe, default); }
        await using (var db = new ResultadosDbContext(options))
            await Assert.ThrowsAsync<InformeYaGeneradoException>(() => new InformesHomologacionSql(db).GuardarAsync(Informe(informe.SolicitudId), default));
        await using var lectura = new ResultadosDbContext(options);
        var leido = (await new InformesHomologacionSql(lectura).ObtenerAsync(informe.SolicitudId, default))!;
        Assert.Equal(informe.GeneradoPorId, leido.GeneradoPorId);
        Assert.Equal(informe.Fecha, leido.Fecha);
        Assert.Equal("SinAcuerdo", leido.Parejas[0].SugerenciaFinal);
        Assert.Equal(["Homologable", "NoHomologable"], leido.Parejas[0].Docentes.Select(d => d.Decision));
        Assert.Equal(90, leido.Parejas[0].Agente.PorcentajeAcademico);
        Assert.Null(await new InformesHomologacionSql(lectura).ObtenerAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Base_no_disponible_no_confirma()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options);
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new InformesHomologacionSql(db).GuardarAsync(Informe(Guid.NewGuid()), default));
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new InformesHomologacionSql(db).ObtenerAsync(Guid.NewGuid(), default));
    }

    [SqlServerExternoFact]
    public async Task SQL_Server_real_genera_un_solo_informe_aunque_sea_simultaneo()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU20_TEST_SQL"))
            { InitialCatalog = "Hu23Tests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var admin = new ResultadosDbContext(options);
        try
        {
            await admin.Database.MigrateAsync();
            var solicitud = Guid.NewGuid();
            async Task<Exception?> Guardar()
            {
                await using var db = new ResultadosDbContext(options);
                try { await new InformesHomologacionSql(db).GuardarAsync(Informe(solicitud), default); return null; }
                catch (Exception e) { return e; }
            }
            var errores = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(Guardar)));
            Assert.Single(errores, e => e is null);
            Assert.All(errores.OfType<Exception>(), e => Assert.IsType<InformeYaGeneradoException>(e));
            await using var lectura = new ResultadosDbContext(options);
            Assert.Equal(1, await lectura.Informes.CountAsync());
        }
        finally
        {
            if (connection.InitialCatalog.StartsWith("Hu23Tests_", StringComparison.Ordinal)) await admin.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task El_cierre_confirma_lo_escrito_dentro_y_descarta_si_falla()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options;
        await using (var init = new ResultadosDbContext(options)) await init.Database.EnsureCreatedAsync();
        var bien = Informe(Guid.NewGuid()); var mal = Informe(Guid.NewGuid());
        await using (var db = new ResultadosDbContext(options))
        {
            var almacen = new InformesHomologacionSql(db);
            Assert.Equal(1, await almacen.ConCierreAsync(bien.SolicitudId, async () => { await almacen.GuardarAsync(bien, default); return 1; }, default));
            await Assert.ThrowsAsync<InvalidOperationException>(() => almacen.ConCierreAsync<int>(mal.SolicitudId, async () =>
            { await almacen.GuardarAsync(mal, default); throw new InvalidOperationException("fallo posterior"); }, default));
        }
        await using var lectura = new ResultadosDbContext(options);
        Assert.NotNull(await new InformesHomologacionSql(lectura).ObtenerAsync(bien.SolicitudId, default));
        Assert.Null(await new InformesHomologacionSql(lectura).ObtenerAsync(mal.SolicitudId, default));
    }

    [SqlServerExternoFact]
    public async Task SQL_Server_real_serializa_el_cierre_por_solicitud()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU20_TEST_SQL"))
            { InitialCatalog = "Hu23Tests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var admin = new ResultadosDbContext(options);
        try
        {
            await admin.Database.MigrateAsync();
            var solicitud = Guid.NewGuid();
            var dentro = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var soltar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var dbA = new ResultadosDbContext(options);
            await using var dbB = new ResultadosDbContext(options);
            await using var dbC = new ResultadosDbContext(options);
            var primero = new InformesHomologacionSql(dbA).ConCierreAsync(solicitud, async () => { dentro.SetResult(); await soltar.Task; return 1; }, default);
            await dentro.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var mismaSolicitud = new InformesHomologacionSql(dbB).ConCierreAsync(solicitud, () => Task.FromResult(2), default);
            var otraSolicitud = new InformesHomologacionSql(dbC).ConCierreAsync(Guid.NewGuid(), () => Task.FromResult(3), default);
            Assert.Equal(3, await otraSolicitud.WaitAsync(TimeSpan.FromSeconds(10)));
            await Task.Delay(500);
            Assert.False(mismaSolicitud.IsCompleted, "La misma solicitud debe esperar a que termine el cierre en curso.");
            soltar.SetResult();
            Assert.Equal(1, await primero);
            Assert.Equal(2, await mismaSolicitud.WaitAsync(TimeSpan.FromSeconds(10)));
        }
        finally
        {
            if (connection.InitialCatalog.StartsWith("Hu23Tests_", StringComparison.Ordinal)) await admin.Database.EnsureDeletedAsync();
        }
    }
}

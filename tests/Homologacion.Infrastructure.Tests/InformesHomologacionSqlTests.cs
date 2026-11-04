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
}

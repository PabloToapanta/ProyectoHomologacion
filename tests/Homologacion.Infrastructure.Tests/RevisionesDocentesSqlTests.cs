using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.RevisionDocente;
using Homologacion.Infrastructure.AnalisisInterno;
using Homologacion.Infrastructure.RevisionDocente;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

/// <summary>HU-31: tabla RevisionesDocentes en la base de Homologación.</summary>
public class RevisionesDocentesSqlTests
{
    private static RevisionDocenteRegistrada Revision(Guid solicitud, Guid docente, string decision = "Homologable", string? observacion = "Observación sintética.") =>
        new(solicitud, Guid.Parse("70000000-0000-0000-0000-000000000002"), Guid.Parse("70000000-0000-0000-0000-000000000003"), docente, "docente.sintetico",
            "Ajuste", decision, observacion, "Incompleto", null, new DateTimeOffset(2026, 11, 4, 13, 0, 0, TimeSpan.FromHours(-5)));

    [Fact]
    public void Migracion_y_snapshot_incluyen_revisiones_docentes()
    {
        using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>()
            .UseSqlServer("Server=unused;Database=design_only;Integrated Security=true").Options);
        Assert.Contains("20261104000300_RevisionesDocentesHu31", db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Guarda_por_docente_y_reemplaza_solo_el_propio()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options;
        var (solicitud, ana, luis) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using (var db = new ResultadosDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var repo = new RevisionesDocentesSql(db);
            await repo.GuardarAsync(Revision(solicitud, ana), default);
            await repo.GuardarAsync(Revision(solicitud, luis, "NoHomologable"), default);
        }
        await using (var db = new ResultadosDbContext(options))
            await new RevisionesDocentesSql(db).GuardarAsync(Revision(solicitud, ana, "NoHomologable", "Corrijo."), default);
        await using var lectura = new ResultadosDbContext(options);
        var lista = await new RevisionesDocentesSql(lectura).ListarAsync(solicitud, default);
        Assert.Equal(2, lista.Count);
        var deAna = lista.Single(r => r.DocenteId == ana);
        Assert.Equal(("NoHomologable", "Corrijo.", "Incompleto", "docente.sintetico"), (deAna.Decision, deAna.Observacion, deAna.EstadoAgente, deAna.Docente));
        Assert.Equal(new DateTimeOffset(2026, 11, 4, 13, 0, 0, TimeSpan.FromHours(-5)), deAna.Fecha);
        Assert.Empty(await new RevisionesDocentesSql(lectura).ListarAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Base_no_disponible_no_confirma_guardado()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options);
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new RevisionesDocentesSql(db).GuardarAsync(Revision(Guid.NewGuid(), Guid.NewGuid()), default));
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new RevisionesDocentesSql(db).ListarAsync(Guid.NewGuid(), default));
    }

    [SqlServerExternoFact]
    public async Task SQL_Server_real_conserva_revisiones_con_conexion_nueva()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU20_TEST_SQL"))
            { InitialCatalog = "Hu31Tests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var admin = new ResultadosDbContext(options);
        try
        {
            await admin.Database.MigrateAsync();
            var (solicitud, ana) = (Guid.NewGuid(), Guid.NewGuid());
            await using (var db = new ResultadosDbContext(options)) await new RevisionesDocentesSql(db).GuardarAsync(Revision(solicitud, ana), default);
            await using (var db = new ResultadosDbContext(options)) await new RevisionesDocentesSql(db).GuardarAsync(Revision(solicitud, ana, "NoHomologable"), default);
            await using var lectura = new ResultadosDbContext(options);
            Assert.Equal("NoHomologable", Assert.Single(await new RevisionesDocentesSql(lectura).ListarAsync(solicitud, default)).Decision);
        }
        finally
        {
            if (connection.InitialCatalog.StartsWith("Hu31Tests_", StringComparison.Ordinal)) await admin.Database.EnsureDeletedAsync();
        }
    }
}

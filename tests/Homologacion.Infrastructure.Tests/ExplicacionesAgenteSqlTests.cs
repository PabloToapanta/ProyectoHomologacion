using Homologacion.Application.AnalisisInterno;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

/// <summary>HU-37: tabla ExplicacionesAgente, solo de inserción.</summary>
public class ExplicacionesAgenteSqlTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 22, 9, 0, 0, TimeSpan.FromHours(-5));
    private static readonly string Clave = new('a', 64);
    private static ExplicacionGuardada Explicacion(DateTimeOffset fecha, string clave, string resumen = "Explicacion sintetica.", Guid? forzadoPor = null, string? motivo = null) =>
        new(Guid.NewGuid(), clave, fecha, "modelo-sintetico", "hu18-v1", Guid.Parse("37000000-0000-0000-0000-000000000001"),
            new(resumen, ["C1", "R1"]), forzadoPor, motivo);

    private static async Task<DbContextOptions<ResultadosDbContext>> Sqlite(SqliteConnection connection)
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options;
        await using var db = new ResultadosDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return options;
    }

    [Fact]
    public void Migracion_y_snapshot_incluyen_explicaciones()
    {
        using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>()
            .UseSqlServer("Server=unused;Database=design_only;Integrated Security=true").Options);
        Assert.Contains("20261124000100_ExplicacionesAgenteHu37", db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void El_contrato_no_ofrece_modificar_ni_eliminar_explicaciones() =>
        Assert.Equal(["BuscarAsync", "GuardarAsync"], typeof(IExplicacionesAgente).GetMethods().Select(m => m.Name).Order());

    [Fact]
    public async Task Guarda_y_devuelve_la_mas_reciente_de_la_clave_sin_borrar_las_anteriores()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = await Sqlite(connection);
        var docente = Guid.NewGuid();
        var anterior = Explicacion(T0, Clave);
        var forzada = Explicacion(T0.AddHours(1), Clave, "Explicacion sintetica nueva.", docente, "Motivo sintetico");
        await using (var db = new ResultadosDbContext(options))
        {
            var almacen = new ExplicacionesAgenteSql(db);
            await almacen.GuardarAsync(anterior, default);
            await almacen.GuardarAsync(forzada, default);
            await almacen.GuardarAsync(Explicacion(T0.AddHours(2), new string('b', 64)), default);
        }
        await using var lectura = new ResultadosDbContext(options);
        var leida = await new ExplicacionesAgenteSql(lectura).BuscarAsync(Clave, default);
        Assert.NotNull(leida);
        Assert.Equal((forzada.Id, forzada.Fecha, docente, "Motivo sintetico", "modelo-sintetico", "hu18-v1", forzada.VersionParametros),
            (leida!.Id, leida.Fecha, leida.ForzadoPorId, leida.Motivo, leida.Modelo, leida.VersionPlantilla, leida.VersionParametros));
        Assert.Equal("Explicacion sintetica nueva.", leida.Explicacion.Resumen);
        Assert.Equal(["C1", "R1"], leida.Explicacion.EvidenciasCitadas);
        Assert.Equal(2, await lectura.ExplicacionesAgente.CountAsync(x => x.Clave == Clave));
        Assert.Null(await new ExplicacionesAgenteSql(lectura).BuscarAsync(new string('c', 64), default));
    }

    [Fact]
    public async Task Contenido_guardado_ilegible_se_trata_como_no_encontrado()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = await Sqlite(connection);
        await using (var db = new ResultadosDbContext(options))
            await new ExplicacionesAgenteSql(db).GuardarAsync(Explicacion(T0, Clave), default);
        await using (var db = new ResultadosDbContext(options))
            await db.Database.ExecuteSqlRawAsync("UPDATE ExplicacionesAgente SET Explicacion = '{{no json'");
        await using var lectura = new ResultadosDbContext(options);
        Assert.Null(await new ExplicacionesAgenteSql(lectura).BuscarAsync(Clave, default));
    }

    [Fact]
    public async Task Base_no_disponible_no_confirma()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options);
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new ExplicacionesAgenteSql(db).GuardarAsync(Explicacion(T0, Clave), default));
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new ExplicacionesAgenteSql(db).BuscarAsync(Clave, default));
    }

    [Fact]
    public void El_agente_Ollama_declara_modelo_y_version_de_plantilla()
    {
        var agente = new OllamaAgenteInterno(new HttpClient(), new(new Uri("http://ollama.invalid/"), "modelo-sintetico", TimeSpan.FromSeconds(5)));
        Assert.Equal(("modelo-sintetico", OllamaAgenteInterno.VersionPlantillaActual), (agente.Modelo, agente.VersionPlantilla));
        Assert.Equal("hu18-v1", OllamaAgenteInterno.VersionPlantillaActual);
    }

    [SqlServerExternoFact]
    public async Task SQL_Server_real_impide_modificar_o_eliminar_explicaciones()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU20_TEST_SQL"))
            { InitialCatalog = "Hu37Tests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var admin = new ResultadosDbContext(options);
        try
        {
            await admin.Database.MigrateAsync();
            var almacen = new ExplicacionesAgenteSql(admin);
            await almacen.GuardarAsync(Explicacion(T0, Clave), default);
            // Índice no único: dos análisis simultáneos pueden insertar la misma clave.
            await almacen.GuardarAsync(Explicacion(T0, Clave), default);
            await Assert.ThrowsAsync<SqlException>(() => admin.Database.ExecuteSqlRawAsync("UPDATE ExplicacionesAgente SET Modelo = N'otro'"));
            await Assert.ThrowsAsync<SqlException>(() => admin.Database.ExecuteSqlRawAsync("DELETE FROM ExplicacionesAgente"));
            Assert.Equal(2, await admin.ExplicacionesAgente.CountAsync(e => e.Modelo == "modelo-sintetico"));
            Assert.NotNull(await almacen.BuscarAsync(Clave, default));
        }
        finally { await admin.Database.EnsureDeletedAsync(); }
    }
}

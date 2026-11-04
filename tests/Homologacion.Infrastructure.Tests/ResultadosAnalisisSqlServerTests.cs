using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Infrastructure.AnalisisInterno;
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
            await repo.GuardarAsync(Resultado(a, aprobada, Guid.NewGuid(), "Incompleto", antes.AddMinutes(5)), default);
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
        Assert.Equal("Homologable", completo.Analisis.Resultado.Sugerencia);
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
            var pareja = r with { Analisis = r.Analisis with { AsignaturaReceptoraId = Guid.NewGuid() } };
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

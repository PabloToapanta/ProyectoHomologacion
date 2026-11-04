using System.Net;
using System.Text;
using System.Text.Json;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Homologacion.Infrastructure.AnalisisExterno;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

/// <summary>Solo se ejecuta con un SQL Server aislado (variable HU20_TEST_SQL, la misma del workflow de persistencia).</summary>
public sealed class SqlServerExternoFactAttribute : FactAttribute
{
    public SqlServerExternoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HU20_TEST_SQL")))
            Skip = "Requiere HU20_TEST_SQL con un SQL Server aislado; no usa bases del equipo.";
    }
}

/// <summary>HU-19: fuente HTTP del contexto externo y tabla propia AnalisisExternos.</summary>
public class AnalisisExternoInfraestructuraTests
{
    private static readonly Guid Id = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private sealed class Umbral : IUmbralEquivalencia { public decimal Valor => 80; }
    private sealed class Capture(string body = "{}", HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
    private static FuentesAnalisisHttp Fuente(HttpClient http) => new(http, Options.Create(new ConfiguracionAnalisisInterno()));
    private static AnalisisGuardado Resultado(Guid solicitud, Guid aprobada, Guid receptora, string estado = "Incompleto") => new(DateTimeOffset.UtcNow,
        new(solicitud, aprobada, receptora, estado, "Mensaje sintético", null,
            new EvaluarEquivalencia(new Umbral()).Ejecutar(OllamaRealTests.DatosSinteticos()), null, true, ["No es una aprobación oficial."]));

    [Fact]
    public async Task Lee_el_contexto_externo_de_Solicitudes()
    {
        var body = JsonSerializer.Serialize(new
        {
            id = Id, tipoCambio = "Externo", carreraReceptoraId = Guid.NewGuid(), institucionOrigen = "Universidad sintética Ecuador",
            carreraOrigenExterna = "Carrera sintética", periodosCursados = (int?)null, cambiosPreviosCarrera = 0,
            asignaturasAprobadas = new[] { new { id = Guid.NewGuid(), nombre = "Externa sintética", horas = 64, unidadHoras = "hora", creditos = 4, sistemaCreditos = "sistema",
                referenciaPrograma = "programa", estado = "Aprobada", fechaAprobacion = "2025-01-01" } }
        });
        using var handler = new Capture(body); using var http = new HttpClient(handler);
        var r = await Fuente(http).ObtenerSolicitudExternaAsync(Id, default);
        Assert.Equal($"/api/solicitudes/{Id}/contexto-analisis-externo", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Null(r!.PeriodosCursados);
        Assert.Equal(0, r.CambiosPreviosCarrera);
        Assert.Equal("hora", Assert.Single(r.AsignaturasAprobadas).UnidadHoras);
    }

    [Fact]
    public async Task Contexto_externo_ausente_o_de_otra_solicitud()
    {
        using (var http = new HttpClient(new Capture("{}", HttpStatusCode.NotFound)))
            Assert.Null(await Fuente(http).ObtenerSolicitudExternaAsync(Id, default));
        var ajeno = JsonSerializer.Serialize(new { id = Guid.NewGuid(), tipoCambio = "Externo", carreraReceptoraId = Guid.NewGuid(), asignaturasAprobadas = Array.Empty<object>() });
        using (var http = new HttpClient(new Capture(ajeno)))
            await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Fuente(http).ObtenerSolicitudExternaAsync(Id, default));
        using (var http = new HttpClient(new Capture("{}", HttpStatusCode.InternalServerError)))
            await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Fuente(http).ObtenerSolicitudExternaAsync(Id, default));
    }

    [Fact]
    public void Migracion_externa_y_snapshot_corresponden_al_modelo()
    {
        using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>()
            .UseSqlServer("Server=unused;Database=design_only;Integrated Security=true").Options);
        Assert.Equal(["20261104000100_AnalisisInternosHu20", "20261104000200_AnalisisExternosHu19"], db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Tabla_externa_es_independiente_de_la_interna_y_reemplaza_la_pareja()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options;
        var (solicitud, aprobada, receptora) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using (var db = new ResultadosDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            await new ResultadosAnalisisSql(db).GuardarAsync(Resultado(solicitud, aprobada, receptora, "Completo"), default);
            var externo = new ResultadosAnalisisExternoSql(db);
            await externo.GuardarAsync(Resultado(solicitud, aprobada, receptora), default);
            await externo.GuardarAsync(Resultado(solicitud, aprobada, receptora, "Fallido"), default);
        }
        await using var lectura = new ResultadosDbContext(options);
        var guardado = Assert.Single(await new ResultadosAnalisisExternoSql(lectura).ListarAsync(solicitud, default));
        Assert.Equal("Fallido", guardado.Analisis.Estado);
        Assert.Equal("Completo", Assert.Single(await new ResultadosAnalisisSql(lectura).ListarAsync(solicitud, default)).Analisis.Estado);
        Assert.Equal(1, await lectura.ResultadosExternos.CountAsync());
    }

    [Fact]
    public async Task Base_externa_no_disponible_no_confirma_guardado()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ResultadosDbContext(new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlite(connection).Options);
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() =>
            new ResultadosAnalisisExternoSql(db).GuardarAsync(Resultado(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), default));
        await Assert.ThrowsAsync<AlmacenAnalisisNoDisponibleException>(() => new ResultadosAnalisisExternoSql(db).ListarAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public void Composicion_registra_el_analisis_externo()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Homologacion"] = "Server=unused;Database=unused", ["InternalAccess:Key"] = "clave-sintetica-servidor-hu19-32-bytes"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(cfg); services.AddLogging();
        services.AddSingleton<IUmbralEquivalencia, Umbral>(); services.AddScoped<EvaluarEquivalencia>();
        services.AddAnalisisInterno();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GestionarAnalisisExterno>());
        Assert.IsType<ResultadosAnalisisExternoSql>(scope.ServiceProvider.GetRequiredService<IResultadosAnalisisExterno>());
    }

    [SqlServerExternoFact]
    public async Task SQL_Server_real_conserva_el_analisis_externo_con_conexion_nueva()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HU20_TEST_SQL"))
            { InitialCatalog = "Hu19Tests_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<ResultadosDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var admin = new ResultadosDbContext(options);
        try
        {
            await admin.Database.MigrateAsync();
            var (solicitud, aprobada, receptora) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await using (var db = new ResultadosDbContext(options))
            {
                await new ResultadosAnalisisExternoSql(db).GuardarAsync(Resultado(solicitud, aprobada, receptora), default);
                await new ResultadosAnalisisExternoSql(db).GuardarAsync(Resultado(solicitud, aprobada, receptora), default);
            }
            await using var lectura = new ResultadosDbContext(options);
            var guardado = Assert.Single(await new ResultadosAnalisisExternoSql(lectura).ListarAsync(solicitud, default));
            Assert.Equal("Incompleto", guardado.Analisis.Estado);
            Assert.Null(guardado.Analisis.Resultado);
            Assert.Empty(await new ResultadosAnalisisSql(lectura).ListarAsync(solicitud, default));
        }
        finally
        {
            if (connection.InitialCatalog.StartsWith("Hu19Tests_", StringComparison.Ordinal)) await admin.Database.EnsureDeletedAsync();
        }
    }
}

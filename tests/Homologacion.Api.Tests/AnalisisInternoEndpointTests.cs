using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Equivalencias;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Homologacion.Api.Tests;

internal sealed class EscenarioInterno : ISolicitudesAnalisisInterno, ICatalogoAnalisisInterno, IAgenteAnalisisInterno
{
    public static readonly Guid SolicitudId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid AprobadaId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid OrigenId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid ReceptoraId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid CarreraOrigen = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid CarreraReceptora = Guid.Parse("10000000-0000-0000-0000-000000000006");
    private static readonly Guid ContenidoO = Guid.Parse("10000000-0000-0000-0000-000000000007");
    private static readonly Guid ContenidoR = Guid.Parse("10000000-0000-0000-0000-000000000008");
    private static readonly Guid ResultadoO = Guid.Parse("10000000-0000-0000-0000-000000000009");
    private static readonly Guid ResultadoR = Guid.Parse("10000000-0000-0000-0000-000000000010");
    public SolicitudParaAnalisis? Solicitud { get; set; } = new(SolicitudId, "Interno", CarreraOrigen, CarreraReceptora,
        [new(AprobadaId, OrigenId, 8, 80, "Aprobada", new DateOnly(2025, 1, 1))]);
    public Dictionary<Guid, AsignaturaParaAnalisis> Materias { get; } = new()
    {
        [OrigenId] = new(OrigenId, CarreraOrigen, "FISEI", true, 8, 80, "plan-sintetico-O", "hora-reloj", "sistema-sintetico", true,
            [new(ContenidoO, "Contenido origen sintetico")], [new(ResultadoO, "Resultado origen sintetico")]),
        [ReceptoraId] = new(ReceptoraId, CarreraReceptora, "FISEI", true, 10, 100, "plan-sintetico-R", "hora-reloj", "sistema-sintetico", true,
            [new(ContenidoR, "Contenido receptor sintetico")], [new(ResultadoR, "Resultado receptor sintetico")])
    };
    public RevisionParaAnalisis Revision { get; set; } = new(true, true,
        [new(ContenidoO.ToString(), ContenidoR.ToString(), "plan-O/U1/C1", "plan-R/U1/C1")],
        [new(ResultadoO.ToString(), ResultadoR.ToString(), "plan-O/U1/RA", "plan-R/U1/RA")]);
    public bool FuenteCaida { get; set; }
    public TipoFalloAgente? FalloAgente { get; set; }
    public int Consultas { get; private set; }
    public int LlamadasAgente { get; private set; }
    public ContextoAgente? Contexto { get; private set; }
    public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct)
    {
        Consultas++;
        if (FuenteCaida) throw new FuenteAnalisisNoDisponibleException();
        return Task.FromResult(id == SolicitudId ? Solicitud : null);
    }
    public Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult(Materias.GetValueOrDefault(id));
    public Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct) => Task.FromResult(Revision);
    public Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken ct)
    {
        LlamadasAgente++; Contexto = contexto;
        return FalloAgente is { } fallo ? Task.FromException<ExplicacionAgente>(new FalloAgenteException(fallo))
            : Task.FromResult(new ExplicacionAgente("Asistencia sintetica sobre evidencia registrada.", ["C1", "R1"]));
    }
}
internal sealed class RelojAnalisis : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
}
internal sealed class UmbralAnalisis : IUmbralEquivalencia { public decimal Valor => 80; }

public class AnalisisInternoEndpointTests
{
    private static string Ruta => $"/api/homologaciones/solicitudes/{EscenarioInterno.SolicitudId}/analisis-interno";
    private static object Input() => new { asignaturaAprobadaId = EscenarioInterno.AprobadaId, asignaturaReceptoraId = EscenarioInterno.ReceptoraId };
    private static WebApplicationFactory<Homologacion.Api.Program> Host(MotorFactory factory, EscenarioInterno e, bool simular = true) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISolicitudesAnalisisInterno>(); services.AddSingleton<ISolicitudesAnalisisInterno>(e);
            services.RemoveAll<ICatalogoAnalisisInterno>(); services.AddSingleton<ICatalogoAnalisisInterno>(e);
            services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new RelojAnalisis());
            if (simular) { services.RemoveAll<IAgenteAnalisisInterno>(); services.AddSingleton<IAgenteAnalisisInterno>(e); }
        }));
    [Fact]
    public async Task Docente_analiza_solo_ids_y_recibe_resultado_estructurado()
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno(); using var host = Host(f, e); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        using var response = await c.PostAsJsonAsync(Ruta, Input());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var j = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Completo", j.RootElement.GetProperty("estado").GetString());
        Assert.Equal(100, j.RootElement.GetProperty("resultado").GetProperty("porcentajeAcademico").GetDecimal());
        Assert.True(j.RootElement.GetProperty("evaluacion").GetProperty("homologable").GetBoolean());
        Assert.Equal("Elegible", j.RootElement.GetProperty("antiguedad").GetProperty("estado").GetString());
    }
    [Theory]
    [InlineData(null, 401)]
    [InlineData("Administrador", 403)]
    [InlineData("Coordinador", 403)]
    public async Task Otros_roles_no_pueden_analizar(string? role, int expected)
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno(); using var host = Host(f, e); using var c = host.CreateClient();
        if (role is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(role));
        Assert.Equal(expected, (int)(await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
        Assert.Equal(0, e.Consultas);
    }
    [Fact]
    public async Task No_permite_suplir_horas_creditos_ni_identidad_desde_cliente()
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno(); using var host = Host(f, e); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        var body = new { asignaturaAprobadaId = EscenarioInterno.AprobadaId, asignaturaReceptoraId = EscenarioInterno.ReceptoraId, horas = 999, creditos = 999, docenteId = Guid.NewGuid() };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(Ruta, body)).StatusCode);
        Assert.Equal(0, e.Consultas);
    }
    [Fact]
    public async Task Solicitud_ajena_es_404_sin_modelo()
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno { Solicitud = null }; using var host = Host(f, e); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
        Assert.Equal(0, e.LlamadasAgente);
    }
    [Fact]
    public async Task Dependencia_caida_responde_503_sin_detalles_privados()
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno { FuenteCaida = true }; using var host = Host(f, e); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
    }
    [Fact]
    public async Task Modelo_no_configurado_no_simula_exito_ni_impide_arranque()
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno(); using var host = Host(f, e, false); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        using var r = await c.PostAsJsonAsync(Ruta, Input());
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal("Fallido", j.RootElement.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, j.RootElement.GetProperty("resultado").ValueKind);
        Assert.Equal(0, e.LlamadasAgente);
    }
}

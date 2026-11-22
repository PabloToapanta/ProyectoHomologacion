using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Homologacion.Api.Tests;

/// <summary>HU-37: reutilización y forzado del análisis del agente en el endpoint interno.</summary>
public class ReutilizacionAnalisisEndpointTests
{
    private sealed class AgenteDescrito(EscenarioInterno e) : IAgenteAnalisisInterno, IDescripcionAgente
    {
        public string Modelo => "modelo-sintetico";
        public string VersionPlantilla => "hu18-v1";
        public Task<ExplicacionAgente> JustificarAsync(ContextoAgente contexto, CancellationToken ct) => e.JustificarAsync(contexto, ct);
    }
    private sealed class Explicaciones : IExplicacionesAgente
    {
        public List<ExplicacionGuardada> Filas { get; } = [];
        public int Busquedas { get; private set; }
        public Task<ExplicacionGuardada?> BuscarAsync(string clave, CancellationToken ct)
        {
            Busquedas++;
            return Task.FromResult(Filas.Where(f => f.Clave == clave).OrderByDescending(f => f.Fecha).FirstOrDefault());
        }
        public Task GuardarAsync(ExplicacionGuardada e, CancellationToken ct) { Filas.Add(e); return Task.CompletedTask; }
    }
    private sealed class Resultados : IResultadosAnalisisInterno
    {
        public Task GuardarAsync(AnalisisGuardado r, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<AnalisisGuardado>>([]);
    }
    private static string Ruta => $"/api/homologaciones/solicitudes/{EscenarioInterno.SolicitudId}/analisis-interno";
    private static WebApplicationFactory<Homologacion.Api.Program> Host(MotorFactory factory, EscenarioInterno e, Explicaciones explicaciones) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AnalisisInterno:Habilitado", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IResultadosAnalisisInterno>(); services.AddSingleton<IResultadosAnalisisInterno>(new Resultados());
                services.RemoveAll<ISolicitudesAnalisisInterno>(); services.AddSingleton<ISolicitudesAnalisisInterno>(e);
                services.RemoveAll<ICatalogoAnalisisInterno>(); services.AddSingleton<ICatalogoAnalisisInterno>(e);
                services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new RelojAnalisis());
                services.RemoveAll<IAgenteAnalisisInterno>(); services.AddSingleton<IAgenteAnalisisInterno>(new AgenteDescrito(e));
                services.RemoveAll<IExplicacionesAgente>(); services.AddSingleton<IExplicacionesAgente>(explicaciones);
            });
        });
    private static object Input(bool? forzarNuevo = null, string? motivo = null) => forzarNuevo is null && motivo is null
        ? new { asignaturaAprobadaId = EscenarioInterno.AprobadaId, asignaturaReceptoraId = EscenarioInterno.ReceptoraId }
        : new { asignaturaAprobadaId = EscenarioInterno.AprobadaId, asignaturaReceptoraId = EscenarioInterno.ReceptoraId, forzarNuevo, motivo };
    private static async Task<JsonElement> Agente(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("agente").Clone();
    }

    [Fact]
    public async Task CA1_y_CA4_reutiliza_y_forzar_pide_una_explicacion_nueva()
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno(); var explicaciones = new Explicaciones();
        using var host = Host(f, e, explicaciones); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));

        var primero = await Agente(await c.PostAsJsonAsync(Ruta, Input()));
        Assert.False(primero.GetProperty("reutilizada").GetBoolean());
        var segundo = await Agente(await c.PostAsJsonAsync(Ruta, Input()));
        Assert.True(segundo.GetProperty("reutilizada").GetBoolean());
        Assert.Equal(primero.GetProperty("explicacionId").GetGuid(), segundo.GetProperty("explicacionOriginalId").GetGuid());
        Assert.Equal("hu18-v1", segundo.GetProperty("versionPlantilla").GetString());
        Assert.Equal(1, e.LlamadasAgente);

        var forzado = await Agente(await c.PostAsJsonAsync(Ruta, Input(true, "Se revisó el programa receptor.")));
        Assert.False(forzado.GetProperty("reutilizada").GetBoolean());
        Assert.Equal(2, e.LlamadasAgente);
        Assert.Equal(2, explicaciones.Filas.Count);
        Assert.Equal("Se revisó el programa receptor.", explicaciones.Filas[^1].Motivo);
        Assert.NotNull(explicaciones.Filas[^1].ForzadoPorId);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(true, "   ")]
    [InlineData(false, "motivo sin forzar")]
    public async Task CA4_motivo_obligatorio_al_forzar_y_solo_al_forzar(bool forzar, string? motivo)
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno(); var explicaciones = new Explicaciones();
        using var host = Host(f, e, explicaciones); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        using var response = await c.PostAsJsonAsync(Ruta, Input(forzar, motivo));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((0, 0, 0), (e.Consultas, e.LlamadasAgente, explicaciones.Busquedas));
    }

    [Fact]
    public async Task CA5_sin_autenticacion_otro_rol_o_sin_asignacion_no_consulta_el_almacen()
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno(); var explicaciones = new Explicaciones();
        using var host = Host(f, e, explicaciones); using var c = host.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Estudiante"));
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        e.Solicitud = null;
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync(Ruta, Input(true, "Motivo sintetico"))).StatusCode);
        Assert.Equal((0, 0), (e.LlamadasAgente, explicaciones.Busquedas));
    }

    [Fact]
    public async Task Incompleto_no_lleva_bloque_del_agente()
    {
        using var f = new MotorFactory(); var e = new EscenarioInterno { Revision = new(false, false, [], []) }; var explicaciones = new Explicaciones();
        using var host = Host(f, e, explicaciones); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        using var response = await c.PostAsJsonAsync(Ruta, Input());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Incompleto", json.RootElement.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("agente").ValueKind);
        Assert.Empty(explicaciones.Filas);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Homologacion.Api.Tests;

/// <summary>HU-19: rutas del análisis externo y de RN-05 externo.</summary>
public class AnalisisExternoEndpointTests
{
    private static readonly Guid SolicitudId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid AprobadaId = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly Guid ReceptoraId = Guid.Parse("50000000-0000-0000-0000-000000000003");
    private static readonly Guid Carrera = Guid.Parse("50000000-0000-0000-0000-000000000004");

    private sealed class Escenario : ISolicitudesAnalisisExterno, ICatalogoAnalisisInterno, IReceptorasAnalisisInterno, ISolicitudesAnalisisInterno
    {
        public SolicitudExternaParaAnalisis? Solicitud { get; set; } = new(SolicitudId, "Externo", Carrera, "Universidad sintética Ecuador", "Carrera sintética", 3, 0,
            [new(AprobadaId, "Externa sintética", 80, "hora-reloj", 8, "sistema-sintetico", "programa-sintetico", "Aprobada", new DateOnly(2025, 1, 1))]);
        public bool FuenteCaida { get; set; }
        public int Consultas { get; private set; }
        public Guid CarreraListada { get; private set; }
        public Task<SolicitudExternaParaAnalisis?> ObtenerSolicitudExternaAsync(Guid id, CancellationToken ct)
        {
            Consultas++;
            if (FuenteCaida) throw new FuenteAnalisisNoDisponibleException();
            return Task.FromResult(id == SolicitudId ? Solicitud : null);
        }
        public Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult(id != ReceptoraId ? null :
            new AsignaturaParaAnalisis(ReceptoraId, Carrera, "FISEI", true, 10, 100, "plan-sintetico-R", "hora-reloj", "sistema-sintetico", true,
                [new(Guid.NewGuid(), "Contenido receptor sintético")], [new(Guid.NewGuid(), "Resultado receptor sintético")]));
        public Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct) => throw new InvalidOperationException();
        public Task<IReadOnlyList<ReceptoraAnalisis>> ListarAsync(Guid carreraId, CancellationToken ct)
        {
            CarreraListada = carreraId;
            return Task.FromResult<IReadOnlyList<ReceptoraAnalisis>>([new(Guid.NewGuid(), "Z", "Avanzada", 2), new(ReceptoraId, "A", "Inicial", 1)]);
        }
        public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct) => Task.FromResult<SolicitudParaAnalisis?>(null);
    }
    private sealed class Almacen : IResultadosAnalisisExterno
    {
        public bool Fallar { get; set; }
        public List<AnalisisGuardado> Datos { get; } = [];
        public Task GuardarAsync(AnalisisGuardado r, CancellationToken ct)
        {
            if (Fallar) throw new AlmacenAnalisisNoDisponibleException();
            Datos.RemoveAll(x => x.Analisis.AsignaturaAprobadaId == r.Analisis.AsignaturaAprobadaId && x.Analisis.AsignaturaReceptoraId == r.Analisis.AsignaturaReceptoraId);
            Datos.Add(r); return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AnalisisGuardado>>(Datos.Where(x => x.Analisis.SolicitudId == id).ToArray());
    }

    private static string Ruta => $"/api/homologaciones/solicitudes/{SolicitudId}/analisis-externo";
    private static string Requisitos => $"/api/homologaciones/solicitudes/{SolicitudId}/requisitos-externos";
    private static object Input() => new { asignaturaAprobadaId = AprobadaId, asignaturaReceptoraId = ReceptoraId };
    private static WebApplicationFactory<Homologacion.Api.Program> Host(MotorFactory factory, Escenario e, Almacen? almacen = null) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AnalisisInterno:Habilitado", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IResultadosAnalisisExterno>(); services.AddSingleton<IResultadosAnalisisExterno>(almacen ?? new Almacen());
                services.RemoveAll<ISolicitudesAnalisisExterno>(); services.AddSingleton<ISolicitudesAnalisisExterno>(e);
                services.RemoveAll<ISolicitudesAnalisisInterno>(); services.AddSingleton<ISolicitudesAnalisisInterno>(e);
                services.RemoveAll<ICatalogoAnalisisInterno>(); services.AddSingleton<ICatalogoAnalisisInterno>(e);
                services.RemoveAll<IReceptorasAnalisisInterno>(); services.AddSingleton<IReceptorasAnalisisInterno>(e);
                services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new RelojAnalisis());
            });
        });
    private static HttpClient Docente(MotorFactory f, WebApplicationFactory<Homologacion.Api.Program> host)
    {
        var c = host.CreateClient(); c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente")); return c;
    }

    [Fact]
    public async Task Analiza_guarda_y_recupera_sin_sugerencia()
    {
        using var f = new MotorFactory(); var e = new Escenario(); using var host = Host(f, e); using var c = Docente(f, host);
        using var response = await c.PostAsJsonAsync(Ruta, Input());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using (var j = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
        {
            Assert.Equal("Incompleto", j.RootElement.GetProperty("estado").GetString());
            Assert.Equal(JsonValueKind.Null, j.RootElement.GetProperty("resultado").ValueKind);
            var evaluacion = j.RootElement.GetProperty("evaluacion");
            Assert.Equal(JsonValueKind.Null, evaluacion.GetProperty("compatibilidadAcademicaPct").ValueKind);
            Assert.Equal(80, evaluacion.GetProperty("compatibilidadHorariaPct").GetDecimal());
            Assert.False(evaluacion.GetProperty("homologable").GetBoolean());
        }
        using var saved = await c.GetAsync(Ruta);
        using var lista = JsonDocument.Parse(await saved.Content.ReadAsStringAsync());
        Assert.Equal("Incompleto", Assert.Single(lista.RootElement.EnumerateArray()).GetProperty("analisis").GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Opciones_de_la_carrera_receptora_y_rechazo_de_solicitud_interna()
    {
        using var f = new MotorFactory(); var e = new Escenario(); using var host = Host(f, e); using var c = Docente(f, host);
        using var opciones = JsonDocument.Parse(await c.GetStringAsync(Ruta + "/opciones"));
        Assert.Equal(1, opciones.RootElement[0].GetProperty("nivel").GetInt32());
        Assert.Equal(Carrera, e.CarreraListada);
        e.Solicitud = e.Solicitud! with { TipoCambio = "Interno" };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(Ruta + "/opciones")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(Requisitos)).StatusCode);
    }

    [Fact]
    public async Task Requisitos_externos_informan_incompleto_sin_analisis_completos()
    {
        using var f = new MotorFactory(); var e = new Escenario(); using var host = Host(f, e); using var c = Docente(f, host);
        await c.PostAsJsonAsync(Ruta, Input());
        using var response = await c.GetAsync(Requisitos);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var j = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Incompleto", j.RootElement.GetProperty("estado").GetString());
        Assert.Equal(0, j.RootElement.GetProperty("asignaturasHomologables").GetInt32());
        Assert.Empty(j.RootElement.GetProperty("incumplimientos").EnumerateArray());
        e.Solicitud = e.Solicitud! with { CambiosPreviosCarrera = 1 };
        using var incumple = JsonDocument.Parse(await c.GetStringAsync(Requisitos));
        Assert.Equal("NoCumple", incumple.RootElement.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Ajena_almacen_y_fuente_caidos()
    {
        using var f = new MotorFactory(); var e = new Escenario { Solicitud = null };
        using (var host = Host(f, e)) using (var c = Docente(f, host))
        {
            Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Ruta)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Requisitos)).StatusCode);
        }
        var almacen = new Almacen { Fallar = true };
        using (var host = Host(f, new Escenario(), almacen)) using (var c = Docente(f, host))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
        using (var host = Host(f, new Escenario { FuenteCaida = true })) using (var c = Docente(f, host))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
    }

    [Fact]
    public async Task No_acepta_horas_creditos_ni_identidad_del_cliente()
    {
        using var f = new MotorFactory(); var e = new Escenario(); using var host = Host(f, e); using var c = Docente(f, host);
        var body = new { asignaturaAprobadaId = AprobadaId, asignaturaReceptoraId = ReceptoraId, horas = 999, unidadHoras = "otra", docenteId = Guid.NewGuid() };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(Ruta, body)).StatusCode);
        Assert.Equal(0, e.Consultas);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Administrador", 403)]
    [InlineData("Coordinador", 403)]
    public async Task Otros_roles_no_acceden(string? role, int esperado)
    {
        using var f = new MotorFactory(); var e = new Escenario(); using var host = Host(f, e); using var c = host.CreateClient();
        if (role is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(role));
        Assert.Equal(esperado, (int)(await c.PostAsJsonAsync(Ruta, Input())).StatusCode);
        Assert.Equal(esperado, (int)(await c.GetAsync(Ruta)).StatusCode);
        Assert.Equal(esperado, (int)(await c.GetAsync(Ruta + "/opciones")).StatusCode);
        Assert.Equal(esperado, (int)(await c.GetAsync(Requisitos)).StatusCode);
        Assert.Equal(0, e.Consultas);
    }
}

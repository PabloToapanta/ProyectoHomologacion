using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Application.RevisionDocente;
using Homologacion.Domain.Equivalencias;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Homologacion.Api.Tests;

/// <summary>HU-31: análisis propio de los docentes asignados junto al del agente.</summary>
public class RevisionesDocentesEndpointTests
{
    private static readonly Guid Solicitud = Guid.Parse("80000000-0000-0000-0000-000000000001");
    private static readonly Guid Aprobada = Guid.Parse("80000000-0000-0000-0000-000000000002");
    private static readonly Guid Receptora = Guid.Parse("80000000-0000-0000-0000-000000000003");
    private static readonly Guid Carrera = Guid.Parse("80000000-0000-0000-0000-000000000004");

    private sealed class Fuentes : ISolicitudesAnalisisInterno, ICatalogoAnalisisInterno, IResultadosAnalisisInterno, IResultadosAnalisisExterno, IRevisionesDocentes
    {
        public bool Asignada { get; set; } = true;
        public bool AlmacenCaido { get; set; }
        public int Consultas { get; private set; }
        public List<AnalisisGuardado> Agente { get; } = [];
        public List<RevisionDocenteRegistrada> Revisiones { get; } = [];
        public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct)
        {
            Consultas++;
            return Task.FromResult(id == Solicitud && Asignada
                ? new SolicitudParaAnalisis(Solicitud, "Interno", Guid.NewGuid(), Carrera, [new(Aprobada, Guid.NewGuid(), 3, 144, "Aprobada", new DateOnly(2025, 1, 1))])
                : null);
        }
        public Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(id == Receptora ? new AsignaturaParaAnalisis(Receptora, Carrera, "FISEI", true, 3, 144, "plan", "h", "c", true, [], []) : null);
        public Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct) => throw new InvalidOperationException();
        public Task GuardarAsync(AnalisisGuardado resultado, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AnalisisGuardado>> ListarAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult<IReadOnlyList<AnalisisGuardado>>(Agente);
        public Task GuardarAsync(RevisionDocenteRegistrada r, CancellationToken ct)
        {
            if (AlmacenCaido) throw new AlmacenAnalisisNoDisponibleException();
            Revisiones.RemoveAll(x => x.DocenteId == r.DocenteId && x.AsignaturaAprobadaId == r.AsignaturaAprobadaId && x.AsignaturaReceptoraId == r.AsignaturaReceptoraId);
            Revisiones.Add(r); return Task.CompletedTask;
        }
        Task<IReadOnlyList<RevisionDocenteRegistrada>> IRevisionesDocentes.ListarAsync(Guid solicitudId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<RevisionDocenteRegistrada>>(Revisiones.Where(r => r.SolicitudId == solicitudId).ToArray());
    }

    /// <summary>HU-23: sin informe generado, los análisis docentes siguen abiertos.</summary>
    private sealed class SinInforme : IInformesHomologacion
    {
        public Task<InformeHomologacion?> ObtenerAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult<InformeHomologacion?>(null);
        public Task GuardarAsync(InformeHomologacion informe, CancellationToken ct) => Task.CompletedTask;
    }

    private static string Ruta => $"/api/homologaciones/solicitudes/{Solicitud}/revisiones-docentes";
    private static WebApplicationFactory<Homologacion.Api.Program> Host(MotorFactory f, Fuentes e) => f.WithWebHostBuilder(b =>
    {
        b.UseSetting("AnalisisInterno:Habilitado", "false");
        b.ConfigureServices(s =>
        {
            s.RemoveAll<ISolicitudesAnalisisInterno>(); s.AddSingleton<ISolicitudesAnalisisInterno>(e);
            s.RemoveAll<ICatalogoAnalisisInterno>(); s.AddSingleton<ICatalogoAnalisisInterno>(e);
            s.RemoveAll<IResultadosAnalisisInterno>(); s.AddSingleton<IResultadosAnalisisInterno>(e);
            s.RemoveAll<IResultadosAnalisisExterno>(); s.AddSingleton<IResultadosAnalisisExterno>(e);
            s.RemoveAll<IRevisionesDocentes>(); s.AddSingleton<IRevisionesDocentes>(e);
            s.RemoveAll<IInformesHomologacion>(); s.AddSingleton<IInformesHomologacion>(new SinInforme());
            s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(new RelojAnalisis());
        });
    });
    private static HttpClient Cliente(MotorFactory f, WebApplicationFactory<Homologacion.Api.Program> host, string rol = "Docente")
    {
        var c = host.CreateClient(); c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol)); return c;
    }
    private static object Ajuste(string decision = "NoHomologable", string? observacion = "Faltan contenidos.") =>
        new { asignaturaAprobadaId = Aprobada, asignaturaReceptoraId = Receptora, postura = "Ajuste", decision, observacion };

    [Fact]
    public async Task De_acuerdo_con_analisis_completo_guarda_la_sugerencia_del_agente()
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e); using var c = Cliente(f, host);
        e.Agente.Add(new(DateTimeOffset.UnixEpoch, new(Solicitud, Aprobada, Receptora, "Completo", "m", new(100, 100, 100, "Homologable", "j", []),
            new([], null, 100, 100, 100, 80, EstadoEquivalencia.Homologable, "", [], [], []), null, true, [])));
        using var r = await c.PutAsJsonAsync(Ruta, new { asignaturaAprobadaId = Aprobada, asignaturaReceptoraId = Receptora, postura = "DeAcuerdo" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal("Homologable", j.RootElement.GetProperty("decision").GetString());
        Assert.Equal("Completo", j.RootElement.GetProperty("estadoAgente").GetString());
        Assert.Equal("usuario-sintetico", j.RootElement.GetProperty("docente").GetString());
    }

    [Fact]
    public async Task Dos_docentes_ven_ambos_analisis_y_cada_uno_corrige_el_suyo()
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e);
        using var ana = Cliente(f, host); using var luis = Cliente(f, host);
        Assert.Equal(HttpStatusCode.OK, (await ana.PutAsJsonAsync(Ruta, Ajuste("Homologable"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ana.PutAsJsonAsync(Ruta, Ajuste("NoHomologable", "Corrijo."))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await luis.PutAsJsonAsync(Ruta, Ajuste("Homologable", "Discrepo."))).StatusCode);
        using var j = JsonDocument.Parse(await luis.GetStringAsync(Ruta));
        Assert.Equal(2, j.RootElement.GetArrayLength());
        Assert.Contains(j.RootElement.EnumerateArray(), x => x.GetProperty("observacion").GetString() == "Corrijo.");
    }

    [Fact]
    public async Task Reglas_identidad_ajena_y_campos_extra_se_rechazan()
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e); using var c = Cliente(f, host);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync(Ruta, new { asignaturaAprobadaId = Aprobada, asignaturaReceptoraId = Receptora, postura = "DeAcuerdo" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync(Ruta, Ajuste(observacion: null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync(Ruta, new { asignaturaAprobadaId = Aprobada, asignaturaReceptoraId = Receptora, postura = "Ajuste",
            decision = "Homologable", observacion = "x", docenteId = Guid.NewGuid() })).StatusCode);
        Assert.Empty(e.Revisiones);
    }

    [Fact]
    public async Task No_asignado_es_404_y_almacen_caido_503()
    {
        using var f = new MotorFactory(); var e = new Fuentes { Asignada = false }; using var host = Host(f, e); using var c = Cliente(f, host);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync(Ruta, Ajuste())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Ruta)).StatusCode);
        e.Asignada = true; e.AlmacenCaido = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await c.PutAsJsonAsync(Ruta, Ajuste())).StatusCode);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Coordinador", 403)]
    [InlineData("Administrador", 403)]
    public async Task Otros_roles_no_registran_ni_consultan(string? rol, int esperado)
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e); using var c = host.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        Assert.Equal(esperado, (int)(await c.PutAsJsonAsync(Ruta, Ajuste())).StatusCode);
        Assert.Equal(esperado, (int)(await c.GetAsync(Ruta)).StatusCode);
        Assert.Equal(0, e.Consultas);
    }
}

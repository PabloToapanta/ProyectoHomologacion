using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Application.RevisionDocente;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Homologacion.Api.Tests;

/// <summary>HU-23: generación y consulta del informe de homologación.</summary>
public class InformeEndpointTests
{
    private static readonly Guid Solicitud = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid Aprobada = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    private static readonly Guid Receptora = Guid.Parse("a0000000-0000-0000-0000-000000000003");
    private static readonly Guid Carrera = Guid.Parse("a0000000-0000-0000-0000-000000000004");

    private sealed class Fuentes : ISolicitudesAnalisisInterno, ICatalogoAnalisisInterno, IReceptorasAnalisisInterno, IResultadosAnalisisInterno, IResultadosAnalisisExterno,
        IRevisionesDocentes, IInformesHomologacion
    {
        public bool Asignada { get; set; } = true;
        public int Consultas { get; private set; }
        public List<RevisionDocenteRegistrada> Revisiones { get; } = [];
        public InformeHomologacion? Informe { get; private set; }
        public Task<SolicitudParaAnalisis?> ObtenerSolicitudAsync(Guid id, CancellationToken ct)
        {
            Consultas++;
            return Task.FromResult(id == Solicitud && Asignada
                ? new SolicitudParaAnalisis(Solicitud, "Interno", Guid.NewGuid(), Carrera, [new(Aprobada, Guid.NewGuid(), 3, 144, "Aprobada", new DateOnly(2025, 1, 1))])
                : null);
        }
        public Task<AsignaturaParaAnalisis?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<AsignaturaParaAnalisis?>(new(id, Carrera, "FISEI", true, 3, 144, "plan", "h", "c", true, [], []));
        public Task<RevisionParaAnalisis> ObtenerRevisionAsync(Guid origen, Guid receptora, CancellationToken ct) => throw new InvalidOperationException();
        public Task<IReadOnlyList<ReceptoraAnalisis>> ListarAsync(Guid carreraId, CancellationToken ct) => Task.FromResult<IReadOnlyList<ReceptoraAnalisis>>([new(Receptora, "R-01", "Receptora", 1)]);
        public Task GuardarAsync(AnalisisGuardado resultado, CancellationToken ct) => Task.CompletedTask;
        Task<IReadOnlyList<AnalisisGuardado>> IResultadosAnalisisInterno.ListarAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult<IReadOnlyList<AnalisisGuardado>>([]);
        Task<IReadOnlyList<AnalisisGuardado>> IResultadosAnalisisExterno.ListarAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult<IReadOnlyList<AnalisisGuardado>>([]);
        public Task GuardarAsync(RevisionDocenteRegistrada r, CancellationToken ct) { Revisiones.Add(r); return Task.CompletedTask; }
        Task<IReadOnlyList<RevisionDocenteRegistrada>> IRevisionesDocentes.ListarAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult<IReadOnlyList<RevisionDocenteRegistrada>>(Revisiones);
        public Task<InformeHomologacion?> ObtenerAsync(Guid solicitudId, CancellationToken ct) => Task.FromResult(Informe);
        public Task GuardarAsync(InformeHomologacion informe, CancellationToken ct)
        {
            if (Informe is not null) throw new InformeYaGeneradoException();
            Informe = informe; return Task.CompletedTask;
        }
    }

    private static string Base => $"/api/homologaciones/solicitudes/{Solicitud}";
    private static WebApplicationFactory<Homologacion.Api.Program> Host(MotorFactory f, Fuentes e) => f.WithWebHostBuilder(b =>
    {
        b.UseSetting("AnalisisInterno:Habilitado", "false");
        b.ConfigureServices(s =>
        {
            s.RemoveAll<ISolicitudesAnalisisInterno>(); s.AddSingleton<ISolicitudesAnalisisInterno>(e);
            s.RemoveAll<ICatalogoAnalisisInterno>(); s.AddSingleton<ICatalogoAnalisisInterno>(e);
            s.RemoveAll<IReceptorasAnalisisInterno>(); s.AddSingleton<IReceptorasAnalisisInterno>(e);
            s.RemoveAll<IResultadosAnalisisInterno>(); s.AddSingleton<IResultadosAnalisisInterno>(e);
            s.RemoveAll<IResultadosAnalisisExterno>(); s.AddSingleton<IResultadosAnalisisExterno>(e);
            s.RemoveAll<IRevisionesDocentes>(); s.AddSingleton<IRevisionesDocentes>(e);
            s.RemoveAll<IInformesHomologacion>(); s.AddSingleton<IInformesHomologacion>(e);
            s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(new RelojAnalisis());
        });
    });
    private static HttpClient Docente(MotorFactory f, WebApplicationFactory<Homologacion.Api.Program> host)
    {
        var c = host.CreateClient(); c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente")); return c;
    }
    private static object Ajuste(string decision) => new { asignaturaAprobadaId = Aprobada, asignaturaReceptoraId = Receptora, postura = "Ajuste", decision, observacion = "Observación sintética." };

    [Fact]
    public async Task Genera_consulta_y_cierra_los_analisis_docentes()
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e);
        using var ana = Docente(f, host); using var luis = Docente(f, host);
        Assert.Equal(HttpStatusCode.NotFound, (await ana.GetAsync(Base + "/informe")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ana.PostAsync(Base + "/informe", null)).StatusCode);
        await ana.PutAsJsonAsync(Base + "/revisiones-docentes", Ajuste("Homologable"));
        Assert.Equal(HttpStatusCode.BadRequest, (await ana.PostAsync(Base + "/informe", null)).StatusCode);
        await luis.PutAsJsonAsync(Base + "/revisiones-docentes", Ajuste("NoHomologable"));
        using var r = await luis.PostAsync(Base + "/informe", null);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        using (var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync()))
        {
            Assert.Equal("SinAcuerdo", j.RootElement.GetProperty("parejas")[0].GetProperty("sugerenciaFinal").GetString());
            Assert.Equal(1, j.RootElement.GetProperty("sinAcuerdo").GetInt32());
        }
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync(Base + "/informe")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await ana.PostAsync(Base + "/informe", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await ana.PutAsJsonAsync(Base + "/revisiones-docentes", Ajuste("NoHomologable"))).StatusCode);
    }

    [Fact]
    public async Task Docente_no_asignado_recibe_404()
    {
        using var f = new MotorFactory(); var e = new Fuentes { Asignada = false }; using var host = Host(f, e); using var c = Docente(f, host);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync(Base + "/informe", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Base + "/informe")).StatusCode);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Coordinador", 403)]
    [InlineData("Administrador", 403)]
    public async Task Otros_roles_no_generan_ni_consultan(string? rol, int esperado)
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e); using var c = host.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        Assert.Equal(esperado, (int)(await c.PostAsync(Base + "/informe", null)).StatusCode);
        Assert.Equal(esperado, (int)(await c.GetAsync(Base + "/informe")).StatusCode);
        Assert.Equal(0, e.Consultas);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Application.RevisionDocente;
using Homologacion.Domain.Auditoria;
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
        IRevisionesDocentes, IInformesHomologacion, IAuditoriaInformes, IEstadoSolicitudes, IDecisionesCoordinador,
        IResultadosAgrupacionInforme, IRevisionesAgrupacionInforme
    {
        public int MarcasRemitida { get; private set; }
        public Task MarcarRemitidaAsync(Guid solicitudId, CancellationToken ct) { MarcasRemitida++; return Task.CompletedTask; }
        public int MarcasFinalizada { get; private set; }
        public Task MarcarFinalizadaAsync(Guid solicitudId, CancellationToken ct) { MarcasFinalizada++; return Task.CompletedTask; }
        public List<DecisionCoordinador> Decisiones { get; } = [];
        public Task GuardarAsync(DecisionCoordinador d, CancellationToken ct)
        {
            Decisiones.RemoveAll(x => x.AsignaturaAprobadaId == d.AsignaturaAprobadaId && x.AsignaturaReceptoraId == d.AsignaturaReceptoraId);
            Decisiones.Add(d); return Task.CompletedTask;
        }
        Task<IReadOnlyList<DecisionCoordinador>> IDecisionesCoordinador.ListarAsync(Guid solicitudId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<DecisionCoordinador>>(Decisiones.Where(d => d.SolicitudId == solicitudId).ToArray());
        public bool Asignada { get; set; } = true;
        Task<IReadOnlyList<AnalisisAgrupacionGuardado>> IResultadosAgrupacionInforme.ListarAsync(Guid id,CancellationToken ct)=>Task.FromResult<IReadOnlyList<AnalisisAgrupacionGuardado>>([]);
        Task<IReadOnlyList<RevisionAgrupacionRegistrada>> IRevisionesAgrupacionInforme.ListarAsync(Guid id,CancellationToken ct)=>Task.FromResult<IReadOnlyList<RevisionAgrupacionRegistrada>>([]);
        public int Consultas { get; private set; }
        public List<RevisionDocenteRegistrada> Revisiones { get; } = [];
        public InformeHomologacion? Informe { get; set; }
        public List<EventoInforme> Eventos { get; } = [];
        public Task RegistrarAsync(EventoInforme evento, CancellationToken ct) { Eventos.Add(evento); return Task.CompletedTask; }
        Task<IReadOnlyList<EventoInforme>> IAuditoriaInformes.ListarAsync(Guid solicitudId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EventoInforme>>(Eventos.Where(e => e.SolicitudId == solicitudId).ToArray());
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
            s.RemoveAll<IAuditoriaInformes>(); s.AddSingleton<IAuditoriaInformes>(e);
            s.RemoveAll<IEstadoSolicitudes>(); s.AddSingleton<IEstadoSolicitudes>(e);
            s.RemoveAll<IDecisionesCoordinador>(); s.AddSingleton<IDecisionesCoordinador>(e);
            s.RemoveAll<IResultadosAgrupacionInforme>();s.AddSingleton<IResultadosAgrupacionInforme>(e);
            s.RemoveAll<IRevisionesAgrupacionInforme>();s.AddSingleton<IRevisionesAgrupacionInforme>(e);
            s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(new RelojAnalisis());
        });
    });
    private static HttpClient Docente(MotorFactory f, WebApplicationFactory<Homologacion.Api.Program> host)
    {
        var c = host.CreateClient(); c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente")); return c;
    }
    private static HttpClient Coordinador(MotorFactory f, WebApplicationFactory<Homologacion.Api.Program> host)
    {
        var c = host.CreateClient(); c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Coordinador")); return c;
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

    [Fact]
    public async Task Coordinador_consulta_un_informe_generado_pero_no_puede_generarlo()
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e); using var c = Coordinador(f, host);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Base + "/informe")).StatusCode);
        e.Informe = new InformeHomologacion(Solicitud, "Interno", DateTimeOffset.UtcNow, Guid.NewGuid(), "docente.ana",
            [], [], 0, 0, 0, ["Informe técnico; no es una resolución oficial."]);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync(Base + "/informe")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsync(Base + "/informe", null)).StatusCode);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Administrador", 403)]
    public async Task Otros_roles_no_generan_ni_consultan(string? rol, int esperado)
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e); using var c = host.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        Assert.Equal(esperado, (int)(await c.PostAsync(Base + "/informe", null)).StatusCode);
        Assert.Equal(esperado, (int)(await c.GetAsync(Base + "/informe")).StatusCode);
        Assert.Equal(0, e.Consultas);
    }

    [Fact]
    public async Task HU25_el_coordinador_consulta_el_historial_con_el_evento_generado()
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e);
        using var ana = Docente(f, host); using var luis = Docente(f, host);
        await ana.PutAsJsonAsync(Base + "/revisiones-docentes", Ajuste("Homologable"));
        await luis.PutAsJsonAsync(Base + "/revisiones-docentes", Ajuste("Homologable"));
        Assert.Equal(HttpStatusCode.Created, (await luis.PostAsync(Base + "/informe", null)).StatusCode);
        using var coordinador = host.CreateClient();
        coordinador.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Coordinador"));
        using var r = await coordinador.GetAsync(Base + "/informe/historial");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        var evento = Assert.Single(j.RootElement.EnumerateArray());
        Assert.Equal("Generado", evento.GetProperty("tipo").GetString());
        Assert.Equal("Docente", evento.GetProperty("rol").GetString());
        Assert.False(string.IsNullOrWhiteSpace(evento.GetProperty("usuario").GetString()));
        Assert.True(evento.TryGetProperty("fecha", out _));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await coordinador.DeleteAsync(Base + "/informe/historial")).StatusCode);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Docente", 403)]
    [InlineData("Administrador", 403)]
    public async Task HU25_otros_roles_no_consultan_el_historial(string? rol, int esperado)
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e); using var c = host.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        Assert.Equal(esperado, (int)(await c.GetAsync(Base + "/informe/historial")).StatusCode);
    }

    [Fact]
    public async Task HU32_el_docente_remite_una_sola_vez_y_el_coordinador_ve_el_evento()
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e);
        using var ana = Docente(f, host); using var luis = Docente(f, host);
        // CA3: sin informe generado no se remite.
        using (var sin = await ana.PostAsync(Base + "/informe/remision", null))
        {
            Assert.Equal(HttpStatusCode.BadRequest, sin.StatusCode);
            Assert.Contains("informe", await sin.Content.ReadAsStringAsync());
        }
        await ana.PutAsJsonAsync(Base + "/revisiones-docentes", Ajuste("Homologable"));
        await luis.PutAsJsonAsync(Base + "/revisiones-docentes", Ajuste("NoHomologable"));
        Assert.Equal(HttpStatusCode.Created, (await ana.PostAsync(Base + "/informe", null)).StatusCode);
        using var r = await luis.PostAsync(Base + "/informe/remision", null);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        using (var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync()))
        {
            Assert.Equal(Solicitud, j.RootElement.GetProperty("solicitudId").GetGuid());
            Assert.True(j.RootElement.TryGetProperty("fecha", out _));
            Assert.False(string.IsNullOrWhiteSpace(j.RootElement.GetProperty("remitidoPor").GetString()));
        }
        // CA2: un segundo intento responde 409 sin cambiar la remisión.
        Assert.Equal(HttpStatusCode.Conflict, (await ana.PostAsync(Base + "/informe/remision", null)).StatusCode);
        Assert.Equal(1, e.MarcasRemitida);
        using var coordinador = host.CreateClient();
        coordinador.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Coordinador"));
        using var h = JsonDocument.Parse(await coordinador.GetStringAsync(Base + "/informe/historial"));
        Assert.Equal(["Generado", "Remitido"], h.RootElement.EnumerateArray().Select(x => x.GetProperty("tipo").GetString()));
    }

    [Fact]
    public async Task HU32_docente_no_asignado_recibe_404()
    {
        using var f = new MotorFactory(); var e = new Fuentes { Asignada = false }; using var host = Host(f, e); using var c = Docente(f, host);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync(Base + "/informe/remision", null)).StatusCode);
        Assert.Equal(0, e.MarcasRemitida);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Coordinador", 403)]
    [InlineData("Administrador", 403)]
    public async Task HU32_solo_el_docente_remite(string? rol, int esperado)
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e); using var c = host.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        Assert.Equal(esperado, (int)(await c.PostAsync(Base + "/informe/remision", null)).StatusCode);
        Assert.Equal((0, 0), (e.Consultas, e.MarcasRemitida));
    }

    private static object Decision(string resultado = "NoConvalidar", string justificacion = "No cubre los resultados de aprendizaje.") =>
        new { asignaturaAprobadaId = Aprobada, asignaturaReceptoraId = Receptora, resultado, justificacion };

    private static async Task Remitido(MotorFactory f, WebApplicationFactory<Homologacion.Api.Program> host, string segunda = "NoHomologable")
    {
        using var ana = Docente(f, host); using var luis = Docente(f, host);
        await ana.PutAsJsonAsync(Base + "/revisiones-docentes", Ajuste("Homologable"));
        await luis.PutAsJsonAsync(Base + "/revisiones-docentes", Ajuste(segunda));
        Assert.Equal(HttpStatusCode.Created, (await ana.PostAsync(Base + "/informe", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await luis.PostAsync(Base + "/informe/remision", null)).StatusCode);
    }

    [Fact]
    public async Task HU33_el_coordinador_revisa_decide_y_finaliza_una_sola_vez()
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e);
        using var coordinador = Coordinador(f, host);
        // CA4: sin informe remitido no hay revisión, decisión ni finalización.
        Assert.Equal(HttpStatusCode.NotFound, (await coordinador.GetAsync(Base + "/informe/revision")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await coordinador.PostAsync(Base + "/informe/finalizacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await coordinador.PutAsJsonAsync(Base + "/informe/decisiones", Decision())).StatusCode);
        await Remitido(f, host);

        // CA1: parejas, agente, ambos docentes y la decisión pendiente.
        using (var j = JsonDocument.Parse(await coordinador.GetStringAsync(Base + "/informe/revision")))
        {
            var pareja = j.RootElement.GetProperty("informe").GetProperty("parejas")[0];
            Assert.Equal("SinAcuerdo", pareja.GetProperty("sugerenciaFinal").GetString());
            Assert.Equal(2, pareja.GetProperty("docentes").GetArrayLength());
            Assert.True(pareja.TryGetProperty("agente", out _));
            Assert.Equal(1, j.RootElement.GetProperty("decisionesPendientes").GetInt32());
            Assert.Equal(JsonValueKind.Null, j.RootElement.GetProperty("finalizacion").ValueKind);
        }
        // CA2: no se finaliza mientras falte la decisión en «Sin acuerdo».
        Assert.Equal(HttpStatusCode.BadRequest, (await coordinador.PostAsync(Base + "/informe/finalizacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await coordinador.PutAsJsonAsync(Base + "/informe/decisiones", Decision(justificacion: " "))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await coordinador.PutAsJsonAsync(Base + "/informe/decisiones", Decision(resultado: "Homologable"))).StatusCode);
        using (var d = await coordinador.PutAsJsonAsync(Base + "/informe/decisiones", Decision()))
        {
            Assert.Equal(HttpStatusCode.OK, d.StatusCode);
            using var j = JsonDocument.Parse(await d.Content.ReadAsStringAsync());
            Assert.Equal("NoConvalidar", j.RootElement.GetProperty("resultado").GetString());
            Assert.Equal("usuario-sintetico", j.RootElement.GetProperty("coordinador").GetString());
            Assert.True(j.RootElement.TryGetProperty("fecha", out _));
        }
        // CA3: finaliza con fecha y coordinador; después no admite cambios.
        using (var fin = await coordinador.PostAsync(Base + "/informe/finalizacion", null))
        {
            Assert.Equal(HttpStatusCode.OK, fin.StatusCode);
            using var j = JsonDocument.Parse(await fin.Content.ReadAsStringAsync());
            Assert.Equal(Solicitud, j.RootElement.GetProperty("solicitudId").GetGuid());
            Assert.Equal("usuario-sintetico", j.RootElement.GetProperty("finalizadoPor").GetString());
        }
        Assert.Equal(HttpStatusCode.Conflict, (await coordinador.PostAsync(Base + "/informe/finalizacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await coordinador.PutAsJsonAsync(Base + "/informe/decisiones", Decision("Convalidar"))).StatusCode);
        Assert.Equal(1, e.MarcasFinalizada);
        using var h = JsonDocument.Parse(await coordinador.GetStringAsync(Base + "/informe/historial"));
        Assert.Equal(["Generado", "Remitido", "Finalizado"], h.RootElement.EnumerateArray().Select(x => x.GetProperty("tipo").GetString()));
        Assert.Equal("Coordinador", h.RootElement[2].GetProperty("rol").GetString());
        using var r = JsonDocument.Parse(await coordinador.GetStringAsync(Base + "/informe/revision"));
        Assert.Equal("NoConvalidar", r.RootElement.GetProperty("decisiones")[0].GetProperty("resultado").GetString());
        Assert.Equal(JsonValueKind.Object, r.RootElement.GetProperty("finalizacion").ValueKind);
    }

    [Fact]
    public async Task HU33_con_acuerdo_en_todas_las_parejas_se_finaliza_sin_decisiones()
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e);
        await Remitido(f, host, segunda: "Homologable");
        using var coordinador = Coordinador(f, host);
        Assert.Equal(HttpStatusCode.BadRequest, (await coordinador.PutAsJsonAsync(Base + "/informe/decisiones", Decision())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await coordinador.PostAsync(Base + "/informe/finalizacion", null)).StatusCode);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Docente", 403)]
    [InlineData("Administrador", 403)]
    public async Task HU33_solo_el_coordinador_revisa_decide_y_finaliza(string? rol, int esperado)
    {
        using var f = new MotorFactory(); var e = new Fuentes(); using var host = Host(f, e);
        await Remitido(f, host);
        using var c = host.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        Assert.Equal(esperado, (int)(await c.GetAsync(Base + "/informe/revision")).StatusCode);
        Assert.Equal(esperado, (int)(await c.PutAsJsonAsync(Base + "/informe/decisiones", Decision())).StatusCode);
        Assert.Equal(esperado, (int)(await c.PostAsync(Base + "/informe/finalizacion", null)).StatusCode);
        Assert.Empty(e.Decisiones);
        Assert.Equal(0, e.MarcasFinalizada);
    }
}

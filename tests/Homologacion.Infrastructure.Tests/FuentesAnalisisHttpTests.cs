using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Homologacion.Infrastructure.Tests;

public class FuentesAnalisisHttpTests
{
    private static readonly Guid Id = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private sealed class Capture(string body = "{}", HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
    private static FuentesAnalisisHttp Crear(HttpClient http) => new(http, Options.Create(new ConfiguracionAnalisisInterno()));
    [Fact]
    public async Task Composicion_del_modelo_no_transporta_Bearer_ni_clave_interna()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InternalAccess:Key"] = "clave-sintetica-servidor-hu18-32-bytes",
            ["AnalisisInterno:Habilitado"] = "true",
            ["AnalisisInterno:OllamaUrl"] = "http://ollama.test/",
            ["AnalisisInterno:Modelo"] = "modelo-sintetico"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(cfg);
        services.AddLogging();
        services.AddAnalisisInterno();
        using var capture = new Capture("""{"done":true,"message":{"role":"assistant","content":"{\"resumen\":\"Explicacion sintetica.\",\"evidenciasCitadas\":[\"C1\"]}"}}""");
        services.AddHttpClient("hu18-ollama").ConfigurePrimaryHttpMessageHandler(() => capture);
        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer actor-sintetico";
        context.Request.Headers["X-Internal-Key"] = "clave-del-cliente";
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IAgenteAnalisisInterno>().JustificarAsync(OllamaAgenteInternoTests.Contexto(), default);
        Assert.NotNull(capture.Request);
        Assert.Null(capture.Request.Headers.Authorization);
        Assert.False(capture.Request.Headers.Contains("X-Internal-Key"));
    }
    [Fact]
    public async Task Lee_contexto_autorizado_sin_incorporar_datos_personales()
    {
        var body = JsonSerializer.Serialize(new { id = Id, tipoCambio = "Interno", carreraOrigenId = Guid.NewGuid(), carreraReceptoraId = Guid.NewGuid(), asignaturasAprobadas = Array.Empty<object>(), nombreEstudiante = "No transportar al modelo" });
        using var handler = new Capture(body); using var http = new HttpClient(handler);
        var r = await Crear(http).ObtenerSolicitudAsync(Id, default);
        Assert.Equal(Id, r!.Id);
        Assert.Equal($"/api/solicitudes/{Id}/contexto-analisis-interno", handler.Request!.RequestUri!.AbsolutePath);
        Assert.DoesNotContain("nombreEstudiante", JsonSerializer.Serialize(r));
    }
    [Fact]
    public async Task No_encontrada_se_conserva_como_null()
    {
        using var handler = new Capture("", HttpStatusCode.NotFound); using var http = new HttpClient(handler);
        Assert.Null(await Crear(http).ObtenerSolicitudAsync(Id, default));
    }
    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    public async Task Fuente_no_verificable_falla_cerrada_sin_filtrar_diagnostico(int status)
    {
        using var handler = new Capture("diagnostico-privado-sintetico", (HttpStatusCode)status); using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Crear(http).ObtenerSolicitudAsync(Id, default));
        Assert.DoesNotContain("diagnostico-privado", error.Message);
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("{")]
    [InlineData("""{"id":"20000000-0000-0000-0000-000000000002","asignaturasAprobadas":[]}""")]
    public async Task Rechaza_JSON_incompleto_y_respuesta_de_otra_solicitud(string body)
    {
        using var handler = new Capture(body); using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => Crear(http).ObtenerSolicitudAsync(Id, default));
    }
    [Fact]
    public async Task Usa_Bearer_del_actor_y_clave_del_servidor_sin_reenviar_la_del_cliente()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.Request.Headers.Authorization = "Bearer actor-sintetico";
        accessor.HttpContext.Request.Headers["X-Internal-Key"] = "valor-del-cliente";
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["InternalAccess:Key"] = "clave-sintetica-de-servidor-hu18-32-bytes" }).Build();
        using var capture = new Capture(); using var auth = new AutorizacionFuentesAnalisisHandler(accessor, cfg) { InnerHandler = capture };
        using var http = new HttpClient(auth);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://solicitudes.test/");
        request.Headers.Add("X-Internal-Key", "valor-no-confiable");
        await http.SendAsync(request);
        Assert.Equal("Bearer actor-sintetico", capture.Request!.Headers.Authorization!.ToString());
        Assert.Equal("clave-sintetica-de-servidor-hu18-32-bytes", capture.Request.Headers.GetValues("X-Internal-Key").Single());
    }
    [Fact]
    public async Task No_hay_bypass_sin_identidad_del_actor()
    {
        using var capture = new Capture();
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["InternalAccess:Key"] = "clave-sintetica-de-servidor-hu18-32-bytes" }).Build();
        using var auth = new AutorizacionFuentesAnalisisHandler(new HttpContextAccessor(), cfg) { InnerHandler = capture };
        using var http = new HttpClient(auth);
        await Assert.ThrowsAsync<FuenteAnalisisNoDisponibleException>(() => http.GetAsync("http://solicitudes.test/"));
        Assert.Null(capture.Request);
    }
    [Theory]
    [InlineData("url")]
    [InlineData("modelo")]
    [InlineData("timeout")]
    public void Configuracion_insegura_o_incompleta_se_rechaza_sin_exponer_valores(string caso)
    {
        var cfg = new ConfiguracionAnalisisInterno { Habilitado = true, OllamaUrl = "http://ollama.test/", Modelo = "modelo-sintetico" };
        if (caso == "url") cfg.CatalogoUrl = "http://usuario:secreto-sintetico@host/";
        if (caso == "modelo") cfg.Modelo = "";
        if (caso == "timeout") cfg.TimeoutSegundos = 0;
        var errors = cfg.Validate(new ValidationContext(cfg)).ToArray();
        Assert.NotEmpty(errors);
        Assert.All(errors, error => Assert.DoesNotContain("secreto-sintetico", error.ErrorMessage!));
    }
}

using System.Net;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Xunit;

namespace Gateway.Tests;

/// <summary>HU-19: rutas públicas del análisis externo; el contexto interno de Solicitudes no se publica.</summary>
public class AnalisisExternoRouteTests
{
    private static async Task<HttpClient> Autenticado(GatewayFactory f)
    {
        var c = f.CreateClient();
        using var login = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin-gateway", GatewayFactory.Password));
        var session = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        c.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        return c;
    }

    [Theory]
    [InlineData("POST", "/api/homologaciones/solicitudes/{0}/analisis-externo", "homologacion")]
    [InlineData("GET", "/api/homologaciones/solicitudes/{0}/analisis-externo", "homologacion")]
    [InlineData("GET", "/api/homologaciones/solicitudes/{0}/analisis-externo/opciones", "homologacion")]
    [InlineData("GET", "/api/homologaciones/solicitudes/{0}/requisitos-externos", "homologacion")]
    [InlineData("PUT", "/api/solicitudes/{0}/datos-cambio-externo", "solicitudes")]
    [InlineData("PUT", "/api/homologaciones/solicitudes/{0}/revisiones-docentes", "homologacion")]
    [InlineData("GET", "/api/homologaciones/solicitudes/{0}/revisiones-docentes", "homologacion")]
    [InlineData("POST", "/api/homologaciones/solicitudes/{0}/informe", "homologacion")]
    [InlineData("GET", "/api/homologaciones/solicitudes/{0}/informe", "homologacion")]
    [InlineData("GET", "/api/homologaciones/solicitudes/{0}/informe/historial", "homologacion")]
    [InlineData("POST", "/api/homologaciones/solicitudes/{0}/informe/remision", "homologacion")]
    // HU-33: revisión del coordinador, decisión en «Sin acuerdo» y finalización.
    [InlineData("GET", "/api/homologaciones/solicitudes/{0}/informe/revision", "homologacion")]
    [InlineData("PUT", "/api/homologaciones/solicitudes/{0}/informe/decisiones", "homologacion")]
    [InlineData("POST", "/api/homologaciones/solicitudes/{0}/informe/finalizacion", "homologacion")]
    // HU-12: adjuntos del expediente (subir, listar y descargar).
    [InlineData("POST", "/api/solicitudes/{0}/adjuntos", "solicitudes")]
    [InlineData("GET", "/api/solicitudes/{0}/adjuntos", "solicitudes")]
    [InlineData("GET", "/api/solicitudes/{0}/adjuntos/3f2c1b7e-0000-4000-8000-000000000012", "solicitudes")]
    public async Task Enruta_al_servicio_dueno_conservando_Bearer(string metodo, string plantilla, string servicio)
    {
        using var f = new GatewayFactory(); using var c = await Autenticado(f);
        var path = string.Format(plantilla, Guid.NewGuid());
        using var request = new HttpRequestMessage(new HttpMethod(metodo), path);
        if (metodo != "GET") request.Content = JsonContent.Create(new { });
        using var response = await c.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var forwarded = f.Forwarded.Last();
        Assert.Equal(servicio, forwarded.Service);
        Assert.Equal(path, forwarded.PathAndQuery);
        Assert.Equal(c.DefaultRequestHeaders.Authorization!.ToString(), forwarded.Authorization);
    }

    [Fact]
    public async Task Contexto_externo_de_Solicitudes_no_se_publica()
    {
        using var f = new GatewayFactory(); using var c = await Autenticado(f);
        using var response = await c.GetAsync($"/api/solicitudes/{Guid.NewGuid()}/contexto-analisis-externo");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(f.Forwarded, x => x.PathAndQuery.Contains("contexto-analisis-externo", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HU32_la_remision_interna_de_Solicitudes_no_se_publica()
    {
        // Solo Homologación cambia el estado a «Remitida», después de comprobar el informe.
        using var f = new GatewayFactory(); using var c = await Autenticado(f);
        using var response = await c.PostAsync($"/api/solicitudes/{Guid.NewGuid()}/remision", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(f.Forwarded, x => x.PathAndQuery.EndsWith("/remision", StringComparison.Ordinal) && x.Service == "solicitudes");
    }

    [Fact]
    public async Task HU33_la_finalizacion_interna_de_Solicitudes_no_se_publica()
    {
        using var f = new GatewayFactory(); using var c = await Autenticado(f);
        using var response = await c.PostAsync($"/api/solicitudes/{Guid.NewGuid()}/finalizacion", JsonContent.Create(new { }));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(f.Forwarded, x => x.PathAndQuery.EndsWith("/finalizacion", StringComparison.Ordinal) && x.Service == "solicitudes");
    }

    [Fact]
    public async Task Sin_token_no_se_reenvia()
    {
        using var f = new GatewayFactory(); using var c = f.CreateClient();
        using var response = await c.GetAsync($"/api/homologaciones/solicitudes/{Guid.NewGuid()}/requisitos-externos");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(f.Forwarded);
    }
}

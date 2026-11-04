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
    public async Task Sin_token_no_se_reenvia()
    {
        using var f = new GatewayFactory(); using var c = f.CreateClient();
        using var response = await c.GetAsync($"/api/homologaciones/solicitudes/{Guid.NewGuid()}/requisitos-externos");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(f.Forwarded);
    }
}

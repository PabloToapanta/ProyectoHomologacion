using System.Net;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Xunit;

namespace Gateway.Tests;

public class AnalisisInternoRouteTests
{
    [Fact]
    public async Task Enruta_el_analisis_a_Homologacion_conservando_Bearer()
    {
        using var f = new GatewayFactory(); using var c = f.CreateClient();
        using var login = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin-gateway", GatewayFactory.Password));
        var session = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        c.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        var path = $"/api/homologaciones/solicitudes/{Guid.NewGuid()}/analisis-interno";
        using var response = await c.PostAsJsonAsync(path, new { asignaturaAprobadaId = Guid.NewGuid(), asignaturaReceptoraId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var forwarded = f.Forwarded.Last();
        Assert.Equal("homologacion", forwarded.Service);
        Assert.Equal(path, forwarded.PathAndQuery);
        Assert.Equal(c.DefaultRequestHeaders.Authorization.ToString(), forwarded.Authorization);
    }
}

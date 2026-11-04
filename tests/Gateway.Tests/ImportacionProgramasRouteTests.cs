using System.Net;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Xunit;

namespace Gateway.Tests;

/// <summary>HU-08: rutas de carga de programas analíticos hacia Catálogo.</summary>
public class ImportacionProgramasRouteTests
{
    [Theory]
    [InlineData("POST", "/api/importaciones-programas/analisis")]
    [InlineData("POST", "/api/importaciones-programas/confirmacion")]
    [InlineData("GET", "/api/asignaturas/{0}/programa-analitico")]
    public async Task Enruta_a_Catalogo_conservando_Bearer(string metodo, string plantilla)
    {
        using var f = new GatewayFactory(); using var c = f.CreateClient();
        using var login = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin-gateway", GatewayFactory.Password));
        c.DefaultRequestHeaders.Authorization = new("Bearer", (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken);
        var path = string.Format(plantilla, Guid.NewGuid());
        using var request = new HttpRequestMessage(new HttpMethod(metodo), path);
        if (metodo == "POST") request.Content = new MultipartFormDataContent { { new ByteArrayContent("%PDF-1.4"u8.ToArray()), "archivos", "plan.pdf" } };
        using var response = await c.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var forwarded = f.Forwarded.Last();
        Assert.Equal("catalogo", forwarded.Service);
        Assert.Equal(path, forwarded.PathAndQuery);
        Assert.Equal(c.DefaultRequestHeaders.Authorization.ToString(), forwarded.Authorization);
    }
}

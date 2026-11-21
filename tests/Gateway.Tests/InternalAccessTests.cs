using System.Net;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Gateway.Tests;

public class InternalAccessTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("valor-del-navegador-no-confiable")]
    public async Task Gateway_inyecta_su_clave_y_reemplaza_la_del_cliente(string? clientKey)
    {
        using var factory = new GatewayFactory();
        using var client = factory.CreateClient();
        if (clientKey is not null) client.DefaultRequestHeaders.Add("X-Internal-Key", clientKey);
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin-gateway", GatewayFactory.Password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(GatewayFactory.InternalKey, factory.Forwarded.Last().InternalKey);
        Assert.DoesNotContain(GatewayFactory.InternalKey, await response.Content.ReadAsStringAsync());
        Assert.False(response.Headers.Contains("X-Internal-Key"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("secreto-corto-sintetico")]
    public void Gateway_no_arranca_sin_clave_valida(string? key)
    {
        using var factory = new GatewayFactory();
        using var invalid = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["InternalAccess:Key"] = key })));
        var error = Assert.Throws<InvalidOperationException>(() => invalid.CreateClient());
        Assert.Contains("InternalAccess:Key", error.Message);
        if (key is not null) Assert.DoesNotContain(key, error.Message);
    }
}

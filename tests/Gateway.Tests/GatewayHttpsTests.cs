using System.Net;
using System.Net.Http.Json;
using Auth.Api.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gateway.Tests;

public sealed class GatewayHttpsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("http://localhost")]
    [InlineData("https://localhost/ruta")]
    [InlineData("https://usuario:clave@localhost")]
    public void OrigenPublicoInvalido_ImpideHabilitarHttps(string origin)
    {
        using var factory = new GatewayFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Https:Enabled"] = "true", ["Https:PublicOrigin"] = origin })));

        Assert.Throws<OptionsValidationException>(() => configured.CreateClient());
    }

    [Theory]
    [InlineData("/", "frontend")]
    [InlineData("/login", "frontend")]
    [InlineData("/panel", "frontend")]
    [InlineData("/admin/carreras?seccion=asignaturas", "frontend")]
    [InlineData("/validaciones", "frontend")]
    [InlineData("/solicitudes", "frontend")]
    [InlineData("/docente/solicitudes", "frontend")]
    [InlineData("/assets/index-demo.js", "frontend")]
    public async Task RutasDeInterfaz_SeSirvenPorGatewayHttps(string path, string destination)
    {
        using var factory = new GatewayFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Hu34"));
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost") });

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(destination, factory.Forwarded.Last().Service);
    }

    [Fact]
    public async Task ApiDesconocida_NoSeReenviaAlFrontend()
    {
        using var factory = new GatewayFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Hu34"));
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost") });

        using var response = await client.GetAsync("/api/desconocida");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.Forwarded);
    }

    [Theory]
    [InlineData("/login", "https://localhost/login")]
    [InlineData("/api/carreras?pagina=2", "https://localhost/api/carreras?pagina=2")]
    public async Task GetHttp_RedirigeALocalhostHttpsSinContactarServicios(string path, string expectedLocation)
    {
        using var factory = new GatewayFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Https:Enabled"] = "true", ["Https:PublicOrigin"] = "https://localhost" })));
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost")
        });

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
        Assert.Equal(expectedLocation, response.Headers.Location?.ToString());
        Assert.Empty(factory.Forwarded);
    }

    [Fact]
    public async Task LoginHttp_SeRechazaSinContactarAuth()
    {
        using var factory = new GatewayFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Https:Enabled"] = "true", ["Https:PublicOrigin"] = "https://localhost" })));
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost")
        });

        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("usuario-sintetico", "clave-sintetica"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(factory.Forwarded);
    }

    [Fact]
    public async Task RespuestaHttps_IncluyeHstsEnEntornoDeIntegracion()
    {
        using var factory = new GatewayFactory();
        using var configured = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Staging");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Https:Enabled"] = "true", ["Https:PublicOrigin"] = "https://localhost" }));
        });
        using var client = configured.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/api/carreras");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Strict-Transport-Security", out var values));
        Assert.Contains("max-age=", Assert.Single(values));
    }
}

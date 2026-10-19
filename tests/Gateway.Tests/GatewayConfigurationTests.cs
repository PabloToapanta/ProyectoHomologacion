using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gateway.Tests;

public sealed class GatewayConfigurationTests
{
    [Theory]
    [InlineData("Jwt:Issuer", "")]
    [InlineData("Jwt:Audience", " ")]
    [InlineData("Jwt:SigningKey", "corta")]
    public void ConfiguracionJwtInsegura_ImpideArranque(string key, string value)
    {
        using var factory = new GatewayFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })));
        Assert.Throws<OptionsValidationException>(() => configured.CreateClient());
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("ftp://destino.test/")]
    [InlineData("http://usuario:secreto-sintetico@destino.test/")]
    [InlineData("http://destino.test/?token=secreto-sintetico")]
    [InlineData("http://destino.test/#fragmento")]
    public void DestinosInvalidos_SeRechazanSinExponerSuValor(string address)
    {
        var values = new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:auth:Destinations:primary:Address"] = address,
            ["ReverseProxy:Clusters:catalogo:Destinations:primary:Address"] = "http://catalogo.test/",
            ["ReverseProxy:Clusters:homologacion:Destinations:primary:Address"] = "https://homologacion.test/"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var error = Assert.Throws<OptionsValidationException>(() => GatewayConfiguration.ValidateDestinations(config));
        Assert.DoesNotContain("secreto-sintetico", error.Message);
    }

    [Fact]
    public void ClusterSinDestino_SeRechaza()
    {
        Assert.Throws<OptionsValidationException>(() => GatewayConfiguration.ValidateDestinations(new ConfigurationBuilder().Build()));
    }

    [Fact]
    public void DestinosExternosHttpOHttps_SeAceptan()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:auth:Destinations:primary:Address"] = "http://auth.test/",
            ["ReverseProxy:Clusters:catalogo:Destinations:primary:Address"] = "https://catalogo.test/",
            ["ReverseProxy:Clusters:homologacion:Destinations:primary:Address"] = "http://homologacion.test:8080/"
        }).Build();
        GatewayConfiguration.ValidateDestinations(config);
    }

    [Fact]
    public async Task RutaSinPoliticaExplicita_ContinuaProtegidaPorFallback()
    {
        using var factory = new GatewayFactory();
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            { ["ReverseProxy:Routes:carreras:AuthorizationPolicy"] = null })));
        using var client = configured.CreateClient();
        var response = await client.GetAsync("/api/carreras");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.Forwarded);
    }
    [Fact]
    public void Gateway_NoReferenciaProyectosDeNegocio()
    {
        Assert.DoesNotContain(typeof(Gateway.Program).Assembly.GetReferencedAssemblies(),
            reference => reference.Name!.StartsWith("Auth.", StringComparison.Ordinal));
    }
}

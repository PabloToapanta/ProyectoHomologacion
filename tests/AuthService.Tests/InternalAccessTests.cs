using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AuthService.Tests;

public class InternalAccessTests
{
    // Sintética: no se utiliza en despliegues.
    private const string Key = "hu36-test-only-internal-key-64-characters-for-isolated-tests";

    [Theory]
    [InlineData(null)]
    [InlineData("incorrecta")]
    [InlineData("otra-clave-valida-en-longitud-pero-no-autorizada")]
    public async Task Clave_ausente_o_incorrecta_impide_acceso_incluso_a_Swagger(string? key)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Remove("X-Internal-Key");
        if (key is not null) client.DefaultRequestHeaders.Add("X-Internal-Key", key);
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Key, body);
        if (key is not null) Assert.DoesNotContain(key, body);
    }

    [Fact]
    public async Task Cabecera_repetida_no_se_acepta()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Remove("X-Internal-Key");
        client.DefaultRequestHeaders.Add("X-Internal-Key", new[] { Key, Key });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("secreto-corto-sintetico")]
    [InlineData("clave con espacio en el medio no valida")]
    public void Configuracion_invalida_impide_arranque_sin_filtrar_el_valor(string? key)
    {
        using var factory = new AuthApiFactory();
        using var invalid = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["InternalAccess:Key"] = key })));
        var error = Assert.Throws<InvalidOperationException>(() => invalid.CreateClient());
        Assert.Contains("InternalAccess:Key", error.Message);
        if (!string.IsNullOrEmpty(key)) Assert.DoesNotContain(key, error.Message);
    }
}

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Homologacion.Infrastructure.AnalisisInterno;
using Xunit;

namespace Homologacion.Api.Tests;

public class InternalAccessTests
{
    // Sintética: no se utiliza en despliegues.
    private const string Key = "hu36-test-only-internal-key-64-characters-for-isolated-tests";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("secreto-corto-sintetico")]
    public void Clave_invalida_se_rechaza_antes_de_resolver_la_base_para_migraciones(string? key)
    {
        var resoluciones = 0;
        using var factory = new MotorFactory();
        using var invalid = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalAccess:Key"] = key,
                ["Database:ApplyMigrations"] = "true"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ResultadosDbContext>();
                services.AddScoped<ResultadosDbContext>(_ =>
                {
                    resoluciones++;
                    throw new InvalidOperationException("No debe resolverse SQL antes de validar seguridad.");
                });
            });
        });
        var error = Assert.Throws<InvalidOperationException>(() => invalid.CreateClient());
        Assert.Contains("Configure InternalAccess:Key", error.Message);
        Assert.Equal(0, resoluciones);
        if (!string.IsNullOrEmpty(key)) Assert.DoesNotContain(key, error.Message);
    }

    [Fact]
    public void Jwt_invalido_se_rechaza_antes_de_resolver_la_base_para_migraciones()
    {
        var resoluciones = 0;
        using var factory = new MotorFactory();
        using var invalid = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "",
                ["Database:ApplyMigrations"] = "true"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ResultadosDbContext>();
                services.AddScoped<ResultadosDbContext>(_ =>
                {
                    resoluciones++;
                    throw new InvalidOperationException("No debe resolverse SQL antes de validar seguridad.");
                });
            });
        });
        Assert.Throws<OptionsValidationException>(() => invalid.CreateClient());
        Assert.Equal(0, resoluciones);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("incorrecta")]
    [InlineData("otra-clave-valida-en-longitud-pero-no-autorizada")]
    public async Task Clave_ausente_o_incorrecta_impide_acceso_incluso_a_Swagger(string? key)
    {
        using var factory = new MotorFactory();
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
        using var factory = new MotorFactory();
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
        using var factory = new MotorFactory();
        using var invalid = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["InternalAccess:Key"] = key })));
        var error = Assert.Throws<InvalidOperationException>(() => invalid.CreateClient());
        Assert.Contains("InternalAccess:Key", error.Message);
        if (!string.IsNullOrEmpty(key)) Assert.DoesNotContain(key, error.Message);
    }
}

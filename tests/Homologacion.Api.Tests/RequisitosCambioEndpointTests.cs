using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Homologacion.Application.RequisitosCambio;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Homologacion.Api.Tests;

public class RequisitosCambioEndpointTests
{
    private const string Ruta = "/api/homologaciones/validar-requisitos";

    [Theory]
    [InlineData("Interno", 1, 1, 0, true)]
    [InlineData("Interno", 1, 0, 0, false)]
    [InlineData("Interno", 0, 1, 0, false)]
    [InlineData("Externo", 2, 2, 0, true)]
    [InlineData("Externo", 2, 1, 0, false)]
    [InlineData("Externo", 1, 2, 0, false)]
    [InlineData("Externo", 2, 2, 1, false)]
    public async Task Criterios_mediante_HTTP(string tipo, int periodos, int homologables, int cambios, bool esperado)
    {
        using var tokens = new MotorFactory();
        using var factory = tokens.WithWebHostBuilder(builder => builder.ConfigureServices(
            services => services.AddScoped<ValidarRequisitosCambio>()));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.Token());
        var response = await client.PostAsJsonAsync(Ruta, new { tipoCambio = tipo,
            periodosCursados = periodos, asignaturasHomologables = homologables, cambiosPrevios = cambios });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(esperado, json.GetProperty("cumple").GetBoolean());
        Assert.Equal(esperado, json.GetProperty("incumplimientos").GetArrayLength() == 0);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"tipoCambio\":\"Interno\",\"periodosCursados\":1,\"asignaturasHomologables\":1}")]
    [InlineData("{\"tipoCambio\":\"Interno\",\"periodosCursados\":-1,\"asignaturasHomologables\":1,\"cambiosPrevios\":0}")]
    [InlineData("{\"tipoCambio\":\"Interno\",\"periodosCursados\":1,\"asignaturasHomologables\":-1,\"cambiosPrevios\":0}")]
    [InlineData("{\"tipoCambio\":\"Interno\",\"periodosCursados\":1,\"asignaturasHomologables\":1,\"cambiosPrevios\":-1}")]
    [InlineData("{\"tipoCambio\":\"Otro\",\"periodosCursados\":1,\"asignaturasHomologables\":1,\"cambiosPrevios\":0}")]
    [InlineData("{\"tipoCambio\":0,\"periodosCursados\":1,\"asignaturasHomologables\":1,\"cambiosPrevios\":0}")]
    [InlineData("{\"tipoCambio\":\"Interno\",\"periodosCursados\":1,\"asignaturasHomologables\":1,\"cambiosPrevios\":0,\"cumple\":true}")]
    public async Task Datos_invalidos_o_incompletos_producen_400(string body)
    {
        using var tokens = new MotorFactory();
        using var factory = tokens.WithWebHostBuilder(builder => builder.ConfigureServices(
            services => services.AddScoped<ValidarRequisitosCambio>()));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Token());
        var response = await client.PostAsync(Ruta, new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Otro", HttpStatusCode.Forbidden)]
    public async Task Politica_JWT_se_conserva(string? rol, HttpStatusCode esperado)
    {
        using var factory = new MotorFactory(); using var client = factory.CreateClient();
        if (rol is not null) client.DefaultRequestHeaders.Authorization = new("Bearer", factory.Token(rol));
        Assert.Equal(esperado, (await client.PostAsJsonAsync(Ruta, new { })).StatusCode);
    }

    [Fact]
    public async Task Swagger_documenta_endpoint_y_respuestas()
    {
        using var factory = new MotorFactory(); using var client = factory.CreateClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var responses = doc.GetProperty("paths").GetProperty(Ruta).GetProperty("post").GetProperty("responses");
        foreach (var codigo in new[] { "200", "400", "401", "403" })
            Assert.True(responses.TryGetProperty(codigo, out _));
    }
}

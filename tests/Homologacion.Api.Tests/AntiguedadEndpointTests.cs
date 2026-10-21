using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Homologacion.Application.Antiguedad;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Homologacion.Api.Tests;

// Registro aislado para verificar la propuesta sin editar Program.cs de Pablo.
public sealed class AntiguedadFactory : IDisposable
{
    private readonly MotorFactory tokens = new();
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Homologacion.Api.Program> factory;

    public AntiguedadFactory()
    {
        factory = tokens.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddScoped<ValidarAntiguedad>()));
    }

    public HttpClient CreateClient() => factory.CreateClient();
    public HttpClient Autenticado()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Token());
        return client;
    }
    public void Dispose() { factory.Dispose(); tokens.Dispose(); }
}

public class AntiguedadEndpointTests
{
    private const string Ruta = "/api/homologaciones/validar-antiguedad";

    [Theory]
    [InlineData(0, HttpStatusCode.OK)]
    [InlineData(1, HttpStatusCode.BadRequest)]
    public async Task Fecha_evaluacion_no_supera_hoy_en_Ecuador(int dias, HttpStatusCode esperado)
    {
        var hoy = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).DateTime);
        using var factory = new AntiguedadFactory(); using var client = factory.Autenticado();
        var response = await client.PostAsJsonAsync(Ruta, new { estadoAsignatura = "Aprobada",
            fechaAprobacion = hoy.AddYears(-1).ToString("yyyy-MM-dd"), fechaEvaluacion = hoy.AddDays(dias).ToString("yyyy-MM-dd") });
        Assert.Equal(esperado, response.StatusCode);
    }

    [Theory]
    [InlineData("Aprobada", "2017-10-20", "Elegible")]
    [InlineData("Aprobada", "2016-10-20", "Elegible")]
    [InlineData("Aprobada", "2016-10-19", "RequiereValidacionConocimientos")]
    [InlineData("Reprobada", null, "NoElegible")]
    [InlineData("NoCursada", null, "NoElegible")]
    public async Task Criterios_por_HTTP(string estado, string? fecha, string esperado)
    {
        using var factory = new AntiguedadFactory(); using var client = factory.Autenticado();
        var response = await client.PostAsJsonAsync(Ruta, new { estadoAsignatura = estado,
            fechaAprobacion = fecha, fechaEvaluacion = "2026-10-20" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(esperado, json.GetProperty("estado").GetString());
        Assert.NotEmpty(json.GetProperty("motivo").GetString()!);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"estadoAsignatura\":\"Aprobada\",\"fechaEvaluacion\":\"2026-10-20\"}")]
    [InlineData("{\"estadoAsignatura\":\"Aprobada\",\"fechaAprobacion\":\"2026-10-21\",\"fechaEvaluacion\":\"2026-10-20\"}")]
    [InlineData("{\"estadoAsignatura\":\"Desconocido\",\"fechaEvaluacion\":\"2026-10-20\"}")]
    [InlineData("{\"estadoAsignatura\":0,\"fechaEvaluacion\":\"2026-10-20\"}")]
    [InlineData("{\"estadoAsignatura\":\"Aprobada\",\"fechaAprobacion\":\"20/10/2016\",\"fechaEvaluacion\":\"2026-10-20\"}")]
    public async Task Datos_invalidos_no_generan_decision_academica(string body)
    {
        using var factory = new AntiguedadFactory(); using var client = factory.Autenticado();
        var response = await client.PostAsync(Ruta, new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Endpoint_conserva_proteccion_JWT()
    {
        using var factory = new AntiguedadFactory(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync(Ruta, new { })).StatusCode);
    }

    [Fact]
    public async Task Swagger_incluye_contrato()
    {
        using var factory = new AntiguedadFactory(); using var client = factory.CreateClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.True(doc.GetProperty("paths").TryGetProperty(Ruta, out _));
    }
}

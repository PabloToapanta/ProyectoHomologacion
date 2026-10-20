using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Yarp.ReverseProxy.Forwarder;

namespace Homologacion.Api.Tests;

public sealed class MotorFactory(decimal umbral = 80) : WebApplicationFactory<Homologacion.Api.Program>
{
    public const string Issuer = "hu14-tests";
    public const string Audience = "hu14-api";
    public string Key { get; } = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer, ["Jwt:Audience"] = Audience, ["Jwt:SigningKey"] = Key,
            ["Equivalencia:UmbralPct"] = umbral.ToString(System.Globalization.CultureInfo.InvariantCulture)
        }));
    }
    public string Token(string rol = "Administrador", string? issuer = null, bool expirado = false,
        string? audience = null, string? key = null, string algorithm = SecurityAlgorithms.HmacSha256)
    {
        var now = DateTime.UtcNow;
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer ?? Issuer, audience ?? Audience,
            [new Claim("sub", Guid.NewGuid().ToString()), new Claim("name", "usuario-sintetico"), new Claim("role", rol)],
            now.AddMinutes(-10), expirado ? now.AddMinutes(-1) : now.AddMinutes(10),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? Key)), algorithm)));
    }
    public HttpClient Autenticado(string rol = "Administrador")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(rol));
        return client;
    }
}

public class EvaluarEndpointTests
{
    public const string Ruta = "/api/homologaciones/evaluar";
    public static object Datos() => new
    {
        origenes = new[] { new { id = "O", aprobada = true, planAnalitico = "plan-sintetico-O-v1",
            horas = 80, unidadHoras = "hora-reloj", creditos = 8, sistemaCreditos = "sistema-sintetico-A",
            contenidos = Enumerable.Range(1, 10).Select(i => "C" + i), resultadosAprendizaje = Enumerable.Range(1, 5).Select(i => "R" + i) } },
        receptora = new { id = "D", planAnalitico = "plan-sintetico-D-v1", horas = 100, unidadHoras = "hora-reloj",
            creditos = 10, sistemaCreditos = "sistema-sintetico-A",
            contenidos = Enumerable.Range(1, 10).Select(i => "C" + i), resultadosAprendizaje = Enumerable.Range(1, 5).Select(i => "R" + i) },
        correspondenciasContenidos = Enumerable.Range(1, 8).Select(i => Mapeo("C" + i)),
        correspondenciasResultadosAprendizaje = Enumerable.Range(1, 4).Select(i => Mapeo("R" + i))
    };
    private static object Mapeo(string id) => new { asignaturaOrigen = "O", elementoOrigen = id, elementoReceptor = id,
        evidencia = new { ubicacionOrigen = "unidad-sintetica/" + id, ubicacionReceptora = "unidad-sintetica/" + id } };

    [Fact]
    public async Task Tres_criterios_al_ochenta_generan_resultado_positivo_mediante_API()
    {
        using var factory = new MotorFactory(); using var client = factory.Autenticado();
        var response = await client.PostAsJsonAsync(Ruta, Datos());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(80, json.GetProperty("compatibilidadAcademicaPct").GetDecimal());
        Assert.Equal(80, json.GetProperty("compatibilidadHorariaPct").GetDecimal());
        Assert.Equal(80, json.GetProperty("compatibilidadCreditosPct").GetDecimal());
        Assert.True(json.GetProperty("homologable").GetBoolean());
        Assert.Equal("Homologable", json.GetProperty("estado").GetString());
        Assert.Equal(new[] { "C9", "C10" }, json.GetProperty("contenidosSinCobertura").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal(new[] { "R5" }, json.GetProperty("resultadosAprendizajeSinCobertura").EnumerateArray().Select(x => x.GetString()));
        Assert.False(json.TryGetProperty("profundidadPct", out _));
        Assert.False(json.TryGetProperty("contenidoPct", out _));
    }

    [Fact]
    public async Task Datos_ausentes_son_informacion_incompleta()
    {
        using var factory = new MotorFactory(); using var client = factory.Autenticado();
        var response = await client.PostAsJsonAsync(Ruta, new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("homologable").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("compatibilidadAcademicaPct").ValueKind);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{malformado")]
    [InlineData("{\"origenes\":[{\"horas\":-1}]}")]
    [InlineData("{\"receptora\":{\"creditos\":0}}")]
    [InlineData("{\"origenes\":[{\"contenidos\":[null]}]}")]
    [InlineData("{\"origenes\":[{\"resultadosAprendizaje\":[\"R\",\"R\"]}]}")]
    [InlineData("{\"correspondenciasContenidos\":[null]}")]
    [InlineData("{\"correspondenciasContenidos\":[{\"asignaturaOrigen\":\"inexistente\",\"elementoOrigen\":\"C\",\"elementoReceptor\":\"C\"}]}")]
    public async Task Datos_invalidos_producen_400_problem_details(string body)
    {
        using var factory = new MotorFactory(); using var client = factory.Autenticado();
        var response = await client.PostAsync(Ruta, new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("{\"origen\":{\"id\":\"O\"},\"correspondencias\":[]}")]
    [InlineData("{\"compatibilidadAcademicaPct\":100,\"umbralPct\":1}")]
    public async Task Contrato_anterior_y_porcentajes_de_cliente_se_rechazan(string body)
    {
        using var factory = new MotorFactory(); using var client = factory.Autenticado();
        var response = await client.PostAsync(Ruta, new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sin_evidencia_no_recomienda_homologacion()
    {
        using var factory = new MotorFactory(); using var client = factory.Autenticado();
        var body = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(Datos()))!;
        body["correspondenciasContenidos"]![0]!["evidencia"] = null;
        var response = await client.PostAsJsonAsync(Ruta, body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InformacionIncompleta", json.GetProperty("estado").GetString());
        Assert.False(json.GetProperty("homologable").GetBoolean());
        Assert.Contains("evidencia", json.GetProperty("justificacion").GetString());
    }

    [Fact]
    public async Task Varios_origenes_combinan_cobertura_pero_no_inventan_agregacion_cuantitativa()
    {
        using var factory = new MotorFactory(); using var client = factory.Autenticado();
        var body = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(Datos()))!;
        var segundo = body["origenes"]![0]!.DeepClone(); segundo["id"] = "O2";
        body["origenes"]!.AsArray().Add(segundo);
        var duplicada = body["correspondenciasContenidos"]![0]!.DeepClone(); duplicada["asignaturaOrigen"] = "O2";
        body["correspondenciasContenidos"]!.AsArray().Add(duplicada);
        var response = await client.PostAsJsonAsync(Ruta, body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(80, json.GetProperty("compatibilidadAcademicaPct").GetDecimal());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("compatibilidadHorariaPct").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("compatibilidadCreditosPct").ValueKind);
        Assert.Equal("InformacionIncompleta", json.GetProperty("estado").GetString());
        Assert.False(json.GetProperty("homologable").GetBoolean());
    }

    [Theory]
    [InlineData("Administrador")] [InlineData("Docente")] [InlineData("Coordinador")]
    public async Task Roles_academicos_tienen_acceso(string rol)
    {
        using var factory = new MotorFactory(); using var client = factory.Autenticado(rol);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(Ruta, Datos())).StatusCode);
    }

    [Fact]
    public async Task Sin_token_o_con_token_fuera_del_contrato_responde_401()
    {
        using var factory = new MotorFactory(); using var client = factory.CreateClient();
        foreach (var token in new[] { null, "invalido", factory.Token(issuer: "otro"), factory.Token(expirado: true),
            factory.Token(audience: "otra"), factory.Token(key: new string('x', 86)),
            factory.Token(algorithm: SecurityAlgorithms.HmacSha512) })
        {
            client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(Ruta, Datos())).StatusCode);
        }
    }

    [Fact]
    public async Task Rol_ajeno_es_403()
    {
        using var factory = new MotorFactory(); using var client = factory.Autenticado("Otro");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Ruta, Datos())).StatusCode);
    }

    [Fact]
    public async Task Configuracion_externa_85_se_refleja_en_HTTP()
    {
        using var factory = new MotorFactory(85); using var client = factory.Autenticado();
        var response = await client.PostAsJsonAsync(Ruta, Datos());
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(85, json.GetProperty("umbralPct").GetDecimal());
        Assert.Equal("NoHomologable", json.GetProperty("estado").GetString());
        Assert.False(json.GetProperty("homologable").GetBoolean());
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(101)]
    public void Configuracion_invalida_impide_arranque(decimal umbral)
    {
        using var factory = new MotorFactory(umbral);
        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public async Task Swagger_documenta_endpoint_y_bearer()
    {
        using var factory = new MotorFactory(); using var client = factory.CreateClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.True(doc.GetProperty("paths").TryGetProperty(Ruta, out _));
        Assert.True(doc.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
    }
}

public class IntegracionGatewayTests
{
    private sealed class ProxyFactory(MotorFactory motor) : WebApplicationFactory<Gateway.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = MotorFactory.Issuer, ["Jwt:Audience"] = MotorFactory.Audience, ["Jwt:SigningKey"] = motor.Key,
                ["ReverseProxy:Clusters:auth:Destinations:primary:Address"] = "http://auth.test/",
                ["ReverseProxy:Clusters:catalogo:Destinations:primary:Address"] = "http://catalogo.test/",
                ["ReverseProxy:Clusters:homologacion:Destinations:primary:Address"] = "http://motor.test/"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IForwarderHttpClientFactory>();
                services.AddSingleton<IForwarderHttpClientFactory>(new ProxyClients(motor));
            });
        }
    }
    private sealed class ProxyClients(MotorFactory motor) : IForwarderHttpClientFactory
    {
        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context) => new(motor.Server.CreateHandler());
    }

    [Theory]
    [InlineData("Administrador", HttpStatusCode.OK)]
    [InlineData("Docente", HttpStatusCode.OK)]
    [InlineData("Coordinador", HttpStatusCode.OK)]
    [InlineData("Otro", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Yarp_real_enruta_y_protege_el_motor(string? rol, HttpStatusCode esperado)
    {
        using var motor = new MotorFactory();
        // Arrancar el destino antes de crear el cliente de YARP.
        using var backend = motor.CreateClient();
        using var proxy = new ProxyFactory(motor); using var client = proxy.CreateClient();
        if (rol is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", motor.Token(rol));
        var response = await client.PostAsJsonAsync(EvaluarEndpointTests.Ruta, EvaluarEndpointTests.Datos());
        Assert.Equal(esperado, response.StatusCode);
        if (esperado == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("O", json.GetProperty("asignaturasOrigen")[0].GetString());
            Assert.True(json.GetProperty("homologable").GetBoolean());
        }
    }
}

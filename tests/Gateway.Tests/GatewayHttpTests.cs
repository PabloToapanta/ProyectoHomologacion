using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Auth.Api.Contracts;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Gateway.Tests;

public sealed class GatewayHttpTests(GatewayFactory factory) : IClassFixture<GatewayFactory>
{
    public static TheoryData<string, string, string> AcademicRoutes => new()
    {
        { "GET", "/api/carreras", "catalogo" }, { "POST", "/api/carreras", "catalogo" },
        { "PUT", "/api/carreras/carrera-prueba", "catalogo" }, { "DELETE", "/api/carreras/carrera-prueba", "catalogo" },
        { "GET", "/api/carreras/carrera-prueba/asignaturas", "catalogo" },
        { "POST", "/api/asignaturas", "catalogo" }, { "GET", "/api/asignaturas/asignatura-prueba", "catalogo" },
        { "POST", "/api/asignaturas/asignatura-prueba/unidades", "catalogo" },
        { "POST", "/api/homologaciones/evaluar", "homologacion" },
        { "POST", "/api/homologaciones/validar-antiguedad", "homologacion" },
        { "POST", "/api/homologaciones/validar-requisitos", "homologacion" }
    };

    [Theory]
    [MemberData(nameof(AcademicRoutes))]
    public async Task RutasAcademicas_SinTokenDevuelven401SinContactarDestino(string method, string path, string service)
    {
        using var client = factory.CreateClient();
        var before = factory.Forwarded.Count;
        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, x => x.Scheme == "Bearer");
        Assert.True(factory.Forwarded.Count == before, $"No se debe contactar a {service} ni a otros destinos sin JWT.");
    }

    [Theory]
    [MemberData(nameof(AcademicRoutes))]
    public async Task RutasAcademicas_ConJwtSeEnrutanSinModificarMetodoRutaQueryOCuerpo(string method, string path, string service)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token());
        using var request = new HttpRequestMessage(new HttpMethod(method), path + "?orden=nivel&pagina=2");
        if (method is "POST" or "PUT") request.Content = new StringContent("{\"datoSintetico\":true}", Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var forwarded = factory.Forwarded.Last();
        Assert.Equal(service, forwarded.Service);
        Assert.Equal(method, forwarded.Method);
        Assert.Equal(path + "?orden=nivel&pagina=2", forwarded.PathAndQuery);
        Assert.Equal(client.DefaultRequestHeaders.Authorization.ToString(), forwarded.Authorization);
        if (method is "POST" or "PUT") Assert.Equal("{\"datoSintetico\":true}", forwarded.Body);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoginPublico_UsaAuthRealYTokenResultantePermiteAccesoAcademico(bool invalidBearer)
    {
        using var client = factory.CreateClient();
        if (invalidBearer) client.DefaultRequestHeaders.Authorization = new("Bearer", "invalido");
        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin-gateway", GatewayFactory.Password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.Equal("auth", factory.Forwarded.Last().Service);
        client.DefaultRequestHeaders.Authorization = new("Bearer", result.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/carreras")).StatusCode);
    }

    [Fact]
    public async Task LoginIncorrecto_PropagaError401SinTokenDelAuthReal()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin-gateway", "incorrecta"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth", factory.Forwarded.Last().Service);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Credenciales incorrectas", body);
        Assert.DoesNotContain("accessToken", body);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("no-expiration")]
    [InlineData("unsigned")]
    [InlineData("algorithm")]
    public async Task JwtInvalido_Devuelve401SinContactarDestino(string scenario)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", scenario == "malformed" ? "invalido" : Token(scenario));
        var before = factory.Forwarded.Count;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/carreras")).StatusCode);
        Assert.Equal(before, factory.Forwarded.Count);
    }

    [Theory]
    [InlineData("Administrador", HttpStatusCode.OK)]
    [InlineData("Docente", HttpStatusCode.OK)]
    [InlineData("Coordinador", HttpStatusCode.OK)]
    [InlineData("Estudiante", HttpStatusCode.Forbidden)]
    [InlineData("administrador", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Forbidden)]
    public async Task Roles_SoloSeAceptanLosTresRolesDefinidos(string? role, HttpStatusCode expected)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token(role: role));
        var before = factory.Forwarded.Count;
        Assert.Equal(expected, (await client.GetAsync("/api/carreras")).StatusCode);
        Assert.Equal(before + (expected == HttpStatusCode.OK ? 1 : 0), factory.Forwarded.Count);
    }

    [Theory]
    [InlineData("/api/solicitudes")]
    [InlineData("/api/informes")]
    [InlineData("/api/auth/register")]
    public async Task RutasNoPlanificadas_NoSeReenvian(string path)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token());
        var before = factory.Forwarded.Count;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(before, factory.Forwarded.Count);
    }

    [Fact]
    public async Task CabecerasDeIdentidadForjadas_NoSustituyenJwt()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Role", "Administrador");
        client.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString());
        var before = factory.Forwarded.Count;
        using var response = await client.GetAsync("/api/carreras");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(before, factory.Forwarded.Count);
    }

    [Fact]
    public async Task JsonLoginInvalido_Propaga400DelAuthReal()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("auth", factory.Forwarded.Last().Service);
    }

    [Fact]
    public async Task OpenApi_DocumentaLasRutasYLaSeguridadDelProxy()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths");
        Assert.Equal(10, paths.EnumerateObject().Count());
        var login = paths.GetProperty("/api/auth/login").GetProperty("post");
        if (login.TryGetProperty("security", out var security)) Assert.Empty(security.EnumerateArray());
        var academic = paths.GetProperty("/api/carreras").GetProperty("get");
        Assert.True(academic.GetProperty("responses").TryGetProperty("401", out _));
        Assert.True(academic.GetProperty("responses").TryGetProperty("403", out _));
        Assert.True(academic.GetProperty("security")[0].TryGetProperty("Bearer", out _));
    }

    private string Token(string scenario = "valid", string? role = "Administrador")
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim> { new("sub", Guid.NewGuid().ToString()), new("name", "usuario-sintetico") };
        if (role is not null) claims.Add(new Claim("role", role));
        var key = scenario == "signature" ? new string('x', 64) : factory.Key;
        var token = new JwtSecurityToken(
            issuer: scenario == "issuer" ? "otro-emisor" : GatewayFactory.Issuer,
            audience: scenario == "audience" ? "otra-api" : GatewayFactory.Audience, claims: claims,
            notBefore: scenario == "future" ? now.AddMinutes(1) : now.AddMinutes(-10),
            expires: scenario == "no-expiration" ? null : scenario == "expired" ? now.AddSeconds(-1) : now.AddMinutes(15),
            signingCredentials: scenario == "unsigned" ? null : new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), scenario == "algorithm" ? SecurityAlgorithms.HmacSha384 : SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

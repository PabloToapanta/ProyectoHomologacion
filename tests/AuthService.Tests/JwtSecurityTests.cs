using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AuthService.Tests;

public sealed class JwtSecurityTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("signature")]
    [InlineData("expired")]
    [InlineData("issuer")]
    [InlineData("audience")]
    public async Task Middleware_RechazaTokensInvalidos(string scenario)
    {
        using var client = factory.CreateClient();
        if (scenario != "missing")
        {
            var value = scenario == "malformed" ? "no-es-jwt" : Token(scenario);
            client.DefaultRequestHeaders.Authorization = new("Bearer", value);
        }
        var response = await client.GetAsync("/_test/admin");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Administrador", HttpStatusCode.OK)]
    [InlineData("Docente", HttpStatusCode.Forbidden)]
    [InlineData("Coordinador", HttpStatusCode.Forbidden)]
    public async Task Middleware_InterpretaRolDelJwt(string role, HttpStatusCode expected)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Token("valid", role));
        Assert.Equal(expected, (await client.GetAsync("/_test/admin")).StatusCode);
    }

    private string Token(string scenario, string role = "Administrador")
    {
        var jwt = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var now = DateTime.UtcNow;
        var key = scenario == "signature" ? new string('x', 64) : jwt.SigningKey;
        var token = new JwtSecurityToken(
            issuer: scenario == "issuer" ? "otro-emisor" : jwt.Issuer,
            audience: scenario == "audience" ? "otra-api" : jwt.Audience,
            claims: [new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", role)],
            notBefore: now.AddMinutes(-20), expires: scenario == "expired" ? now.AddMinutes(-1) : now.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Theory]
    [InlineData("", "api", "suficientemente-larga-para-validacion-32", 15)]
    [InlineData("auth", "", "suficientemente-larga-para-validacion-32", 15)]
    [InlineData("auth", "api", "corta", 15)]
    [InlineData("auth", "api", "suficientemente-larga-para-validacion-32", 0)]
    [InlineData("auth", "api", "suficientemente-larga-para-validacion-32", 61)]
    public void ConfiguracionInsegura_SeRechaza(string issuer, string audience, string key, int minutes)
    {
        var options = new JwtOptions { Issuer = issuer, Audience = audience, SigningKey = key, ExpirationMinutes = minutes };
        Assert.False(Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true));
    }
}

// Solo se añade al host de pruebas; no introduce rutas nuevas al contrato del servicio.
[ApiController]
public sealed class ProtectedProbeController : ControllerBase
{
    [Authorize(Roles = "Administrador")]
    [HttpGet("/_test/admin")]
    public IActionResult Get() => Ok();
}

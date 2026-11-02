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
    [InlineData("plain")]
    [InlineData("other-key")]
    [InlineData("tampered")]
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
        var response = await client.GetAsync("/_test/admin");
        Assert.Equal(expected, response.StatusCode);
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
        var signed = new JwtSecurityTokenHandler().WriteToken(token);
        if (scenario == "plain") return signed;
        var encryptionKey = scenario == "other-key"
            ? System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)
            : Convert.FromBase64String(jwt.EncryptionKey);
        var encrypted = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(
            new SecurityTokenDescriptor
            {
                Issuer = scenario == "issuer" ? "otro-emisor" : jwt.Issuer,
                Audience = scenario == "audience" ? "otra-api" : jwt.Audience,
                Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", role)]),
                NotBefore = now.AddMinutes(-20),
                Expires = scenario == "expired" ? now.AddMinutes(-1) : now.AddMinutes(5),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    SecurityAlgorithms.HmacSha256),
                EncryptingCredentials = new EncryptingCredentials(new SymmetricSecurityKey(encryptionKey),
                    SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes256CbcHmacSha512)
            }));
        if (scenario == "tampered")
        {
            var parts = encrypted.Split('.');
            parts[3] = (parts[3][0] == 'A' ? 'B' : 'A') + parts[3][1..];
            return string.Join('.', parts);
        }
        return encrypted;
    }

    [Theory]
    [InlineData("", "api", "suficientemente-larga-para-validacion-32", 15)]
    [InlineData("auth", "", "suficientemente-larga-para-validacion-32", 15)]
    [InlineData("auth", "api", "corta", 15)]
    [InlineData("auth", "api", "suficientemente-larga-para-validacion-32", 0)]
    [InlineData("auth", "api", "suficientemente-larga-para-validacion-32", 61)]
    public void ConfiguracionInsegura_SeRechaza(string issuer, string audience, string key, int minutes)
    {
        var options = new JwtOptions { Issuer = issuer, Audience = audience, SigningKey = key,
            EncryptionKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ExpirationMinutes = minutes };
        Assert.False(Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    [InlineData("YWFh")]
    public void ClaveDeCifradoInvalida_SeRechazaSinExponerla(string encryptionKey)
    {
        var options = new JwtOptions { Issuer = "auth", Audience = "api", SigningKey = new string('s', 32),
            EncryptionKey = encryptionKey };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(options, new ValidationContext(options), errors, true));
        if (encryptionKey.Length > 0)
            Assert.DoesNotContain(encryptionKey, string.Join(' ', errors.Select(x => x.ErrorMessage)));
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

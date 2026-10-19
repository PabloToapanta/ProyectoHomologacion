using System.Net;
using System.Net.Http.Json;
using AuthService.Contracts;
using AuthService.Data;
using AuthService.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace AuthService.Tests;

public sealed class LoginApiTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient client;
    public LoginApiTests(AuthApiFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task LoginCorrecto_DevuelveJwtSinFiltrarUsuarioNiHash()
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin-api", AuthApiFactory.Password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.Equal(3, result.AccessToken.Split('.').Length);
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(AuthApiFactory.Password, body);
        Assert.DoesNotContain("passwordHash", body);
    }

    [Theory]
    [InlineData("admin-api", "incorrecta")]
    [InlineData("no-existe", AuthApiFactory.Password)]
    public async Task LoginIncorrecto_Devuelve401GenericoSinToken(string nombre, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(nombre, password));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Credenciales incorrectas", body);
        Assert.DoesNotContain("accessToken", body);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"nombreUsuario\":\" \",\"password\":\"abc\"}")]
    [InlineData("{\"nombreUsuario\":\"admin-api\",\"password\":null}")]
    [InlineData("{\"nombreUsuario\":")]
    public async Task LoginInvalido_Devuelve400(string body)
    {
        var response = await client.PostAsync("/api/auth/login", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("accessToken", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_DocumentaLoginYErrores()
    {
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/auth/login", body);
        Assert.Contains("401", body);
        Assert.Contains("400", body);
    }
}

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Prueba-Api-123!";
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Auth"] = "Server=unused;Database=AuthTests;Integrated Security=true;TrustServerCertificate=true",
            ["Jwt:Issuer"] = "auth-api-test", ["Jwt:Audience"] = "api-test",
            ["Jwt:SigningKey"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ["SeedAdmin:Enabled"] = "false", ["Database:ApplyMigrations"] = "false"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<AuthDbContext>();
            services.AddDbContext<AuthDbContext>(options => options.UseSqlite(connection));
            services.AddControllers().AddApplicationPart(typeof(ProtectedProbeController).Assembly);
            using var db = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseSqlite(connection).Options);
            db.Database.EnsureCreated();
            var user = new Usuario { NombreUsuario = "admin-api", NombreNormalizado = "ADMIN-API",
                Rol = db.Roles.Single(x => x.Nombre == "Administrador") };
            user.PasswordHash = new PasswordHasher<Usuario>().HashPassword(user, Password);
            db.Usuarios.Add(user);
            db.SaveChanges();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}

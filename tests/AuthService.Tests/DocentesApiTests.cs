using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Auth.Domain.Entities;
using Auth.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AuthService.Tests;

/// <summary>HU-29: Solicitudes consulta los docentes activos por contrato HTTP, sin acceder a la base de Auth.</summary>
public sealed class DocentesApiTests(DocentesApiFactory factory) : IClassFixture<DocentesApiFactory>
{
    [Fact]
    public async Task Coordinador_ListaSoloDocentesActivosSinDatosSensibles()
    {
        using var client = factory.Client("Coordinador");
        var response = await client.GetAsync("/api/usuarios/docentes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var texto = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", texto, StringComparison.OrdinalIgnoreCase);
        var docentes = await response.Content.ReadFromJsonAsync<List<Docente>>();
        Assert.Equal(["docente.ana", "docente.luis"], docentes!.Select(d => d.NombreUsuario));
        Assert.All(docentes!, d => Assert.NotEqual(Guid.Empty, d.Id));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Docente", HttpStatusCode.Forbidden)]
    [InlineData("Administrador", HttpStatusCode.Forbidden)]
    public async Task OtrosRoles_NoListanDocentes(string? role, HttpStatusCode expected)
    {
        using var client = factory.Client(role);
        Assert.Equal(expected, (await client.GetAsync("/api/usuarios/docentes")).StatusCode);
    }

    private sealed record Docente(Guid Id, string NombreUsuario);
}

public sealed class DocentesApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "auth-docentes-test", Audience = "api-test";
    private readonly string key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    // HU-35: Auth exige un token cifrado; clave sintética por ejecución.
    private readonly string encryptionKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Auth"] = "Server=unused;Database=AuthTests;Integrated Security=true;TrustServerCertificate=true",
            ["Jwt:Issuer"] = Issuer, ["Jwt:Audience"] = Audience, ["Jwt:SigningKey"] = key, ["Jwt:EncryptionKey"] = encryptionKey,
            ["SeedAdmin:Enabled"] = "false", ["Database:ApplyMigrations"] = "false"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<AuthDbContext>();
            services.AddDbContext<AuthDbContext>(options => options.UseSqlite(connection));
            using var db = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseSqlite(connection).Options);
            db.Database.EnsureCreated();
            // Datos sintéticos: el orden de inserción difiere del alfabético esperado.
            foreach (var (nombre, rol, activo) in new[] { ("docente.luis", "Docente", true), ("docente.inactivo", "Docente", false),
                         ("coordinador.uno", "Coordinador", true), ("docente.ana", "Docente", true), ("admin.uno", "Administrador", true) })
            {
                var user = new Usuario { NombreUsuario = nombre, NombreNormalizado = nombre.ToUpperInvariant(), Activo = activo,
                    Rol = db.Roles.Single(x => x.Nombre == rol) };
                user.PasswordHash = new PasswordHasher<Usuario>().HashPassword(user, "Sintetica-Docentes-123!");
                db.Usuarios.Add(user);
            }
            db.SaveChanges();
        });
    }

    public HttpClient Client(string? role)
    {
        var client = CreateClient();
        if (role is null) return client;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer, Audience = Audience,
            Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", role)]),
            NotBefore = DateTime.UtcNow.AddMinutes(-1), Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
            EncryptingCredentials = new EncryptingCredentials(new SymmetricSecurityKey(Convert.FromBase64String(encryptionKey)),
                SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes256CbcHmacSha512)
        });
        client.DefaultRequestHeaders.Authorization = new("Bearer", handler.WriteToken(token));
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}

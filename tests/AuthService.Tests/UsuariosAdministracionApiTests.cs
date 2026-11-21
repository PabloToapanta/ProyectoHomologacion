using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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

/// <summary>HU-02 / T54: contrato HTTP propuesto; pruebas rojas antes de implementar la administración.</summary>
public sealed class UsuariosAdministracionApiTests
{
    private const string Inicial = "Inicial-Sintetica-123!";
    private const string Nueva = "Nueva-Sintetica-456!";

    [Fact]
    public async Task Administrador_CreaYConsultaCuentaSinExponerContrasena()
    {
        using var factory = new UsuariosAdministracionFactory();
        using var client = factory.Client("Administrador");
        using var alta = await client.PostAsJsonAsync("/api/usuarios", new
        {
            nombreUsuario = "docente.nuevo", rol = "Docente", contrasena = Inicial
        });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var id = (await JsonDocument.ParseAsync(await alta.Content.ReadAsStreamAsync()))
            .RootElement.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        AssertNoSecreto(await alta.Content.ReadAsStringAsync());

        using var detalle = await client.GetAsync($"/api/usuarios/{id}");
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        var texto = await detalle.Content.ReadAsStringAsync();
        Assert.Contains("docente.nuevo", texto);
        Assert.Contains("Docente", texto);
        AssertNoSecreto(texto);

        using var listado = await client.GetAsync("/api/usuarios");
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        Assert.Contains(id.ToString(), await listado.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        AssertNoSecreto(await listado.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/auth/login", new { nombreUsuario = "docente.nuevo", password = Inicial })).StatusCode);
    }

    [Fact]
    public async Task NombreNormalizadoDuplicado_Devuelve409YSoloConservaUnaCuenta()
    {
        using var factory = new UsuariosAdministracionFactory();
        using var client = factory.Client("Administrador");
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/usuarios",
            new { nombreUsuario = "docente.uno", rol = "Docente", contrasena = Inicial })).StatusCode);
        using var duplicado = await client.PostAsJsonAsync("/api/usuarios",
            new { nombreUsuario = " DOCENTE.UNO ", rol = "Coordinador", contrasena = Inicial });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.Equal(1, await factory.CountByNormalizedNameAsync("DOCENTE.UNO"));
    }

    [Fact]
    public async Task DesactivarYReactivar_CambiaLoginYConsultaDeDocentesHu29()
    {
        using var factory = new UsuariosAdministracionFactory();
        using var admin = factory.Client("Administrador");
        using var coordinador = factory.Client("Coordinador");
        var id = factory.DocenteId;
        Assert.Equal(HttpStatusCode.OK, (await admin.PatchAsJsonAsync($"/api/usuarios/{id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await admin.PostAsJsonAsync("/api/auth/login", new { nombreUsuario = "docente.existe", password = Inicial })).StatusCode);
        Assert.DoesNotContain("docente.existe", await coordinador.GetStringAsync("/api/usuarios/docentes"));

        Assert.Equal(HttpStatusCode.OK, (await admin.PatchAsJsonAsync($"/api/usuarios/{id}/estado", new { activo = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PostAsJsonAsync("/api/auth/login", new { nombreUsuario = "docente.existe", password = Inicial })).StatusCode);
        using var respuesta = await coordinador.GetAsync("/api/usuarios/docentes");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var docente = (await respuesta.Content.ReadFromJsonAsync<List<DocenteHu29>>())!.Single(x => x.Id == id);
        Assert.Equal("docente.existe", docente.NombreUsuario);
    }

    [Fact]
    public async Task CambiarRol_ActualizaSiguienteLoginYFiltroDeDocentesHu29()
    {
        using var factory = new UsuariosAdministracionFactory();
        using var admin = factory.Client("Administrador");
        using var coordinador = factory.Client("Coordinador");
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PutAsJsonAsync($"/api/usuarios/{factory.DocenteId}/rol", new { rol = "Coordinador" })).StatusCode);
        Assert.DoesNotContain("docente.existe", await coordinador.GetStringAsync("/api/usuarios/docentes"));
        using var login = await admin.PostAsJsonAsync("/api/auth/login",
            new { nombreUsuario = "docente.existe", password = Inicial });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("Coordinador", (await login.Content.ReadFromJsonAsync<LoginRol>())!.Role);
    }

    [Fact]
    public async Task ContrasenaNueva_ReemplazaLaAnteriorSinDevolverSecreto()
    {
        using var factory = new UsuariosAdministracionFactory();
        using var admin = factory.Client("Administrador");
        using var respuesta = await admin.PutAsJsonAsync($"/api/usuarios/{factory.DocenteId}/contrasena", new { contrasena = Nueva });
        Assert.True(respuesta.IsSuccessStatusCode);
        AssertNoSecreto(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await admin.PostAsJsonAsync("/api/auth/login", new { nombreUsuario = "docente.existe", password = Inicial })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await admin.PostAsJsonAsync("/api/auth/login", new { nombreUsuario = "docente.existe", password = Nueva })).StatusCode);
        var hash = await factory.PasswordHashAsync(factory.DocenteId);
        Assert.NotEqual(Nueva, hash);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Docente", HttpStatusCode.Forbidden)]
    [InlineData("Coordinador", HttpStatusCode.Forbidden)]
    public async Task SoloAdministrador_AccedeALasRutasDeGestion(string? rol, HttpStatusCode esperado)
    {
        using var factory = new UsuariosAdministracionFactory();
        using var client = factory.Client(rol);
        foreach (var ruta in new[] { "/api/usuarios", $"/api/usuarios/{factory.DocenteId}" })
            Assert.Equal(esperado, (await client.GetAsync(ruta)).StatusCode);
        Assert.Equal(esperado, (await client.PostAsJsonAsync("/api/usuarios",
            new { nombreUsuario = "otro", rol = "Docente", contrasena = Inicial })).StatusCode);
        Assert.Equal(esperado, (await client.PatchAsJsonAsync($"/api/usuarios/{factory.DocenteId}/estado", new { activo = false })).StatusCode);
        Assert.Equal(esperado, (await client.PutAsJsonAsync($"/api/usuarios/{factory.DocenteId}/rol", new { rol = "Coordinador" })).StatusCode);
        Assert.Equal(esperado, (await client.PutAsJsonAsync($"/api/usuarios/{factory.DocenteId}/contrasena", new { contrasena = Nueva })).StatusCode);
    }

    [Fact]
    public async Task Administrador_NoPuedeDesactivarSuPropiaCuenta()
    {
        using var factory = new UsuariosAdministracionFactory();
        using var client = factory.Client("Administrador");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PatchAsJsonAsync($"/api/usuarios/{factory.AdminId}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/auth/login", new { nombreUsuario = "admin.existe", password = Inicial })).StatusCode);
    }

    [Theory]
    [InlineData("", "Docente", "Inicial-Sintetica-123!")]
    [InlineData("docente.otro", "Administrador", "Inicial-Sintetica-123!")]
    [InlineData("docente.otro", "Docente", "corta")]
    public async Task AltaInvalida_Devuelve400SinCrearCuenta(string nombre, string rol, string contrasena)
    {
        using var factory = new UsuariosAdministracionFactory();
        using var client = factory.Client("Administrador");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/usuarios", new { nombreUsuario = nombre, rol, contrasena })).StatusCode);
        Assert.Equal(0, await factory.CountByNormalizedNameAsync("DOCENTE.OTRO"));
    }

    [Fact]
    public async Task CuentaInexistenteYRolNoPermitido_DevuelvenErroresClaros()
    {
        using var factory = new UsuariosAdministracionFactory();
        using var client = factory.Client("Administrador");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/usuarios/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PatchAsJsonAsync($"/api/usuarios/{Guid.NewGuid()}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PutAsJsonAsync($"/api/usuarios/{factory.DocenteId}/rol", new { rol = "Administrador" })).StatusCode);
    }

    [Fact]
    public async Task CambioEstadoSinValor_NoDesactivaLaCuenta()
    {
        using var factory = new UsuariosAdministracionFactory();
        using var client = factory.Client("Administrador");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PatchAsJsonAsync($"/api/usuarios/{factory.DocenteId}/estado", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/auth/login", new { nombreUsuario = "docente.existe", password = Inicial })).StatusCode);
    }

    private static void AssertNoSecreto(string texto)
    {
        Assert.DoesNotContain(Inicial, texto);
        Assert.DoesNotContain(Nueva, texto);
        Assert.DoesNotContain("passwordHash", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contrasena", texto, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record DocenteHu29(Guid Id, string NombreUsuario);
    private sealed record LoginRol(string Role);
}

public sealed class UsuariosAdministracionFactory : WebApplicationFactory<Program>
{
    private const string Interna = "hu36-test-only-internal-key-64-characters-for-isolated-tests";
    private const string Issuer = "hu02-auth-test", Audience = "hu02-api-test";
    private const string Inicial = "Inicial-Sintetica-123!";
    private readonly string signingKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    private readonly string encryptionKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public Guid AdminId { get; } = Guid.NewGuid();
    public Guid DocenteId { get; } = Guid.NewGuid();

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Add("X-Internal-Key", Interna);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Auth"] = "Server=unused;Database=Hu02Tests;Integrated Security=true",
            ["Jwt:Issuer"] = Issuer, ["Jwt:Audience"] = Audience, ["Jwt:SigningKey"] = signingKey,
            ["Jwt:EncryptionKey"] = encryptionKey, ["InternalAccess:Key"] = Interna,
            ["SeedAdmin:Enabled"] = "false", ["Database:ApplyMigrations"] = "false"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<AuthDbContext>();
            services.AddDbContext<AuthDbContext>(options => options.UseSqlite(connection));
            using var db = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseSqlite(connection).Options);
            db.Database.EnsureCreated();
            foreach (var (id, nombre, rol) in new[]
                     { (AdminId, "admin.existe", "Administrador"), (DocenteId, "docente.existe", "Docente") })
            {
                var usuario = new Usuario { Id = id, NombreUsuario = nombre, NombreNormalizado = nombre.ToUpperInvariant(),
                    Rol = db.Roles.Single(x => x.Nombre == rol) };
                usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, Inicial);
                db.Usuarios.Add(usuario);
            }
            db.SaveChanges();
        });
    }

    public HttpClient Client(string? rol)
    {
        var client = CreateClient();
        if (rol is null) return client;
        var token = new JwtSecurityTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer, Audience = Audience,
            Subject = new ClaimsIdentity([new Claim("sub", rol == "Administrador" ? AdminId.ToString() : Guid.NewGuid().ToString()),
                new Claim("role", rol)]),
            NotBefore = DateTime.UtcNow.AddMinutes(-1), Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
            EncryptingCredentials = new EncryptingCredentials(new SymmetricSecurityKey(Convert.FromBase64String(encryptionKey)),
                SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes256CbcHmacSha512)
        });
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    public async Task<int> CountByNormalizedNameAsync(string nombre)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Usuarios.CountAsync(x => x.NombreNormalizado == nombre);
    }

    public async Task<string> PasswordHashAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Usuarios.SingleAsync(x => x.Id == id)).PasswordHash;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}

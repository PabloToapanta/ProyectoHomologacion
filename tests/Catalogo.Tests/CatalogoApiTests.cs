using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Catalogo.Application;
using Catalogo.Domain;
using Catalogo.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Catalogo.Tests;

public class CatalogoApiTests
{
    private static object Input(string nombre = "Carrera sintética", string facultad = "FISEI", bool vigencia = true) => new { nombre, facultad, vigencia };

    [Fact]
    public async Task CRUD_HTTP_y_location()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var response = await client.PostAsJsonAsync("/api/carreras", Input());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = (await response.Content.ReadFromJsonAsync<CarreraDto>())!;
        Assert.EndsWith($"/api/carreras/{creada.Id}", response.Headers.Location!.ToString());
        Assert.Equal(creada, await client.GetFromJsonAsync<CarreraDto>(response.Headers.Location));
        Assert.Equal(creada, Assert.Single((await client.GetFromJsonAsync<CarreraDto[]>("/api/carreras"))!));
        var edit = await client.PutAsJsonAsync($"/api/carreras/{creada.Id}", Input("Editada", vigencia: false));
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.False((await edit.Content.ReadFromJsonAsync<CarreraDto>())!.Vigencia);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/carreras/{creada.Id}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<CarreraDto[]>("/api/carreras"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/carreras/{creada.Id}")).StatusCode);
    }

    [Fact]
    public async Task Duplicado_en_creacion_y_edicion_devuelve_409_y_otra_facultad_permitida()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        await client.PostAsJsonAsync("/api/carreras", Input());
        var duplicado = await client.PostAsJsonAsync("/api/carreras", Input(" carrera   sintética ", " fisei "));
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.Contains("duplicado", await duplicado.Content.ReadAsStringAsync());
        var otra = await client.PostAsJsonAsync("/api/carreras", Input(facultad: "Otra"));
        Assert.Equal(HttpStatusCode.Created, otra.StatusCode);
        var dto = (await otra.Content.ReadFromJsonAsync<CarreraDto>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/carreras/{dto.Id}", Input())).StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"nombre\":\"N\",\"facultad\":\"F\"}")]
    [InlineData("{\"nombre\":\"N\",\"facultad\":\"F\",\"vigencia\":null}")]
    [InlineData("{\"nombre\":\" \",\"facultad\":\"F\",\"vigencia\":true}")]
    [InlineData("{\"nombre\":\"N\",\"facultad\":\"F\",\"vigencia\":\"si\"}")]
    [InlineData("null")]
    public async Task Rechaza_JSON_invalido_o_campos_faltantes(string json)
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var result = await client.PostAsync("/api/carreras", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Contains("problem+json", result.Content.Headers.ContentType!.MediaType!);
        Assert.Empty((await client.GetFromJsonAsync<CarreraDto[]>("/api/carreras"))!);
    }

    [Fact]
    public async Task Asignatura_relacionada_impide_eliminar_y_conserva_carrera()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var response = await client.PostAsJsonAsync("/api/carreras", Input());
        var carrera = (await response.Content.ReadFromJsonAsync<CarreraDto>())!;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Asignaturas (Id, CarreraId) VALUES ({Guid.NewGuid()}, {carrera.Id})");
        }
        var result = await client.DeleteAsync($"/api/carreras/{carrera.Id}");
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Contains("asignaturas_relacionadas", await result.Content.ReadAsStringAsync());
        Assert.Single((await client.GetFromJsonAsync<CarreraDto[]>("/api/carreras"))!);
    }

    [Fact]
    public async Task Base_relacional_impone_indice_unico_y_FK_sin_cascada()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var response = await client.PostAsJsonAsync("/api/carreras", Input());
        var carrera = (await response.Content.ReadFromJsonAsync<CarreraDto>())!;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
        db.Carreras.Add(new Carrera(" carrera sintética ", "fisei", true));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Asignaturas (Id, CarreraId) VALUES ({Guid.NewGuid()}, {carrera.Id})");
        var entity = await db.Carreras.SingleAsync();
        db.Carreras.Remove(entity);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Protege_todas_las_operaciones_y_rechaza_rol_docente()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/carreras")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/carreras", Input())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync($"/api/carreras/{id}", Input())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/api/carreras/{id}")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("Docente"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/carreras")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("Administrador", expired: true));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/carreras")).StatusCode);
    }

    [Fact]
    public async Task Swagger_documenta_rutas_respuestas_y_Bearer()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.CreateClient();
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/carreras/{id}", json);
        Assert.Contains("Bearer", json);
        Assert.Contains("409", json);
        Assert.Contains("vigencia", json);
    }

    [Fact]
    public void Capas_de_negocio_no_dependen_de_HTTP_ni_EF()
    {
        foreach (var assembly in new[] { typeof(Carrera).Assembly, typeof(CarreraService).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.Contains("EntityFramework") || a.Name!.Contains("AspNetCore") || a.Name!.Contains("Infrastructure"));
    }
}

internal sealed class CatalogoFactory : WebApplicationFactory<Program>
{
    // Clave sintética exclusiva de pruebas, nunca utilizada en despliegues.
    private const string Key = "catalogo-test-only-signing-key-32-bytes-minimum";
    private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.UseSetting("ConnectionStrings:Catalogo", "Server=unused;Database=Catalogo");
        builder.UseSetting("Jwt:Issuer", "catalogo-tests");
        builder.UseSetting("Jwt:Audience", "catalogo-tests");
        builder.UseSetting("Jwt:SigningKey", Key);
        builder.UseSetting("Database:ApplyMigrations", "false");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CatalogoDbContext>>();
            services.AddDbContext<CatalogoDbContext>(o => o.UseSqlite(connection));
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<CatalogoDbContext>().Database.EnsureCreated();
        });
    }

    public string Token(string role, bool expired = false) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        "catalogo-tests", "catalogo-tests", [new Claim("role", role)],
        expires: expired ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(5),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256)));

    public HttpClient Admin()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Administrador"));
        return client;
    }

    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}

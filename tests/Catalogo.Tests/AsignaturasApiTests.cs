using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Catalogo.Application;
using Catalogo.Domain;
using Catalogo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Catalogo.Tests;

public class AsignaturasApiTests
{
    private static async Task<Guid> CrearCarrera(HttpClient client, string nombre = "Software")
    {
        var response = await client.PostAsJsonAsync("/api/carreras", new { nombre, facultad = "Facultad sintética HU-05", vigencia = true });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CarreraDto>())!.Id;
    }

    private static object Input(Guid carreraId, string codigo = "DEMO-ALG-LIN-001", int nivel = 1, decimal creditos = 3, int horas = 144) =>
        new { carreraId, codigo, nombre = "Álgebra Lineal", nivel, creditos, horas };

    [Fact]
    public async Task Registra_Algebra_Lineal_consulta_location_y_restringe_eliminacion_de_carrera()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var carreraId = await CrearCarrera(client);
        var response = await client.PostAsJsonAsync("/api/asignaturas", Input(carreraId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<AsignaturaDto>())!;
        Assert.EndsWith($"/api/asignaturas/{dto.Id}", response.Headers.Location!.ToString());
        Assert.Equal(dto, await client.GetFromJsonAsync<AsignaturaDto>(response.Headers.Location));
        Assert.Equal(dto, Assert.Single((await client.GetFromJsonAsync<AsignaturaDto[]>($"/api/carreras/{carreraId}/asignaturas"))!));
        Assert.Equal(1, dto.Nivel);
        Assert.Equal(3m, dto.Creditos);
        Assert.Equal(144, dto.Horas);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/carreras/{carreraId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/asignaturas", Input(carreraId))).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<AsignaturaDto[]>($"/api/carreras/{carreraId}/asignaturas"))!);
    }

    [Fact]
    public async Task Lista_aislada_por_carrera_y_ordenada_por_nivel_y_codigo()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var primera = await CrearCarrera(client);
        var segunda = await CrearCarrera(client, "Otra carrera");
        Assert.Empty((await client.GetFromJsonAsync<AsignaturaDto[]>($"/api/carreras/{primera}/asignaturas"))!);
        foreach (var input in new[] { Input(primera, "Z", 2), Input(primera, "B"), Input(primera, "A"), Input(segunda, "A") })
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/asignaturas", input)).StatusCode);
        var lista = (await client.GetFromJsonAsync<AsignaturaDto[]>($"/api/carreras/{primera}/asignaturas"))!;
        Assert.Equal(new[] { "A", "B", "Z" }, lista.Select(a => a.Codigo));
        Assert.All(lista, a => Assert.Equal(primera, a.CarreraId));
        Assert.Single((await client.GetFromJsonAsync<AsignaturaDto[]>($"/api/carreras/{segunda}/asignaturas"))!);
        var repetido = await client.PostAsJsonAsync("/api/asignaturas", Input(primera, " a "));
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Contains("duplicado", await repetido.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(0, 3, 144)]
    [InlineData(-1, 3, 144)]
    [InlineData(1, 0, 144)]
    [InlineData(1, -1, 144)]
    [InlineData(1, 3, 0)]
    [InlineData(1, 3, -1)]
    [InlineData(1, 0.001, 144)]
    public async Task Rechaza_valores_invalidos_sin_persistir(int nivel, decimal creditos, int horas)
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var id = await CrearCarrera(client);
        var result = await client.PostAsJsonAsync("/api/asignaturas", Input(id, nivel: nivel, creditos: creditos, horas: horas));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType!.MediaType);
        Assert.Contains("validacion", await result.Content.ReadAsStringAsync());
        Assert.Empty((await client.GetFromJsonAsync<AsignaturaDto[]>($"/api/carreras/{id}/asignaturas"))!);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"carreraId\":\"no-es-uuid\"}")]
    [InlineData("{\"nivel\":1.5}")]
    [InlineData("{\"horas\":144.5}")]
    public async Task Rechaza_campos_faltantes_y_tipos_invalidos(string json)
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var result = await client.PostAsync("/api/asignaturas", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Rechaza_carrera_y_asignatura_inexistentes()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/asignaturas", Input(Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/asignaturas/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/carreras/{Guid.NewGuid()}/asignaturas")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/asignaturas", Input(Guid.Empty))).StatusCode);
    }

    [Fact]
    public async Task Solo_administrador_accede_y_swagger_documenta_contratos()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/asignaturas", Input(id))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/asignaturas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/carreras/{id}/asignaturas")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("Docente"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/asignaturas", Input(id))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/asignaturas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/carreras/{id}/asignaturas")).StatusCode);
        var swagger = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/asignaturas/{id}", swagger);
        Assert.Contains("/api/carreras/{id}/asignaturas", swagger);
        Assert.Contains("creditos", swagger);
    }

    [Fact]
    public async Task DB_impone_codigo_unico_FK_y_positivos_aun_sin_servicio()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var id = await CrearCarrera(client);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
        db.Asignaturas.Add(new Asignatura(id, "DEMO", "N", 1, 3, 144));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        db.Asignaturas.Add(new Asignatura(id, " demo ", "Otra", 1, 3, 144));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.Asignaturas.Add(new Asignatura(Guid.NewGuid(), "C", "N", 1, 3, 144));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Asignaturas SET Horas = 0 WHERE CarreraId = {id}"));
    }

    [Fact]
    public void Script_SQL_Server_amplia_tabla_sin_borrar_y_exige_resolver_referencias_antiguas()
    {
        using var db = new CatalogoDbContext(new DbContextOptionsBuilder<CatalogoDbContext>()
            .UseSqlServer("Server=unused;Database=Catalogo").Options);
        var sql = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("THROW 51000", sql);
        Assert.Contains("IF EXISTS (SELECT 1 FROM [Asignaturas])", sql);
        Assert.Contains("ALTER TABLE [Asignaturas] ADD [Creditos] decimal(9,2) NOT NULL", sql);
        Assert.Contains("ON DELETE NO ACTION", sql);
        Assert.Contains("CK_Asignaturas_HorasPositivas", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
        Assert.DoesNotContain("UPDATE [Asignaturas]", sql);
    }
}

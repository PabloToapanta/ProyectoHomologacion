using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Catalogo.Application;
using Xunit;

namespace Catalogo.Tests;

public class MallasApiTests
{
    private static async Task<Guid> Carrera(HttpClient client, string nombre = "Carrera HU07") =>
        (await (await client.PostAsJsonAsync("/api/carreras", new { nombre, facultad = "FISEI", vigencia = true }))
            .Content.ReadFromJsonAsync<CarreraDto>())!.Id;

    [Fact]
    public async Task Crear_activar_y_conservar_version_anterior_y_asignaturas()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var carrera = await Carrera(client);
        var iniciales = await client.GetFromJsonAsync<JsonElement>($"/api/carreras/{carrera}/mallas");
        var inicial = Assert.Single(iniciales.EnumerateArray());
        Assert.True(inicial.GetProperty("vigente").GetBoolean());
        var asignatura = await client.PostAsJsonAsync("/api/asignaturas", new { carreraId = carrera, codigo = "A", nombre = "Álgebra", nivel = 1, creditos = 3, horas = 120 });
        Assert.Equal(HttpStatusCode.Created, asignatura.StatusCode);
        var nueva = await client.PostAsJsonAsync($"/api/carreras/{carrera}/mallas", new { nombre = "Plan nuevo", vigente = true });
        Assert.Equal(HttpStatusCode.Created, nueva.StatusCode);
        var mallas = await client.GetFromJsonAsync<JsonElement>($"/api/carreras/{carrera}/mallas");
        Assert.Equal(2, mallas.GetArrayLength());
        Assert.Single(mallas.EnumerateArray(), m => m.GetProperty("vigente").GetBoolean());
        var lista = await client.GetFromJsonAsync<JsonElement>($"/api/carreras/{carrera}/asignaturas");
        Assert.Equal(inicial.GetProperty("id").GetGuid(), lista[0].GetProperty("mallaId").GetGuid());
        Assert.False(lista[0].GetProperty("malla").GetProperty("vigente").GetBoolean());
        var activar = await client.PutAsJsonAsync($"/api/mallas/{inicial.GetProperty("id").GetGuid()}/vigencia", new { vigente = true });
        Assert.Equal(HttpStatusCode.OK, activar.StatusCode);
    }

    [Fact]
    public async Task Codigo_por_version_y_pertenencia_y_filtro_sin_ocultar_historico()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var carrera = await Carrera(client);
        var otraCarrera = await Carrera(client, "Otra HU07");
        var inicial = (await client.GetFromJsonAsync<JsonElement>($"/api/carreras/{carrera}/mallas"))[0].GetProperty("id").GetGuid();
        var nueva = await client.PostAsJsonAsync($"/api/carreras/{carrera}/mallas", new { nombre = "2027", vigente = true });
        Assert.Equal(HttpStatusCode.Created, nueva.StatusCode);
        var idNueva = (await nueva.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        object Asignatura(Guid mallaId, string codigo = "A") => new { carreraId = carrera, mallaId, codigo, nombre = "Asignatura", nivel = 1, creditos = 2, horas = 90 };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/asignaturas", Asignatura(inicial))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/asignaturas", Asignatura(idNueva))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/asignaturas", Asignatura(idNueva, " a "))).StatusCode);
        var ajena = (await client.GetFromJsonAsync<JsonElement>($"/api/carreras/{otraCarrera}/mallas"))[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/asignaturas", Asignatura(ajena, "B"))).StatusCode);
        Assert.Equal(2, (await client.GetFromJsonAsync<JsonElement>($"/api/carreras/{carrera}/asignaturas")).GetArrayLength());
        Assert.Single((await client.GetFromJsonAsync<JsonElement>($"/api/carreras/{carrera}/asignaturas?mallaId={inicial}")).EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/carreras/{carrera}/asignaturas?mallaId={ajena}")).StatusCode);
    }

    [Fact]
    public async Task Valida_nombre_duplicado_y_activacion_sin_cambios_parciales()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var carrera = await Carrera(client);
        var ruta = $"/api/carreras/{carrera}/mallas";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(ruta, new { nombre = " ", vigente = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(ruta, new { nombre = "Plan nuevo", vigente = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(ruta, new { nombre = " plan   NUEVO ", vigente = true })).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<JsonElement>(ruta)).EnumerateArray(), m => m.GetProperty("vigente").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/mallas/{Guid.NewGuid()}/vigencia", new { vigente = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/mallas/{Guid.NewGuid()}/vigencia", new { vigente = true })).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Docente", HttpStatusCode.Forbidden)]
    [InlineData("Coordinador", HttpStatusCode.Forbidden)]
    public async Task Gestion_exclusiva_del_administrador(string? rol, HttpStatusCode esperado)
    {
        using var factory = new CatalogoFactory();
        using var client = factory.CreateClient();
        if (rol is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(rol));
        Assert.Equal(esperado, (await client.GetAsync($"/api/carreras/{Guid.NewGuid()}/mallas")).StatusCode);
        Assert.Equal(esperado, (await client.PostAsJsonAsync($"/api/carreras/{Guid.NewGuid()}/mallas", new { nombre = "Plan", vigente = true })).StatusCode);
        Assert.Equal(esperado, (await client.PutAsJsonAsync($"/api/mallas/{Guid.NewGuid()}/vigencia", new { vigente = true })).StatusCode);
    }

    [Fact]
    public async Task Version_nueva_tiene_location_y_documentacion_OpenAPI()
    {
        using var factory = new CatalogoFactory(); using var client = factory.Admin();
        var carrera = await Carrera(client);
        var creada = await client.PostAsJsonAsync($"/api/carreras/{carrera}/mallas", new { nombre = "Plan documentado", vigente = false });
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var datos = await creada.Content.ReadFromJsonAsync<JsonElement>();
        var consulta = await client.GetFromJsonAsync<JsonElement>(creada.Headers.Location);
        Assert.Equal(datos.GetProperty("id").GetGuid(), consulta.GetProperty("id").GetGuid());
        var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = swagger.GetProperty("paths");
        Assert.True(paths.GetProperty("/api/carreras/{carreraId}/mallas").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/mallas/{id}/vigencia").GetProperty("put").GetProperty("responses").TryGetProperty("409", out _));
    }
}

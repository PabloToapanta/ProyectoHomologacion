using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalogo.Application;
using Xunit;

namespace Catalogo.Tests;

/// <summary>Eliminar una asignatura registrada por error, con sus datos propios. Datos sintéticos.</summary>
public class EliminarAsignaturaTests
{
    [Fact]
    public async Task Administrador_elimina_la_asignatura_con_unidades_perfil_y_correspondencias_y_libera_el_codigo()
    {
        using var factory = new CatalogoFactory(); using var client = factory.Admin();
        var c = (await (await client.PostAsJsonAsync("/api/carreras", new { nombre = "Demo", facultad = "FISEI", vigencia = true })).Content.ReadFromJsonAsync<CarreraDto>())!;
        async Task<AsignaturaDto> Crear(string codigo, int horas = 100) => (await (await client.PostAsJsonAsync("/api/asignaturas", new { carreraId = c.Id, codigo, nombre = codigo, nivel = 1, horas, creditos = 3 })).Content.ReadFromJsonAsync<AsignaturaDto>())!;
        var equivocada = await Crear("ALG-01", horas: 3); var otra = await Crear("OTRA-01");
        var perfil = new { planAnalitico = "Plan sintético", unidadHoras = "hora-reloj", sistemaCreditos = "DEMO", esSintetico = true };
        foreach (var a in new[] { equivocada, otra })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/asignaturas/{a.Id}/perfil-comparacion", perfil)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/api/asignaturas/{a.Id}/unidades", new { numero = 1, nombre = "Unidad", horas = 3, resultadoAprendizaje = "Resultado", contenidos = new[] { "Tema" } })).StatusCode);
        }
        using var od = JsonDocument.Parse(await client.GetStringAsync($"/api/asignaturas/{otra.Id}"));
        using var ed = JsonDocument.Parse(await client.GetStringAsync($"/api/asignaturas/{equivocada.Id}"));
        string Contenido(JsonDocument d) => d.RootElement.GetProperty("unidades")[0].GetProperty("contenidos")[0].GetProperty("id").GetString()!;
        var vinculo = new { elementoOrigen = Contenido(od), elementoReceptor = Contenido(ed), ubicacionOrigen = "U1", ubicacionReceptora = "U1" };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/asignaturas/{otra.Id}/correspondencias/{equivocada.Id}", new { contenidos = new[] { vinculo }, resultados = Array.Empty<object>() })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/asignaturas/{equivocada.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/asignaturas/{equivocada.Id}")).StatusCode);
        var lista = await client.GetFromJsonAsync<AsignaturaDto[]>($"/api/carreras/{c.Id}/asignaturas");
        Assert.Equal(["OTRA-01"], lista!.Select(a => a.Codigo));
        // La otra asignatura conserva sus datos; la correspondencia con la eliminada ya no existe.
        Assert.Single((await client.GetFromJsonAsync<JsonElement>($"/api/asignaturas/{otra.Id}")).GetProperty("unidades").EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/asignaturas/{otra.Id}/correspondencias/{equivocada.Id}")).StatusCode);
        // El código queda libre para registrar la asignatura correcta.
        var corregida = await client.PostAsJsonAsync("/api/asignaturas", new { carreraId = c.Id, codigo = "ALG-01", nombre = "ALGEBRA LINEAL", nivel = 1, horas = 144, creditos = 3 });
        Assert.Equal(HttpStatusCode.Created, corregida.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/asignaturas/{equivocada.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("Coordinador", HttpStatusCode.Forbidden)]
    [InlineData("Docente", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Solo_el_Administrador_elimina(string? rol, HttpStatusCode esperado)
    {
        using var factory = new CatalogoFactory(); using var admin = factory.Admin();
        var c = (await (await admin.PostAsJsonAsync("/api/carreras", new { nombre = "Demo", facultad = "FISEI", vigencia = true })).Content.ReadFromJsonAsync<CarreraDto>())!;
        var a = (await (await admin.PostAsJsonAsync("/api/asignaturas", new { carreraId = c.Id, codigo = "A-1", nombre = "A", nivel = 1, horas = 10, creditos = 1 })).Content.ReadFromJsonAsync<AsignaturaDto>())!;
        using var client = factory.CreateClient();
        if (rol is not null) client.DefaultRequestHeaders.Authorization = new("Bearer", factory.Token(rol));
        Assert.Equal(esperado, (await client.DeleteAsync($"/api/asignaturas/{a.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/asignaturas/{a.Id}")).StatusCode);
    }
}

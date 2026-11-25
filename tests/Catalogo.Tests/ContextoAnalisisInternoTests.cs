using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalogo.Application;
using Xunit;

namespace Catalogo.Tests;

public class ContextoAnalisisInternoTests
{
    private static async Task<AsignaturaDto> Crear(HttpClient admin, string codigo)
    {
        var carrera = (await (await admin.PostAsJsonAsync("/api/carreras", new { nombre = codigo, facultad = "FISEI", vigencia = true })).Content.ReadFromJsonAsync<CarreraDto>())!;
        return (await (await admin.PostAsJsonAsync("/api/asignaturas", new { carreraId = carrera.Id, codigo, nombre = codigo, nivel = 1, creditos = 3, horas = 100 })).Content.ReadFromJsonAsync<AsignaturaDto>())!;
    }
    [Fact]
    public async Task Lectura_docente_incluye_programa_y_textos_sin_conceder_escritura()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin();
        var a = await Crear(admin, "HU18-SINTETICO");
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"/api/asignaturas/{a.Id}/unidades",
            new { numero = 1, nombre = "Unidad sintética", horas = 100, resultadoAprendizaje = "Resultado sintético", contenidos = new[] { "Contenido sintético" } })).StatusCode);
        await admin.PutAsJsonAsync($"/api/asignaturas/{a.Id}/perfil-comparacion", new { planAnalitico = "plan-sintetico", unidadHoras = "hora-reloj", sistemaCreditos = "sistema-sintetico", esSintetico = true });
        using var docente = f.CreateClient(); docente.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        using var response = await docente.GetAsync($"/api/asignaturas/{a.Id}/analisis-interno/contexto");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var j = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(a.Id, j.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(a.CarreraId, j.RootElement.GetProperty("carreraId").GetGuid());
        Assert.Equal(a.MallaId, j.RootElement.GetProperty("malla").GetProperty("id").GetGuid());
        Assert.Equal(a.Malla!.Nombre, j.RootElement.GetProperty("malla").GetProperty("nombre").GetString());
        Assert.Equal("FISEI", j.RootElement.GetProperty("facultad").GetString());
        Assert.Equal("Contenido sintético", j.RootElement.GetProperty("contenidos")[0].GetProperty("texto").GetString());
        Assert.Equal("Resultado sintético", j.RootElement.GetProperty("resultados")[0].GetProperty("texto").GetString());
        Assert.True(j.RootElement.GetProperty("esSintetico").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await docente.PostAsJsonAsync("/api/asignaturas", new { })).StatusCode);
    }
    [Fact]
    public async Task Ausencia_de_revision_no_inventa_correspondencias()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin();
        var a = await Crear(admin, "HU18-O"); var b = await Crear(admin, "HU18-R");
        using var docente = f.CreateClient(); docente.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        using var response = await docente.GetAsync($"/api/asignaturas/{a.Id}/analisis-interno/correspondencias/{b.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var j = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(j.RootElement.GetProperty("registrada").GetBoolean());
        Assert.False(j.RootElement.GetProperty("vigente").GetBoolean());
        Assert.Empty(j.RootElement.GetProperty("contenidos").EnumerateArray());
    }
    [Theory]
    [InlineData(null, 401)]
    [InlineData("Coordinador", 403)]
    public async Task Protege_el_contexto_de_lectura(string? rol, int esperado)
    {
        using var f = new CatalogoFactory(); using var c = f.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        Assert.Equal(esperado, (int)(await c.GetAsync($"/api/asignaturas/{Guid.NewGuid()}/analisis-interno/contexto")).StatusCode);
    }
    [Fact]
    public async Task Receptoras_del_analisis_solo_ofrecen_la_malla_vigente()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin();
        var anterior = await Crear(admin, "HU07-R1");
        var nueva = (await (await admin.PostAsJsonAsync($"/api/carreras/{anterior.CarreraId}/mallas", new { nombre = "Plan nuevo", vigente = true }))
            .Content.ReadFromJsonAsync<MallaDto>())!;
        var vigente = (await (await admin.PostAsJsonAsync("/api/asignaturas", new { carreraId = anterior.CarreraId, codigo = "HU07-R1", nombre = "Receptora vigente", nivel = 1, creditos = 3, horas = 100 }))
            .Content.ReadFromJsonAsync<AsignaturaDto>())!;
        Assert.Equal(nueva.Id, vigente.MallaId);
        using var docente = f.CreateClient(); docente.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        using var response = await docente.GetAsync($"/api/carreras/{anterior.CarreraId}/analisis-interno/asignaturas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var receptoras = (await response.Content.ReadFromJsonAsync<AsignaturaDto[]>())!;
        Assert.Equal([vigente.Id], receptoras.Select(r => r.Id));
        // La gestión del Administrador conserva todas las versiones.
        Assert.Equal(2, (await admin.GetFromJsonAsync<AsignaturaDto[]>($"/api/carreras/{anterior.CarreraId}/asignaturas"))!.Length);
    }
}

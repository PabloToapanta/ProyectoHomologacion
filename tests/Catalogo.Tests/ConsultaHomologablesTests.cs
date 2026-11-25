using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalogo.Application;
using Xunit;

namespace Catalogo.Tests;

/// <summary>HU-09: contexto de solo lectura para consultar homologables entre carreras FISEI.</summary>
public class ConsultaHomologablesTests
{
    private static async Task<CarreraDto> Carrera(HttpClient admin, string nombre, string facultad = "FISEI") =>
        (await (await admin.PostAsJsonAsync("/api/carreras", new { nombre, facultad, vigencia = true })).Content.ReadFromJsonAsync<CarreraDto>())!;
    private static async Task<(AsignaturaDto Asignatura, JsonElement Unidad)> Asignatura(HttpClient admin, Guid carrera, string codigo, bool perfil = true)
    {
        var a = (await (await admin.PostAsJsonAsync("/api/asignaturas", new { carreraId = carrera, codigo, nombre = "Asignatura " + codigo, nivel = 1, creditos = 3, horas = 100 }))
            .Content.ReadFromJsonAsync<AsignaturaDto>())!;
        using var u = await admin.PostAsJsonAsync($"/api/asignaturas/{a.Id}/unidades",
            new { numero = 1, nombre = "Unidad sintética", horas = 100, resultadoAprendizaje = "Resultado sintético " + codigo, contenidos = new[] { "Contenido sintético " + codigo } });
        var unidad = JsonDocument.Parse(await u.Content.ReadAsStringAsync()).RootElement.Clone();
        if (perfil) await admin.PutAsJsonAsync($"/api/asignaturas/{a.Id}/perfil-comparacion", new { planAnalitico = "plan-" + codigo, unidadHoras = "hora-reloj", sistemaCreditos = "sistema-sintetico", esSintetico = true });
        return (a, unidad);
    }
    private static object Vinculo(string a, string b) => new { elementoOrigen = a, elementoReceptor = b, ubicacionOrigen = "plan-O/U1", ubicacionReceptora = "plan-R/U1" };
    private static HttpClient Cliente(CatalogoFactory f, string rol)
    {
        var c = f.CreateClient(); c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol)); return c;
    }

    [Theory]
    [InlineData("Docente")]
    [InlineData("Coordinador")]
    public async Task Devuelve_solo_pares_revisados_con_contexto_y_vigencia(string rol)
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin();
        var o = await Carrera(admin, "HU09 origen"); var r = await Carrera(admin, "HU09 receptora");
        var (a1, u1) = await Asignatura(admin, o.Id, "HU09-O1");
        var (a2, _) = await Asignatura(admin, o.Id, "HU09-O2");
        var (b1, v1) = await Asignatura(admin, r.Id, "HU09-R1");
        var contenidoO = u1.GetProperty("contenidos")[0].GetProperty("id").GetString()!;
        var contenidoR = v1.GetProperty("contenidos")[0].GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/asignaturas/{a1.Id}/correspondencias/{b1.Id}",
            new { contenidos = new[] { Vinculo(contenidoO, contenidoR) }, resultados = new[] { Vinculo(u1.GetProperty("id").GetString()!, v1.GetProperty("id").GetString()!) } })).StatusCode);

        using var c = Cliente(f, rol);
        using var response = await c.GetAsync($"/api/consulta-homologables/carreras/{o.Id}/{r.Id}/contexto");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var j = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var pares = j.RootElement.GetProperty("pares");
        var par = Assert.Single(pares.EnumerateArray());
        Assert.Equal(a1.Id, par.GetProperty("origenId").GetGuid());
        Assert.Equal(b1.Id, par.GetProperty("receptoraId").GetGuid());
        Assert.True(par.GetProperty("revision").GetProperty("vigente").GetBoolean());
        var ids = j.RootElement.GetProperty("asignaturas").EnumerateArray().Select(x => x.GetProperty("asignatura").GetProperty("id").GetGuid()).ToArray();
        Assert.Equal(new[] { a1.Id, b1.Id }.Order(), ids.Order());
        Assert.DoesNotContain(a2.Id, ids);
        var contexto = j.RootElement.GetProperty("asignaturas").EnumerateArray().First(x => x.GetProperty("asignatura").GetProperty("id").GetGuid() == a1.Id).GetProperty("contexto");
        Assert.Equal("Contenido sintético HU09-O1", contexto.GetProperty("contenidos")[0].GetProperty("texto").GetString());
        Assert.Equal("FISEI", j.RootElement.GetProperty("origen").GetProperty("facultad").GetString());
    }

    [Fact]
    public async Task Programa_cambiado_despues_de_revisar_queda_como_revision_no_vigente()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin();
        var o = await Carrera(admin, "HU09 O"); var r = await Carrera(admin, "HU09 R");
        var (a1, u1) = await Asignatura(admin, o.Id, "HU09-X1"); var (b1, v1) = await Asignatura(admin, r.Id, "HU09-Y1");
        await admin.PutAsJsonAsync($"/api/asignaturas/{a1.Id}/correspondencias/{b1.Id}", new { contenidos = Array.Empty<object>(), resultados = Array.Empty<object>() });
        await admin.PutAsJsonAsync($"/api/asignaturas/{a1.Id}/perfil-comparacion", new { planAnalitico = "plan-modificado", unidadHoras = "hora-reloj", sistemaCreditos = "sistema-sintetico", esSintetico = true });
        using var c = Cliente(f, "Docente");
        using var j = JsonDocument.Parse(await c.GetStringAsync($"/api/consulta-homologables/carreras/{o.Id}/{r.Id}/contexto"));
        Assert.False(Assert.Single(j.RootElement.GetProperty("pares").EnumerateArray()).GetProperty("revision").GetProperty("vigente").GetBoolean());
    }

    [Fact]
    public async Task Lista_carreras_FISEI_para_docente_y_coordinador()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin();
        var fisei = await Carrera(admin, "HU09 FISEI");
        using var c = Cliente(f, "Docente");
        var lista = (await c.GetFromJsonAsync<CarreraDto[]>("/api/consulta-homologables/carreras"))!;
        Assert.Contains(lista, x => x.Id == fisei.Id);
        Assert.All(lista, x => Assert.Equal("FISEI", x.Facultad, ignoreCase: true));
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Administrador", 403)]
    public async Task CA4_protege_la_consulta(string? rol, int esperado)
    {
        using var f = new CatalogoFactory(); using var c = f.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        Assert.Equal(esperado, (int)(await c.GetAsync($"/api/consulta-homologables/carreras/{Guid.NewGuid()}/{Guid.NewGuid()}/contexto")).StatusCode);
        Assert.Equal(esperado, (int)(await c.GetAsync("/api/consulta-homologables/carreras")).StatusCode);
    }

    [Fact]
    public async Task Carrera_inexistente_es_404()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin();
        var o = await Carrera(admin, "HU09 sola");
        using var c = Cliente(f, "Coordinador");
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/consulta-homologables/carreras/{o.Id}/{Guid.NewGuid()}/contexto")).StatusCode);
    }
}

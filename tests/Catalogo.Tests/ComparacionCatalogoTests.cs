using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalogo.Application;
using Xunit;

namespace Catalogo.Tests;

public class ComparacionCatalogoTests
{
    [Fact]
    public async Task Perfil_persistido_revision_explicita_y_deteccion_de_programa_modificado()
    {
        using var factory = new CatalogoFactory(); using var client = factory.Admin();
        var c = (await (await client.PostAsJsonAsync("/api/carreras", new { nombre = "Demo", facultad = "FISEI", vigencia = true })).Content.ReadFromJsonAsync<CarreraDto>())!;
        async Task<AsignaturaDto> Crear(string codigo) => (await (await client.PostAsJsonAsync("/api/asignaturas", new { carreraId = c.Id, codigo, nombre = codigo, nivel = 1, horas = 100, creditos = 3 })).Content.ReadFromJsonAsync<AsignaturaDto>())!;
        var o = await Crear("DEMO-O"); var r = await Crear("DEMO-R");
        var perfil = new { planAnalitico = "Plan sintético v1", unidadHoras = "hora-reloj", sistemaCreditos = "DEMO", esSintetico = true };
        var ruta = $"/api/asignaturas/{o.Id}/perfil-comparacion";
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(ruta, perfil)).StatusCode);
        Assert.Contains("Plan sintético v1", await client.GetStringAsync(ruta));
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/asignaturas/{r.Id}/perfil-comparacion", perfil)).StatusCode);
        var par = $"/api/asignaturas/{o.Id}/correspondencias/{r.Id}";
        using (var sinRevision = JsonDocument.Parse(await client.GetStringAsync(par))) Assert.False(sinRevision.RootElement.GetProperty("registrada").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(par, new { contenidos = Array.Empty<object>(), resultados = Array.Empty<object>() })).StatusCode);
        foreach (var a in new[] { o, r }) Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/api/asignaturas/{a.Id}/unidades", new { numero = 1, nombre = "Unidad DEMO", horas = 100, resultadoAprendizaje = "Resultado DEMO", contenidos = new[] { "Tema DEMO" } })).StatusCode);
        using var od = JsonDocument.Parse(await client.GetStringAsync($"/api/asignaturas/{o.Id}"));
        using var rd = JsonDocument.Parse(await client.GetStringAsync($"/api/asignaturas/{r.Id}"));
        var ou = od.RootElement.GetProperty("unidades")[0]; var ru = rd.RootElement.GetProperty("unidades")[0];
        var vinculo = new { elementoOrigen = ou.GetProperty("contenidos")[0].GetProperty("id").GetString(), elementoReceptor = ru.GetProperty("contenidos")[0].GetProperty("id").GetString(), ubicacionOrigen = "U1, tema DEMO", ubicacionReceptora = "U1, tema DEMO" };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(par, new { contenidos = new[] { vinculo }, resultados = Array.Empty<object>() })).StatusCode);
        using (var guardada = JsonDocument.Parse(await client.GetStringAsync(par))) Assert.Single(guardada.RootElement.GetProperty("contenidos").EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(par, new { contenidos = new[] { vinculo, vinculo }, resultados = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(par, new { contenidos = new[] { new { vinculo.elementoOrigen, vinculo.elementoReceptor, ubicacionOrigen = "", ubicacionReceptora = "" } }, resultados = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(par, new { contenidos = Array.Empty<object>(), resultados = Array.Empty<object>() })).StatusCode);
        using (var revision = JsonDocument.Parse(await client.GetStringAsync(par))) Assert.True(revision.RootElement.GetProperty("vigente").GetBoolean());
        await client.PutAsJsonAsync(ruta, new { planAnalitico = "Plan sintético v2", unidadHoras = "hora-reloj", sistemaCreditos = "DEMO", esSintetico = true });
        using (var obsoleta = JsonDocument.Parse(await client.GetStringAsync(par))) Assert.False(obsoleta.RootElement.GetProperty("vigente").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(par, new { contenidos = new[] { new { elementoOrigen = "inexistente", elementoReceptor = "inexistente", ubicacionOrigen = "u1", ubicacionReceptora = "u1" } }, resultados = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/asignaturas/{o.Id}/correspondencias/{o.Id}", new { contenidos = Array.Empty<object>(), resultados = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(ruta, new { planAnalitico = "", unidadHoras = "", sistemaCreditos = "" })).StatusCode);
        using var anon = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync(ruta)).StatusCode);
        anon.DefaultRequestHeaders.Authorization = new("Bearer", factory.Token("Docente"));
        Assert.Equal(HttpStatusCode.Forbidden, (await anon.PutAsJsonAsync(ruta, perfil)).StatusCode);
        Assert.Contains("perfil-comparacion", await client.GetStringAsync("/swagger/v1/swagger.json"));
    }
}

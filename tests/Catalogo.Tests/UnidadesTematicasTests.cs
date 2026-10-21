using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalogo.Application;
using Xunit;

namespace Catalogo.Tests;
public class UnidadesTematicasTests
{
    [Fact]
    public async Task Agrupa_contenidos_por_tematica_y_conserva_horas_y_resultado_en_curricular()
    {
        using var factory = new CatalogoFactory(); using var client = factory.Admin();
        var carrera = await (await client.PostAsJsonAsync("/api/carreras", new { nombre = "Software", facultad = "FISEI", vigencia = true })).Content.ReadFromJsonAsync<CarreraDto>();
        var a = await (await client.PostAsJsonAsync("/api/asignaturas", new { carreraId = carrera!.Id, codigo = "TEM-SINT", nombre = "Sintética", nivel = 1, creditos = 3, horas = 144 })).Content.ReadFromJsonAsync<AsignaturaDto>();
        var ruta = $"/api/asignaturas/{a!.Id}/unidades";
        var body = new { numero = 1, nombre = "Unidad", horas = 36, resultadoAprendizaje = "Resultado común", tematicas = new[] {
            new { nombre = "Temática A", contenidos = new[] { "A1", "A2" } }, new { nombre = "Temática B", contenidos = new[] { "B1" } } } };
        var response = await client.PostAsJsonAsync(ruta, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("tematicas").GetArrayLength());
        Assert.Equal(2, json.GetProperty("tematicas")[0].GetProperty("contenidos").GetArrayLength());
        Assert.Equal("B1", json.GetProperty("tematicas")[1].GetProperty("contenidos")[0].GetProperty("tema").GetString());
        Assert.Equal(36, json.GetProperty("horas").GetInt32());
        var id = json.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"{ruta}/{id}", body)).StatusCode);
        using var detalle = JsonDocument.Parse(await client.GetStringAsync($"/api/asignaturas/{a.Id}"));
        Assert.Equal(2, detalle.RootElement.GetProperty("unidades")[0].GetProperty("tematicas").GetArrayLength());
        var reducida = new { numero = 1, nombre = "Unidad", horas = 36, resultadoAprendizaje = "Resultado común", tematicas = new[] { new { nombre = "Unificada", contenidos = new[] { "A1", "A2", "B1" } } } };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"{ruta}/{id}", reducida)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"{ruta}/{id}", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(ruta, new { numero = 2, nombre = "Unidad", horas = 36, resultadoAprendizaje = "R", tematicas = new[] { new { nombre = "", contenidos = new[] { "A" } } } })).StatusCode);
    }

    [Fact]
    public async Task Contenidos_anteriores_no_se_clasifican_por_inferencia()
    {
        var u = new Catalogo.Domain.UnidadCurricular(Guid.NewGuid(), 1, "Anterior", 36, "Resultado", new[] { "Texto anterior" });
        var dto = UnidadDto.From(u);
        Assert.Empty(dto.Tematicas);
        Assert.Equal("Texto anterior", Assert.Single(dto.ContenidosSinClasificar).Tema);
        var id = u.Contenidos[0].Id;
        u.Clasificar(new[] { new Catalogo.Domain.TematicaDatos("Nombre confirmado", new[] { "Texto anterior" }) });
        Assert.Equal(id, u.Contenidos[0].Id);
        Assert.Empty(UnidadDto.From(u).ContenidosSinClasificar);
        await Task.CompletedTask;
    }
}

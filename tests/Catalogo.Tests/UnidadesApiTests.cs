using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalogo.Application;
using Xunit;

namespace Catalogo.Tests;

public class UnidadesApiTests
{
    [Fact]
    public async Task Registra_consulta_orden_y_rechaza_duplicados_y_datos_incompletos()
    {
        using var factory = new CatalogoFactory();
        using var client = factory.Admin();
        var carrera = await (await client.PostAsJsonAsync("/api/carreras", new { nombre = "Software", facultad = "FISEI", vigencia = true })).Content.ReadFromJsonAsync<CarreraDto>();
        var asignatura = await (await client.PostAsJsonAsync("/api/asignaturas", new { carreraId = carrera!.Id, codigo = "SINTETICO", nombre = "Asignatura sintética", nivel = 1, creditos = 3, horas = 144 })).Content.ReadFromJsonAsync<AsignaturaDto>();
        var path = $"/api/asignaturas/{asignatura!.Id}/unidades";
        var input = new { numero = 1, nombre = "Unidad sintética", horas = 36, resultadoAprendizaje = "Resultado sintético", contenidos = new[] { "Tema sintético A", "Tema sintético B" } };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(path, input)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path, input)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, new { numero = 2, nombre = "", horas = 0, contenidos = Array.Empty<string>() })).StatusCode);
        using var detalle = JsonDocument.Parse(await client.GetStringAsync($"/api/asignaturas/{asignatura.Id}"));
        var unidades = detalle.RootElement.GetProperty("unidades");
        Assert.Equal(1, unidades.GetArrayLength());
        Assert.Equal(2, unidades[0].GetProperty("contenidos").GetArrayLength());
        Assert.Equal("Resultado sintético", unidades[0].GetProperty("resultadoAprendizaje").GetString());
        var unidadId = unidades[0].GetProperty("id").GetGuid();
        var editar = $"{path}/{unidadId}";
        var corregida = new { numero = 2, nombre = "Unidad corregida", horas = 40, resultadoAprendizaje = "Resultado corregido", contenidos = new[] { "Contenido agrupado" } };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(editar, corregida)).StatusCode);
        using var actualizado = JsonDocument.Parse(await client.GetStringAsync($"/api/asignaturas/{asignatura.Id}"));
        var editada = actualizado.RootElement.GetProperty("unidades")[0];
        Assert.Equal(unidadId, editada.GetProperty("id").GetGuid());
        Assert.Equal("Unidad corregida", editada.GetProperty("nombre").GetString());
        Assert.Equal(1, editada.GetProperty("contenidos").GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(editar, new { numero = 2, nombre = "", horas = 40, resultadoAprendizaje = "", contenidos = Array.Empty<string>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(path, input)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(editar, input)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/asignaturas/{Guid.NewGuid()}/unidades/{unidadId}", corregida)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/asignaturas/{Guid.NewGuid()}/unidades", input)).StatusCode);
        using var anonimo = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync(path, input)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PutAsJsonAsync(editar, corregida)).StatusCode);
        anonimo.DefaultRequestHeaders.Authorization = new("Bearer", factory.Token("Docente"));
        Assert.Equal(HttpStatusCode.Forbidden, (await anonimo.PutAsJsonAsync(editar, corregida)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await anonimo.PostAsJsonAsync(path, input)).StatusCode);
    }
}

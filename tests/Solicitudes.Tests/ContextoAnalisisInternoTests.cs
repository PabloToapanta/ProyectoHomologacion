using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Solicitudes.Tests;

public class ContextoAnalisisInternoTests
{
    private static string Ruta(Guid id) => $"/api/solicitudes/{id}/contexto-analisis-interno";
    private static async Task<Guid> Crear(SolicitudesFactory f)
    {
        using var coordinador = f.Client();
        using var response = await coordinador.PostAsJsonAsync("/api/solicitudes", f.Input());
        var s = await response.Content.ReadFromJsonAsync<SolicitudesApiTests.SolicitudView>();
        var (ana, luis, _) = f.Directorio.Docentes;
        Assert.Equal(HttpStatusCode.OK, (await coordinador.PostAsJsonAsync($"/api/solicitudes/{s!.Id}/docentes", new { docenteIds = new[] { ana.Id, luis.Id } })).StatusCode);
        return s.Id;
    }
    [Fact]
    public async Task Docente_asignado_recibe_solo_contexto_academico()
    {
        using var f = new SolicitudesFactory(); var id = await Crear(f);
        using (var coordinador = f.Client())
        {
            Assert.Equal(HttpStatusCode.Created, (await coordinador.PostAsJsonAsync($"/api/solicitudes/{id}/asignaturas-aprobadas",
                new { asignaturaOrigenId = f.Catalogo.Algebra.Id, nota = 8, fechaAprobacion = "2026-06-01", numeroMatricula = "MATRICULA-SINTETICA-HU18", intentoMatricula = 1 })).StatusCode);
        }
        using var docente = f.Client("Docente", f.Directorio.Docentes.Ana.Id.ToString());
        using var response = await docente.GetAsync(Ruta(id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(id, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Interno", json.RootElement.GetProperty("tipoCambio").GetString());
        var aprobada = json.RootElement.GetProperty("asignaturasAprobadas")[0];
        Assert.Equal(f.Catalogo.Algebra.Id, aprobada.GetProperty("asignaturaOrigenId").GetGuid());
        Assert.Equal(3, aprobada.GetProperty("creditos").GetDecimal());
        Assert.Equal(144, aprobada.GetProperty("horas").GetInt32());
        Assert.Equal("2026-06-01", aprobada.GetProperty("fechaAprobacion").GetString());
        Assert.DoesNotContain("MATRICULA-SINTETICA-HU18", body);
        foreach (var key in new[] { "numeroQuipux", "identificacionEstudiante", "nombreEstudiante", "numeroMatricula", "docentesAsignados" })
            Assert.DoesNotContain(key, body);
    }
    [Fact]
    public async Task Solicitud_ajena_e_inexistente_tienen_el_mismo_rechazo()
    {
        using var f = new SolicitudesFactory(); var id = await Crear(f);
        using var otro = f.Client("Docente", f.Directorio.Docentes.Rosa.Id.ToString());
        using var ajena = await otro.GetAsync(Ruta(id)); using var inexistente = await otro.GetAsync(Ruta(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, ajena.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        using var a = JsonDocument.Parse(await ajena.Content.ReadAsStringAsync());
        using var b = JsonDocument.Parse(await inexistente.Content.ReadAsStringAsync());
        Assert.Equal(a.RootElement.GetProperty("title").GetString(), b.RootElement.GetProperty("title").GetString());
    }
    [Theory]
    [InlineData(null, 401)]
    [InlineData("Administrador", 403)]
    [InlineData("Coordinador", 403)]
    public async Task Otros_roles_no_obtienen_contexto(string? role, int esperado)
    {
        using var f = new SolicitudesFactory(); using var client = f.Client(role, Guid.NewGuid().ToString());
        Assert.Equal(esperado, (int)(await client.GetAsync(Ruta(Guid.NewGuid()))).StatusCode);
    }
    [Fact]
    public async Task Docente_sin_identificador_valido_no_accede()
    {
        using var f = new SolicitudesFactory(); using var client = f.Client("Docente");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Ruta(Guid.NewGuid()))).StatusCode);
    }
}

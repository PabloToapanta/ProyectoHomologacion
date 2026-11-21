using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Solicitudes.Tests;

/// <summary>HU-19: lectura interna del contexto externo, limitada al docente asignado y sin datos personales.</summary>
public class ContextoAnalisisExternoTests
{
    private static string Ruta(Guid id) => $"/api/solicitudes/{id}/contexto-analisis-externo";
    private static async Task<Guid> CrearAsignada(SolicitudesFactory f, int? periodos, int? cambios)
    {
        using var coordinador = f.Client();
        using var response = await coordinador.PostAsJsonAsync("/api/solicitudes", new
        {
            numeroQuipux = "Q-EXT-CTX", identificacionEstudiante = "EST-SINTETICO-CTX", nombreEstudiante = "Estudiante sintético contexto", tipoCambio = "Externo",
            institucionOrigen = "Universidad sintética Ecuador", carreraOrigenExterna = "Carrera externa sintética", carreraReceptoraId = f.Catalogo.Receptora,
            periodosCursados = periodos, cambiosPreviosCarrera = cambios
        });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Created, (await coordinador.PostAsJsonAsync($"/api/solicitudes/{id}/asignaturas-aprobadas", new
        {
            nombreAsignatura = "Programación externa sintética", creditos = 4, sistemaCreditos = "Créditos sintéticos", horas = 128, unidadHoras = "Horas sintéticas",
            referenciaPrograma = "programa-externo-sintetico", nota = 85, notaMaximaEscala = 100, notaMinimaAprobacion = 70,
            fechaAprobacion = "2025-07-15", numeroMatricula = "MATRICULA-EXTERNA-SINTETICA", intentoMatricula = 1
        })).StatusCode);
        var (ana, luis, _) = f.Directorio.Docentes;
        Assert.Equal(HttpStatusCode.OK, (await coordinador.PostAsJsonAsync($"/api/solicitudes/{id}/docentes", new { docenteIds = new[] { ana.Id, luis.Id } })).StatusCode);
        return id;
    }

    [Fact]
    public async Task Docente_asignado_recibe_datos_academicos_y_de_RN05_sin_datos_personales()
    {
        using var f = new SolicitudesFactory(); var id = await CrearAsignada(f, 3, 0);
        using var docente = f.Client("Docente", f.Directorio.Docentes.Luis.Id.ToString());
        using var response = await docente.GetAsync(Ruta(id));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body); var r = json.RootElement;
        Assert.Equal(id, r.GetProperty("id").GetGuid());
        Assert.Equal("Externo", r.GetProperty("tipoCambio").GetString());
        Assert.Equal(f.Catalogo.Receptora, r.GetProperty("carreraReceptoraId").GetGuid());
        Assert.Equal("Universidad sintética Ecuador", r.GetProperty("institucionOrigen").GetString());
        Assert.Equal(3, r.GetProperty("periodosCursados").GetInt32());
        Assert.Equal(0, r.GetProperty("cambiosPreviosCarrera").GetInt32());
        var a = r.GetProperty("asignaturasAprobadas")[0];
        Assert.Equal("Programación externa sintética", a.GetProperty("nombre").GetString());
        Assert.Equal(128, a.GetProperty("horas").GetInt32());
        Assert.Equal("Horas sintéticas", a.GetProperty("unidadHoras").GetString());
        Assert.Equal(4, a.GetProperty("creditos").GetDecimal());
        Assert.Equal("Créditos sintéticos", a.GetProperty("sistemaCreditos").GetString());
        Assert.Equal("programa-externo-sintetico", a.GetProperty("referenciaPrograma").GetString());
        Assert.Equal("Aprobada", a.GetProperty("estado").GetString());
        Assert.Equal("2025-07-15", a.GetProperty("fechaAprobacion").GetString());
        foreach (var dato in new[] { "Q-EXT-CTX", "EST-SINTETICO-CTX", "Estudiante sintético contexto", "MATRICULA-EXTERNA-SINTETICA", "docente.ana" })
            Assert.DoesNotContain(dato, body);
    }

    [Fact]
    public async Task Datos_no_declarados_llegan_como_nulos()
    {
        using var f = new SolicitudesFactory(); var id = await CrearAsignada(f, null, null);
        using var docente = f.Client("Docente", f.Directorio.Docentes.Ana.Id.ToString());
        using var json = JsonDocument.Parse(await docente.GetStringAsync(Ruta(id)));
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("periodosCursados").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("cambiosPreviosCarrera").ValueKind);
    }

    [Fact]
    public async Task Solicitud_ajena_e_inexistente_tienen_el_mismo_rechazo()
    {
        using var f = new SolicitudesFactory(); var id = await CrearAsignada(f, 3, 0);
        using var otro = f.Client("Docente", f.Directorio.Docentes.Rosa.Id.ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync(Ruta(id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync(Ruta(Guid.NewGuid()))).StatusCode);
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
}

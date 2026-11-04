using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Solicitudes.Domain;
using Xunit;

namespace Solicitudes.Tests;

/// <summary>HU-19 (D4): períodos cursados y cambios previos declarados por el Coordinador para RN-05.</summary>
public class DatosCambioExternoTests
{
    private static Solicitud Externa(int? periodos = null, int? cambios = null) => new("Q-EXT", "EST-SINTETICO", "Estudiante sintético", "Externo",
        null, "Universidad sintética Ecuador", "Carrera externa sintética", Guid.NewGuid(), periodos, cambios);

    [Fact]
    public void Externa_conserva_los_datos_declarados()
    {
        var s = Externa(3, 0);
        Assert.Equal(3, s.PeriodosCursados);
        Assert.Equal(0, s.CambiosPreviosCarrera);
    }

    [Fact]
    public void Datos_ausentes_quedan_sin_dato_y_no_se_presumen()
    {
        var s = Externa();
        Assert.Null(s.PeriodosCursados);
        Assert.Null(s.CambiosPreviosCarrera);
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, -1)]
    [InlineData(101, null)]
    [InlineData(null, 101)]
    public void Rechaza_valores_fuera_de_rango(int? periodos, int? cambios) =>
        Assert.Throws<SolicitudException>(() => Externa(periodos, cambios));

    [Theory]
    [InlineData(2, null)]
    [InlineData(null, 0)]
    public void Cambio_interno_no_admite_datos_externos(int? periodos, int? cambios) =>
        Assert.Throws<SolicitudException>(() => new Solicitud("Q-INT", "EST", "Nombre", "Interno", Guid.NewGuid(), null, null, Guid.NewGuid(), periodos, cambios));

    [Fact]
    public void Se_corrigen_mientras_la_solicitud_esta_registrada()
    {
        var s = Externa(1, null);
        s.DeclararDatosCambioExterno(4, 1);
        Assert.Equal(4, s.PeriodosCursados);
        Assert.Equal(1, s.CambiosPreviosCarrera);
    }

    private static string Datos(Guid id) => $"/api/solicitudes/{id}/datos-cambio-externo";
    private static object Input(SolicitudesFactory f, int? periodos, int? cambios) => new
    {
        numeroQuipux = "Q-EXT-API", identificacionEstudiante = "EST-SINTETICO", nombreEstudiante = "Estudiante sintético", tipoCambio = "Externo",
        institucionOrigen = "Universidad sintética Ecuador", carreraOrigenExterna = "Carrera externa sintética", carreraReceptoraId = f.Catalogo.Receptora,
        periodosCursados = periodos, cambiosPreviosCarrera = cambios
    };
    private static async Task<Guid> CrearExterna(SolicitudesFactory f, HttpClient c, int? periodos = 3, int? cambios = 0)
    {
        using var response = await c.PostAsJsonAsync("/api/solicitudes", Input(f, periodos, cambios));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Registro_y_detalle_muestran_los_datos_declarados()
    {
        using var f = new SolicitudesFactory(); using var c = f.Client();
        var id = await CrearExterna(f, c);
        using var json = JsonDocument.Parse(await c.GetStringAsync($"/api/solicitudes/{id}"));
        Assert.Equal(3, json.RootElement.GetProperty("periodosCursados").GetInt32());
        Assert.Equal(0, json.RootElement.GetProperty("cambiosPreviosCarrera").GetInt32());
    }

    [Fact]
    public async Task Coordinador_corrige_antes_de_asignar_docentes_y_luego_no()
    {
        using var f = new SolicitudesFactory(); using var c = f.Client();
        var id = await CrearExterna(f, c, null, null);
        using (var ok = await c.PutAsJsonAsync(Datos(id), new { periodosCursados = 2, cambiosPreviosCarrera = 0 }))
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        using (var json = JsonDocument.Parse(await c.GetStringAsync($"/api/solicitudes/{id}")))
            Assert.Equal(2, json.RootElement.GetProperty("periodosCursados").GetInt32());
        var (ana, luis, _) = f.Directorio.Docentes;
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/solicitudes/{id}/docentes", new { docenteIds = new[] { ana.Id, luis.Id } })).StatusCode);
        using var tarde = await c.PutAsJsonAsync(Datos(id), new { periodosCursados = 5, cambiosPreviosCarrera = 0 });
        Assert.Equal(HttpStatusCode.Conflict, tarde.StatusCode);
    }

    [Fact]
    public async Task Interna_y_valores_invalidos_se_rechazan()
    {
        using var f = new SolicitudesFactory(); using var c = f.Client();
        using var interna = await c.PostAsJsonAsync("/api/solicitudes", f.Input());
        using var json = JsonDocument.Parse(await interna.Content.ReadAsStringAsync());
        var idInterna = json.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync(Datos(idInterna), new { periodosCursados = 2, cambiosPreviosCarrera = 0 })).StatusCode);
        var id = await CrearExterna(f, c);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync(Datos(id), new { periodosCursados = -1, cambiosPreviosCarrera = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync(Datos(Guid.NewGuid()), new { periodosCursados = 2, cambiosPreviosCarrera = 0 })).StatusCode);
    }

    [Fact]
    public async Task Docente_no_modifica_datos_de_la_solicitud()
    {
        using var f = new SolicitudesFactory(); using var c = f.Client("Docente", f.Directorio.Docentes.Ana.Id.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PutAsJsonAsync(Datos(Guid.NewGuid()), new { periodosCursados = 2, cambiosPreviosCarrera = 0 })).StatusCode);
    }
}

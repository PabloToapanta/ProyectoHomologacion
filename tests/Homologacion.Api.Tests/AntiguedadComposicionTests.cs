using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Homologacion.Api.Tests;

// MotorFactory configura solo JWT y umbral: usa los registros DI de Program.
public class AntiguedadComposicionTests
{
    [Theory]
    [InlineData("Aprobada", "2017-10-20", "Elegible")]
    [InlineData("Aprobada", "2016-10-20", "Elegible")]
    [InlineData("Aprobada", "2016-10-19", "RequiereValidacionConocimientos")]
    [InlineData("Reprobada", null, "NoElegible")]
    [InlineData("NoCursada", null, "NoElegible")]
    public async Task Arranque_normal_resuelve_el_caso_de_uso(string estado, string? fecha, string esperado)
    {
        using var factory = new MotorFactory();
        using var client = factory.Autenticado();
        var response = await client.PostAsJsonAsync("/api/homologaciones/validar-antiguedad",
            new { estadoAsignatura = estado, fechaAprobacion = fecha, fechaEvaluacion = "2026-10-20" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(esperado, body.GetProperty("estado").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("motivo").GetString()));
    }

    [Theory]
    [InlineData("Administrador", HttpStatusCode.OK)]
    [InlineData("Docente", HttpStatusCode.OK)]
    [InlineData("Coordinador", HttpStatusCode.OK)]
    [InlineData("Estudiante", HttpStatusCode.Forbidden)]
    public async Task Composicion_conserva_la_politica_de_roles(string rol, HttpStatusCode esperado)
    {
        using var factory = new MotorFactory();
        using var client = factory.Autenticado(rol);
        var response = await client.PostAsJsonAsync("/api/homologaciones/validar-antiguedad",
            new { estadoAsignatura = "Aprobada", fechaAprobacion = "2016-10-20", fechaEvaluacion = "2026-10-20" });

        Assert.Equal(esperado, response.StatusCode);
    }
}

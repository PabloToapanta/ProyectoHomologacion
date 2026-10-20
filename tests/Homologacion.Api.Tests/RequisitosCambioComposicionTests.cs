using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Homologacion.Api.Tests;

// Usa los registros DI de Program, sin agregar el caso de uso desde el test.
public class RequisitosCambioComposicionTests
{
    [Theory]
    [InlineData("Interno", 1, 1, 0, true)]
    [InlineData("Interno", 1, 0, 0, false)]
    [InlineData("Interno", 0, 1, 0, false)]
    [InlineData("Externo", 2, 2, 0, true)]
    [InlineData("Externo", 2, 1, 0, false)]
    [InlineData("Externo", 1, 2, 0, false)]
    [InlineData("Externo", 2, 2, 1, false)]
    public async Task Arranque_normal_resuelve_requisitos(string tipo, int periodos, int homologables,
        int cambios, bool esperado)
    {
        using var factory = new MotorFactory();
        using var client = factory.Autenticado();
        var response = await client.PostAsJsonAsync("/api/homologaciones/validar-requisitos", new
        {
            tipoCambio = tipo, periodosCursados = periodos,
            asignaturasHomologables = homologables, cambiosPrevios = cambios
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(esperado, body.GetProperty("cumple").GetBoolean());
        Assert.Equal(esperado, body.GetProperty("incumplimientos").GetArrayLength() == 0);
    }

    [Theory]
    [InlineData("Administrador", HttpStatusCode.OK)]
    [InlineData("Docente", HttpStatusCode.OK)]
    [InlineData("Coordinador", HttpStatusCode.OK)]
    [InlineData("Estudiante", HttpStatusCode.Forbidden)]
    public async Task Composicion_conserva_roles(string rol, HttpStatusCode esperado)
    {
        using var factory = new MotorFactory();
        using var client = factory.Autenticado(rol);
        var response = await client.PostAsJsonAsync("/api/homologaciones/validar-requisitos",
            new { tipoCambio = "Interno", periodosCursados = 1, asignaturasHomologables = 1, cambiosPrevios = 0 });
        Assert.Equal(esperado, response.StatusCode);
    }
}

using System.Net;
using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.ConsultaHomologables;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Homologacion.Api.Tests;

/// <summary>HU-09: endpoint de consulta de homologables entre carreras FISEI.</summary>
public class ConsultaHomologablesEndpointTests
{
    private static readonly Guid O = Guid.Parse("09000000-0000-0000-0000-000000000011");
    private static readonly Guid R = Guid.Parse("09000000-0000-0000-0000-000000000012");
    private sealed class Catalogo : ICatalogoConsultaHomologables
    {
        public int Lecturas { get; private set; }
        public bool Caido { get; set; }
        public Task<IReadOnlyList<CarreraConsulta>> CarrerasAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CarreraConsulta>>([new(O, "Origen sintético", "FISEI", true), new(R, "Receptora sintética", "FISEI", true)]);
        public Task<ContextoConsultaCatalogo?> ContextoAsync(Guid o, Guid r, CancellationToken ct)
        {
            Lecturas++;
            if (Caido) throw new FuenteAnalisisNoDisponibleException();
            return Task.FromResult(o == O && r == R ? new ContextoConsultaCatalogo(new(O, "Origen sintético", "FISEI", true), new(R, "Receptora sintética", "FISEI", true), [], []) : null);
        }
    }
    private static WebApplicationFactory<Homologacion.Api.Program> Host(MotorFactory f, Catalogo catalogo) => f.WithWebHostBuilder(b =>
    {
        b.UseSetting("AnalisisInterno:Habilitado", "false");
        b.ConfigureServices(s => { s.RemoveAll<ICatalogoConsultaHomologables>(); s.AddSingleton<ICatalogoConsultaHomologables>(catalogo); });
    });
    private static string Ruta(Guid o, Guid r) => $"/api/homologaciones/consulta-homologables?origen={o}&receptora={r}";

    [Theory]
    [InlineData("Docente")]
    [InlineData("Coordinador")]
    public async Task CA4_docente_y_coordinador_consultan(string rol)
    {
        using var f = new MotorFactory(); var catalogo = new Catalogo(); using var host = Host(f, catalogo); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        using var response = await c.GetAsync(Ruta(O, R));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var j = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(O, j.RootElement.GetProperty("origen").GetProperty("id").GetGuid());
        Assert.Empty(j.RootElement.GetProperty("pares").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/homologaciones/consulta-homologables/carreras")).StatusCode);
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("Administrador", 403)]
    public async Task CA4_sin_sesion_u_otro_rol(string? rol, int esperado)
    {
        using var f = new MotorFactory(); var catalogo = new Catalogo(); using var host = Host(f, catalogo); using var c = host.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol));
        Assert.Equal(esperado, (int)(await c.GetAsync(Ruta(O, R))).StatusCode);
        Assert.Equal(esperado, (int)(await c.GetAsync("/api/homologaciones/consulta-homologables/carreras")).StatusCode);
        Assert.Equal(0, catalogo.Lecturas);
    }

    [Fact]
    public async Task Errores_400_404_y_503()
    {
        using var f = new MotorFactory(); var catalogo = new Catalogo(); using var host = Host(f, catalogo); using var c = host.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Docente"));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(Ruta(O, O))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Ruta(O, Guid.NewGuid()))).StatusCode);
        catalogo.Caido = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await c.GetAsync(Ruta(O, R))).StatusCode);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Homologacion.Application.Configuracion;
using Homologacion.Domain.Configuracion;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Homologacion.Api.Tests;

public class ConfiguracionEndpointTests
{
    private const string Ruta = "/api/homologaciones/configuracion";
    private sealed class Store : IConfiguracionHomologacionStore
    {
        private ParametrosHomologacion actual = ParametrosHomologacion.Predeterminados(Guid.Parse("9e0ce28d-e93a-4cad-b650-48ad9b260301"));
        private readonly List<EntradaHistorialConfiguracion> entradas = [];
        public int Escrituras { get; private set; }
        public Task<ParametrosHomologacion> ObtenerVigentesAsync(CancellationToken ct) => Task.FromResult(actual);
        public Task<ParametrosHomologacion> ActualizarAsync(Guid esperada, ParametrosHomologacion nueva, Guid actor,
            DateTimeOffset fecha, string motivo, string respaldo, CancellationToken ct)
        {
            if (esperada != actual.VersionId) throw new ConfiguracionObsoletaException();
            entradas.Insert(0, new(nueva.VersionId, actual.VersionId, fecha, actor, actual, nueva, motivo, respaldo));
            Escrituras++; actual = nueva; return Task.FromResult(nueva);
        }
        public Task<PaginaHistorialConfiguracion> HistorialAsync(Guid? cursor, int limit, CancellationToken ct)
        {
            var todos = entradas.Concat([new(actual.VersionId == Guid.Parse("9e0ce28d-e93a-4cad-b650-48ad9b260301")
                ? actual.VersionId : Guid.Parse("9e0ce28d-e93a-4cad-b650-48ad9b260301"), null,
                DateTimeOffset.UnixEpoch, Guid.Empty, null, ParametrosHomologacion.Predeterminados(Guid.Parse("9e0ce28d-e93a-4cad-b650-48ad9b260301")), "Inicial", "Sistema")]).ToList();
            var inicio = cursor is null ? 0 : todos.FindIndex(e => e.VersionId == cursor);
            if (inicio < 0) throw new ConfiguracionInvalidaException("Cursor inválido.");
            var pagina = todos.Skip(inicio).Take(limit).ToArray();
            var siguiente = inicio + limit < todos.Count ? todos[inicio + limit].VersionId : (Guid?)null;
            return Task.FromResult(new PaginaHistorialConfiguracion(pagina, siguiente));
        }
    }

    private static WebApplicationFactory<Homologacion.Api.Program> Host(MotorFactory motor, Store store) =>
        motor.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IConfiguracionHomologacionStore>();
            services.AddSingleton<IConfiguracionHomologacionStore>(store);
        }));

    private static object Datos(int umbral = 85) => new
    {
        umbralPct = umbral, antiguedadMaximaAnios = 12, periodosMinimosInterno = 2,
        asignaturasHomologablesMinimasInterno = 2, periodosMinimosExterno = 3,
        asignaturasHomologablesMinimasExterno = 3, motivo = " Ajuste académico ", respaldo = " Acta sintética "
    };
    private static async Task<HttpResponseMessage> Cambiar(HttpClient client, string etag, object datos)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, Ruta) { Content = JsonContent.Create(datos) };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Coordinador_consulta_actualiza_y_ve_historial_con_etag_y_auditoria()
    {
        using var motor = new MotorFactory(); var store = new Store(); using var host = Host(motor, store);
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", motor.Token("Coordinador"));
        var lectura = await client.GetAsync(Ruta);
        Assert.Equal(HttpStatusCode.OK, lectura.StatusCode);
        var etag = lectura.Headers.ETag!.ToString();
        Assert.Equal("\"9e0ce28d-e93a-4cad-b650-48ad9b260301\"", etag);
        var cambio = await Cambiar(client, etag, Datos());
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        var nuevo = await cambio.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(85, nuevo.GetProperty("umbralPct").GetInt32());
        Assert.NotEqual(etag, cambio.Headers.ETag!.ToString());
        Assert.Equal(1, store.Escrituras);
        var historial = await client.GetFromJsonAsync<JsonElement>(Ruta + "/historial?limit=1");
        var entrada = historial.GetProperty("entradas")[0];
        Assert.Equal(80, entrada.GetProperty("anterior").GetProperty("umbralPct").GetInt32());
        Assert.Equal(85, entrada.GetProperty("nueva").GetProperty("umbralPct").GetInt32());
        Assert.Equal("Ajuste académico", entrada.GetProperty("motivo").GetString());
        Assert.Equal("Acta sintética", entrada.GetProperty("respaldo").GetString());
        Assert.NotEqual(Guid.Empty, entrada.GetProperty("actorId").GetGuid());
        var cursor = historial.GetProperty("cursorSiguiente").GetGuid();
        var segunda = await client.GetFromJsonAsync<JsonElement>(Ruta + "/historial?limit=1&cursor=" + cursor);
        Assert.Single(segunda.GetProperty("entradas").EnumerateArray());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Administrador", HttpStatusCode.Forbidden)]
    [InlineData("Docente", HttpStatusCode.Forbidden)]
    public async Task Solo_coordinador_accede_a_las_tres_operaciones(string? rol, HttpStatusCode esperado)
    {
        using var motor = new MotorFactory(); var store = new Store(); using var host = Host(motor, store);
        using var client = host.CreateClient();
        if (rol is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", motor.Token(rol));
        Assert.Equal(esperado, (await client.GetAsync(Ruta)).StatusCode);
        Assert.Equal(esperado, (await client.GetAsync(Ruta + "/historial")).StatusCode);
        Assert.Equal(esperado, (await Cambiar(client, "\"9e0ce28d-e93a-4cad-b650-48ad9b260301\"", Datos())).StatusCode);
        Assert.Equal(0, store.Escrituras);
    }

    [Fact]
    public async Task Etag_obsoleto_y_peticion_invalida_no_escriben()
    {
        using var motor = new MotorFactory(); var store = new Store(); using var host = Host(motor, store);
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", motor.Token("Coordinador"));
        var etag = (await client.GetAsync(Ruta)).Headers.ETag!.ToString();
        Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(client, "", Datos())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(client, etag, Datos(101))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(client, etag, new { umbralPct = 80 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(client, etag,
            new { umbralPct = 80, antiguedadMaximaAnios = 10, periodosMinimosInterno = 1,
                asignaturasHomologablesMinimasInterno = 1, periodosMinimosExterno = 2,
                asignaturasHomologablesMinimasExterno = 2, motivo = "a", respaldo = "b", desconocido = true })).StatusCode);
        Assert.Equal(0, store.Escrituras);
        Assert.Equal(HttpStatusCode.OK, (await Cambiar(client, etag, Datos())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Cambiar(client, etag, Datos())).StatusCode);
        Assert.Equal(1, store.Escrituras);
    }

    [Fact]
    public async Task Swagger_documenta_configuracion_y_historial()
    {
        using var motor = new MotorFactory(); using var host = Host(motor, new Store());
        using var client = host.CreateClient();
        var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var rutas = swagger.GetProperty("paths");
        Assert.True(rutas.GetProperty(Ruta).TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty(Ruta).TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty(Ruta + "/historial").TryGetProperty("get", out _));
    }
}

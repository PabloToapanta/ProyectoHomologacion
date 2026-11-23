using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Configuracion;
using Homologacion.Domain.Configuracion;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Homologacion.Api.Tests;

public class AgrupacionesEndpointTests
{
    private sealed class Parametros : IParametrosHomologacionProvider
    { public Task<ParametrosHomologacion> ObtenerVigentesAsync(CancellationToken ct) => Task.FromResult(ParametrosHomologacion.Predeterminados(Guid.Parse("11111111-1111-1111-1111-111111111111"))); }
    [Fact]
    public async Task Borrador_etag_reservas_analisis_dos_revisiones_informe_y_cierre_integrados()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        using var f = new MotorFactory(); var e = new EscenarioInterno();
        var mallaOrigen = Guid.NewGuid();
        foreach (var idMateria in e.Materias.Keys.ToArray())
        {
            var materia = e.Materias[idMateria];
            e.Materias[idMateria] = materia with { Malla = new(idMateria == EscenarioInterno.OrigenId ? mallaOrigen : Guid.NewGuid(), materia.CarreraId, "Plan capturado HU07", idMateria != EscenarioInterno.OrigenId) };
        }
        e.Solicitud = e.Solicitud! with { AsignaturasAprobadas = [e.Solicitud!.AsignaturasAprobadas[0] with { IntentoMatricula = 1 }] };
        var r2 = Guid.NewGuid(); e.Materias[r2] = e.Materias[EscenarioInterno.ReceptoraId] with { Id = r2 };
        using var host = f.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ResultadosDbContext>>(); services.AddDbContext<ResultadosDbContext>(o => o.UseSqlite(connection));
            services.RemoveAll<ISolicitudesAnalisisInterno>(); services.AddSingleton<ISolicitudesAnalisisInterno>(e);
            services.RemoveAll<ICatalogoAnalisisInterno>(); services.AddSingleton<ICatalogoAnalisisInterno>(e);
            services.RemoveAll<IParametrosHomologacionProvider>(); services.AddSingleton<IParametrosHomologacionProvider>(new Parametros());
            services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new RelojAnalisis());
            services.RemoveAll<IReceptorasAnalisisInterno>(); services.AddSingleton<IReceptorasAnalisisInterno>(new Receptoras(r2));
        }));
        using var c = host.CreateClient(); using (var scope = host.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<ResultadosDbContext>().Database.EnsureCreatedAsync();
        using (var swagger = await c.GetAsync("/swagger/v1/swagger.json"))
        {
            using var schema = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync());
            var route = schema.RootElement.GetProperty("paths").GetProperty("/api/homologaciones/solicitudes/{id}/agrupaciones/{grupoId}");
            Assert.True(route.GetProperty("put").GetProperty("parameters").EnumerateArray().Single(x => x.GetProperty("name").GetString() == "If-Match").GetProperty("required").GetBoolean());
            Assert.True(route.GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("headers").TryGetProperty("ETag", out _));
            var schemas = schema.RootElement.GetProperty("components").GetProperty("schemas");
            var estadoHistorico = schemas.GetProperty("EvaluacionEquivalencia").GetProperty("properties").GetProperty("estado");
            if (estadoHistorico.TryGetProperty("$ref", out var referencia)) estadoHistorico = schemas.GetProperty(referencia.GetString()!.Split('/').Last());
            Assert.Equal("integer", estadoHistorico.GetProperty("type").GetString());
        }
        var token1 = f.Token("Docente"); var token2 = f.Token("Docente"); c.DefaultRequestHeaders.Authorization = new("Bearer", token1);
        var ruta = $"/api/homologaciones/solicitudes/{EscenarioInterno.SolicitudId}/agrupaciones";
        c.DefaultRequestHeaders.Authorization = null; Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync(ruta)).StatusCode);
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Coordinador")); Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync(ruta)).StatusCode);
        c.DefaultRequestHeaders.Authorization = new("Bearer", token1);
        object Input(decimal? horas = 40) => new
        {
            origenes = new[] { EscenarioInterno.AprobadaId },
            receptoras = new[] { EscenarioInterno.ReceptoraId, r2 },
            aportes = new[]{
            new{asignaturaAprobadaId=EscenarioInterno.AprobadaId,asignaturaReceptoraId=EscenarioInterno.ReceptoraId,horasAportadas=horas,creditosAportados=(decimal?)4},
            new{asignaturaAprobadaId=EscenarioInterno.AprobadaId,asignaturaReceptoraId=r2,horasAportadas=(decimal?)40,creditosAportados=(decimal?)4}}
        };
        using var created = await c.PostAsJsonAsync(ruta, Input(null)); Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement; var id = dto.GetProperty("grupoId").GetGuid();
        var etag = created.Headers.ETag!.ToString(); Assert.False(created.Headers.ETag.IsWeak);
        var detalle = ruta + "/" + id;
        Assert.Equal((HttpStatusCode)428, (await c.PutAsJsonAsync(detalle, Input())).StatusCode);
        c.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"obsoleta\""); Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync(detalle)).StatusCode);
        c.DefaultRequestHeaders.Remove("If-Match"); c.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag);
        using var incompleto = await c.PostAsync(detalle + "/analisis", null); Assert.Equal(HttpStatusCode.OK, incompleto.StatusCode);
        using var j = JsonDocument.Parse(await incompleto.Content.ReadAsStringAsync()); Assert.False(j.RootElement.GetProperty("guardado").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(ruta, Input())).StatusCode);
        using var updated = await c.PutAsJsonAsync(detalle, Input()); Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        etag = updated.Headers.ETag!.ToString(); c.DefaultRequestHeaders.Remove("If-Match"); c.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag);
        using var evaluated = await c.PostAsync(detalle + "/analisis", null); Assert.Equal(HttpStatusCode.OK, evaluated.StatusCode);
        using var a = JsonDocument.Parse(await evaluated.Content.ReadAsStringAsync()); Assert.True(a.RootElement.GetProperty("guardado").GetBoolean());
        Assert.Equal(2, a.RootElement.GetProperty("evaluacion").GetProperty("receptoras").GetArrayLength());
        etag = evaluated.Headers.ETag!.ToString(); c.DefaultRequestHeaders.Remove("If-Match"); c.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync(detalle)).StatusCode);
        e.Solicitud = e.Solicitud! with { AsignaturasAprobadas = [e.Solicitud.AsignaturasAprobadas[0] with { IntentoMatricula = 3 }] };
        using (var reanalizado = await c.PostAsync(detalle + "/analisis", null))
        {
            Assert.Equal(HttpStatusCode.OK, reanalizado.StatusCode);
            etag = reanalizado.Headers.ETag!.ToString();
        }
        c.DefaultRequestHeaders.Remove("If-Match"); c.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag);
        using (var persistido = JsonDocument.Parse(await c.GetStringAsync(detalle)))
            Assert.Contains(persistido.RootElement.GetProperty("ultimoAnalisis").GetProperty("evaluacion").GetProperty("informacionFaltante").EnumerateArray(), x => x.GetString()!.Contains("tercera matrícula"));
        var revision = new { postura = "Ajuste", decision = "NoHomologable", observacion = "Revisión sintética del grupo" };
        // Simula activar otra versión en Catálogo después del último análisis guardado.
        var origenActual = e.Materias[EscenarioInterno.OrigenId];
        e.Materias[origenActual.Id] = origenActual with { Malla = origenActual.Malla! with { Vigente = true } };
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync(detalle + "/revisiones", revision)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync(detalle + "/revisiones", revision)).StatusCode);
        // Una pareja histórica no se convierte en grupo; el informe debe leer ambas fuentes reales de DI.
        var historica = Guid.NewGuid();
        e.Solicitud = e.Solicitud! with { AsignaturasAprobadas = [.. e.Solicitud.AsignaturasAprobadas, e.Solicitud.AsignaturasAprobadas[0] with { Id = historica }] };
        var revisionPareja = new { asignaturaAprobadaId = historica, asignaturaReceptoraId = EscenarioInterno.ReceptoraId, postura = "Ajuste", decision = "NoHomologable", observacion = "Pareja histórica sintética" };
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/homologaciones/solicitudes/{EscenarioInterno.SolicitudId}/revisiones-docentes", revisionPareja)).StatusCode);
        var informe = $"/api/homologaciones/solicitudes/{EscenarioInterno.SolicitudId}/informe";
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync(informe, null)).StatusCode);
        c.DefaultRequestHeaders.Authorization = new("Bearer", token2);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/homologaciones/solicitudes/{EscenarioInterno.SolicitudId}/revisiones-docentes", revisionPareja)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync(detalle + "/revisiones", revision)).StatusCode);
        using var generado = await c.PostAsync(informe, null); Assert.Equal(HttpStatusCode.Created, generado.StatusCode);
        using var i = JsonDocument.Parse(await generado.Content.ReadAsStringAsync()); Assert.Single(i.RootElement.GetProperty("grupos").EnumerateArray());
        Assert.Single(i.RootElement.GetProperty("parejas").EnumerateArray());
        var mallasGuardadas = i.RootElement.GetProperty("grupos")[0].GetProperty("mallas");
        Assert.Equal(mallaOrigen, mallasGuardadas.GetProperty("origenes")[0].GetProperty("malla").GetProperty("id").GetGuid());
        Assert.False(mallasGuardadas.GetProperty("origenes")[0].GetProperty("malla").GetProperty("vigenteAlAnalizar").GetBoolean());
        Assert.Equal(2, mallasGuardadas.GetProperty("receptoras").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, i.RootElement.GetProperty("parejas")[0].GetProperty("mallas").ValueKind);
        Assert.Equal(historica, i.RootElement.GetProperty("parejas")[0].GetProperty("asignaturaAprobadaId").GetGuid());
        Assert.Equal(2, i.RootElement.GetProperty("grupos")[0].GetProperty("docentes").GetArrayLength());
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), i.RootElement.GetProperty("grupos")[0].GetProperty("configuracionVersionId").GetGuid());
        Assert.Equal("Homologación por agrupación — regla de análisis pendiente de validación institucional",
            i.RootElement.GetProperty("grupos")[0].GetProperty("leyenda").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(detalle + "/revisiones", revision)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync(detalle + "/analisis", null)).StatusCode);
        e.Solicitud = null; Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(detalle)).StatusCode);
    }
    private sealed class Receptoras(Guid r2) : IReceptorasAnalisisInterno
    { public Task<IReadOnlyList<ReceptoraAnalisis>> ListarAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<ReceptoraAnalisis>>([new(EscenarioInterno.ReceptoraId, "R1", "Receptora", 1), new(r2, "R2", "Otra", 2)]); }
}

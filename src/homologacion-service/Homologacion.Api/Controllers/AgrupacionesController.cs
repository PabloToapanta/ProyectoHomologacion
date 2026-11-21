using System.Text.Json;
using System.Text.Json.Serialization;
using Homologacion.Application.Agrupaciones;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Application.RevisionDocente;
using Homologacion.Domain.Agrupaciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Homologacion.Api.Controllers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ComposicionGrupoRequest(Guid[] Origenes, Guid[] Receptoras, AporteGrupoRequest[] Aportes)
{
    public ComposicionGrupo Aplicacion() => new(Origenes, Receptoras, Aportes?.Select(a => a is null ? null! : new AporteGrupo(a.AsignaturaAprobadaId, a.AsignaturaReceptoraId, a.HorasAportadas, a.CreditosAportados)).ToArray()!);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AporteGrupoRequest(Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, decimal? HorasAportadas, decimal? CreditosAportados);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RevisionGrupoRequest(string? Postura, string? Decision, string? Observacion);
public sealed record GrupoResponse(Guid GrupoId, Guid SolicitudId, string Version, bool Congelado, IReadOnlyList<Guid> Origenes,
    IReadOnlyList<Guid> Receptoras, IReadOnlyList<AporteGrupo> Aportes, AnalisisAgrupacionGuardado? UltimoAnalisis, string Leyenda)
{
    public static GrupoResponse Desde(DetalleGrupo d) => new(d.Grupo.GrupoId, d.Grupo.SolicitudId, GestionarAgrupaciones.ETag(d.Grupo),
        d.Grupo.Congelado, d.Grupo.Origenes, d.Grupo.Receptoras, d.Grupo.Aportes, d.UltimoAnalisis, GestionarAgrupaciones.Leyenda);
}
public sealed record AnalisisGrupoResponse(bool Guardado, Homologacion.Application.Equivalencias.ResultadoAgrupacion Evaluacion, GrupoResponse Detalle, IReadOnlyList<string> Advertencias);
/// <summary>HU-21 / Issue #75: agrupaciones con reservas por solicitud y versión fuerte del borrador.</summary>
[ApiController, Authorize(Roles = "Docente")]
[Route("api/homologaciones/solicitudes/{id:guid}/agrupaciones")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
[ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 409)]
[ProducesResponseType(typeof(ProblemDetails), 428), ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class AgrupacionesController(GestionarAgrupaciones grupos, RegistrarRevisionAgrupacion revisiones) : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    /// <summary>Ofrece orígenes, receptoras y celdas con revisión vigente; no permite aprobar correspondencias.</summary>
    [HttpGet("opciones"), ProducesResponseType(typeof(OpcionesAgrupacion), 200)]
    public Task<IActionResult> Opciones(Guid id, CancellationToken ct) => Responder(async _ => await grupos.OpcionesAsync(id, ct));
    /// <summary>Lista composición, ETag y último análisis de cada grupo.</summary>
    [HttpGet, ProducesResponseType(typeof(IReadOnlyList<GrupoResponse>), 200)]
    public Task<IActionResult> Listar(Guid id, CancellationToken ct) => Responder(async _ => (await grupos.ListarAsync(id, ct)).Select(GrupoResponse.Desde).ToArray());
    /// <summary>Consulta grupo y devuelve ETag fuerte, opaco, en cabecera y versión del cuerpo.</summary>
    [HttpGet("{grupoId:guid}"), ProducesResponseType(typeof(GrupoResponse), 200)]
    public Task<IActionResult> Obtener(Guid id, Guid grupoId, CancellationToken ct) => Responder(async _ => Version(await grupos.ObtenerAsync(id, grupoId, ct)));
    /// <summary>Crea un borrador N:M y reserva sus orígenes atómicamente. Para 1:1 use el flujo de parejas.</summary>
    [HttpPost, RequestSizeLimit(262144), ProducesResponseType(typeof(GrupoResponse), 201)]
    public Task<IActionResult> Crear(Guid id, ComposicionGrupoRequest input, CancellationToken ct) => Responder(async _ =>
    {
        var d = await grupos.CrearAsync(id, input.Aplicacion(), ct);
        Response.Headers.Location = $"/api/homologaciones/solicitudes/{id}/agrupaciones/{d.Grupo.GrupoId}";
        return Version(d);
    }, 201);
    /// <summary>Reemplaza solo un borrador con If-Match exacto. 428 sin cabecera; 409 obsoleta o congelada, sin escritura parcial.</summary>
    [HttpPut("{grupoId:guid}"), RequestSizeLimit(262144), ProducesResponseType(typeof(GrupoResponse), 200)]
    public Task<IActionResult> Reemplazar(Guid id, Guid grupoId, ComposicionGrupoRequest input, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct) => Responder(async _ =>
        Version(await grupos.ReemplazarAsync(id, grupoId, ifMatch, input.Aplicacion(), ct)));
    /// <summary>Elimina un borrador y libera sus reservas en una transacción; exige If-Match.</summary>
    [HttpDelete("{grupoId:guid}"), ProducesResponseType(204)]
    public Task<IActionResult> Eliminar(Guid id, Guid grupoId, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct) => Responder(async _ =>
    { await grupos.EliminarAsync(id, grupoId, ifMatch, ct); return new { }; }, 204);
    /// <summary>Evalúa la composición If-Match usando solo evidencia revisada/vigente del Catálogo.</summary>
    /// <remarks>Null en aportes: información incompleta y guardado=false sin congelar. Otro análisis guardado congela composición.
    /// Devuelve nueva versión ETag. No usa LLM ni convierte unidades. El informe cierra análisis y revisiones.</remarks>
    [HttpPost("{grupoId:guid}/analisis"), ProducesResponseType(typeof(AnalisisGrupoResponse), 200)]
    public Task<IActionResult> Analizar(Guid id, Guid grupoId, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct) => Responder(async _ =>
    { var r = await grupos.AnalizarAsync(id, grupoId, ifMatch, ct); Version(r.Detalle); return new AnalisisGrupoResponse(r.Guardado, r.Evaluacion, GrupoResponse.Desde(r.Detalle), r.Advertencias); });
    /// <summary>Lista las revisiones de los docentes del grupo autorizado.</summary>
    [HttpGet("{grupoId:guid}/revisiones"), ProducesResponseType(typeof(IReadOnlyList<RevisionAgrupacionRegistrada>), 200)]
    public Task<IActionResult> ListarRevisiones(Guid id, Guid grupoId, CancellationToken ct) => Responder(async _ =>
    { await grupos.ObtenerAsync(id, grupoId, ct); return (await revisiones.ListarAsync(id, ct)).Where(r => r.GrupoId == grupoId).ToArray(); });
    /// <summary>Guarda/corrige una revisión por grupo y docente del JWT; el informe generado impide cambios posteriores.</summary>
    [HttpPut("{grupoId:guid}/revisiones"), RequestSizeLimit(8192), ProducesResponseType(typeof(RevisionAgrupacionRegistrada), 200)]
    public Task<IActionResult> Revisar(Guid id, Guid grupoId, RevisionGrupoRequest input, CancellationToken ct) => Responder(async docente =>
    { await grupos.ObtenerAsync(id, grupoId, ct); return await revisiones.RegistrarAsync(id, grupoId, docente, User.FindFirst("name")?.Value ?? docente.ToString(), new(input.Postura, input.Decision, input.Observacion), ct); });
    private GrupoResponse Version(DetalleGrupo d) { Response.Headers.ETag = GestionarAgrupaciones.ETag(d.Grupo); return GrupoResponse.Desde(d); }
    private async Task<IActionResult> Responder(Func<Guid, Task<object>> accion, int status = 200)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var docente) || docente == Guid.Empty) return Forbid();
        try { var value = await accion(docente); return status == 204 ? NoContent() : new JsonResult(value, Json) { StatusCode = status }; }
        catch (SolicitudAnalisisNoEncontradaException e) { return Problem(statusCode: 404, title: e.Message); }
        catch (GrupoNoEncontradoException e) { return Problem(statusCode: 404, title: e.Message); }
        catch (GrupoInvalidoException e) { return Problem(statusCode: 400, title: e.Message); }
        catch (Homologacion.Application.Equivalencias.DatosEvaluacionInvalidosException e) { return Problem(statusCode: 400, title: e.Message); }
        catch (DatosAnalisisSolicitudException e) { return Problem(statusCode: 400, title: e.Message); }
        catch (ConflictoGrupoException e) { return Problem(statusCode: 409, title: e.Message); }
        catch (InformeYaGeneradoException e) { return Problem(statusCode: 409, title: e.Message); }
        catch (VersionGrupoRequeridaException e) { return Problem(statusCode: 428, title: e.Message); }
        catch (FuenteAnalisisNoDisponibleException e) { return Problem(statusCode: 503, title: e.Message); }
        catch (AlmacenAnalisisNoDisponibleException e) { return Problem(statusCode: 503, title: e.Message); }
    }
}

using Homologacion.Api.Contracts;
using Homologacion.Application.AnalisisInterno;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

[ApiController, Authorize(Roles = "Docente")]
[Route("api/homologaciones/solicitudes/{id:guid}/analisis-interno")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AnalisisInternoController(GestionarAnalisisInterno service, ISolicitudesAnalisisInterno solicitudes,
    IReceptorasAnalisisInterno receptoras) : ControllerBase
{
    /// <summary>Recupera los últimos análisis guardados por pareja, comprobando de nuevo la asignación.</summary>
    [HttpGet, ProducesResponseType(typeof(IReadOnlyList<AnalisisGuardadoResponse>), 200)]
    [ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(503)]
    public Task<IActionResult> Consultar(Guid id, CancellationToken ct) => Responder(async () =>
        (await service.ConsultarAsync(id, ct)).Select(x => new AnalisisGuardadoResponse(x.Fecha, AnalisisInternoResponse.Desde(x.Analisis))).ToArray());

    /// <summary>Ofrece las asignaturas receptoras de la carrera de la solicitud interna asignada.</summary>
    [HttpGet("opciones"), ProducesResponseType(typeof(IReadOnlyList<ReceptoraAnalisis>), 200)]
    [ProducesResponseType(400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(404), ProducesResponseType(503)]
    public Task<IActionResult> Opciones(Guid id, CancellationToken ct) => Responder(async () =>
    {
        var solicitud = await solicitudes.ObtenerSolicitudAsync(id, ct) ?? throw new SolicitudAnalisisNoEncontradaException();
        if (solicitud.Id != id) throw new FuenteAnalisisNoDisponibleException();
        if (solicitud.TipoCambio != "Interno") throw new DatosAnalisisSolicitudException("Esta operación solo admite solicitudes internas.");
        return (await receptoras.ListarAsync(solicitud.CarreraReceptoraId, ct)).OrderBy(x => x.Nivel).ThenBy(x => x.Codigo).ToArray();
    });
    /// <summary>Evalúa una asignatura registrada contra una receptora, con autorización del docente en Solicitudes.</summary>
    /// <remarks>El motor conserva porcentajes y sugerencia. Guarda el último intento por pareja. El resultado nulo con estado Incompleto/Fallido no es una recomendación exitosa. No evalúa mínimos globales sin datos.</remarks>
    [HttpPost, RequestSizeLimit(4096), ProducesResponseType(typeof(AnalisisInternoResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public Task<IActionResult> Analizar(Guid id, AnalisisInternoRequest input, CancellationToken ct) =>
        Responder(async () => AnalisisInternoResponse.Desde(await service.EjecutarAsync(id, input.AsignaturaAprobadaId, input.AsignaturaReceptoraId, ct)));

    private async Task<IActionResult> Responder(Func<Task<object>> accion)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var docente) || docente == Guid.Empty) return Forbid();
        try { return Ok(await accion()); }
        catch (SolicitudAnalisisNoEncontradaException error) { return Problem(statusCode: 404, title: error.Message); }
        catch (DatosAnalisisSolicitudException error) { return Problem(statusCode: 400, title: error.Message); }
        catch (FuenteAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }
}
public sealed record AnalisisGuardadoResponse(DateTimeOffset Fecha, AnalisisInternoResponse Analisis);

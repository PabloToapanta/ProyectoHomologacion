using Homologacion.Api.Contracts;
using Homologacion.Application.AnalisisExterno;
using Homologacion.Application.AnalisisInterno;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

/// <summary>HU-19 / Issue #43: análisis de cambio externo (otra universidad ecuatoriana, SC-01) y RN-05 externo.</summary>
[ApiController, Authorize(Roles = "Docente")]
[Route("api/homologaciones/solicitudes/{id:guid}")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AnalisisExternoController(GestionarAnalisisExterno service, IReceptorasAnalisisInterno receptoras) : ControllerBase
{
    /// <summary>Recupera los últimos análisis externos guardados por pareja, comprobando de nuevo la asignación.</summary>
    [HttpGet("analisis-externo"), ProducesResponseType(typeof(IReadOnlyList<AnalisisGuardadoResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public Task<IActionResult> Consultar(Guid id, CancellationToken ct) => Responder(async () =>
        (await service.ConsultarAsync(id, ct)).Select(x => new AnalisisGuardadoResponse(x.Fecha, AnalisisInternoResponse.Desde(x.Analisis))).ToArray());

    /// <summary>Ofrece las asignaturas receptoras de la carrera de la solicitud externa asignada.</summary>
    [HttpGet("analisis-externo/opciones"), ProducesResponseType(typeof(IReadOnlyList<ReceptoraAnalisis>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public Task<IActionResult> Opciones(Guid id, CancellationToken ct) => Responder(async () =>
        (await receptoras.ListarAsync(await service.CarreraReceptoraAsync(id, ct), ct)).OrderBy(x => x.Nivel).ThenBy(x => x.Codigo).ToArray());

    /// <summary>Evalúa una asignatura externa registrada contra una receptora FISEI y guarda el último intento por pareja.</summary>
    /// <remarks>Solo identificadores en el cuerpo. Mientras no se registren contenidos del programa externo (D2), el criterio académico es
    /// información incompleta, `resultado` es null y no se invoca al agente. Horas y créditos solo se comparan con unidad y sistema idénticos
    /// a los de la receptora, sin conversiones (D3). No evalúa apostilla ni instituciones de otro país (D1, SC-01). No es una aprobación oficial.</remarks>
    [HttpPost("analisis-externo"), RequestSizeLimit(4096), ProducesResponseType(typeof(AnalisisInternoResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public Task<IActionResult> Analizar(Guid id, AnalisisInternoRequest input, CancellationToken ct) =>
        Responder(async () => AnalisisInternoResponse.Desde(await service.EjecutarAsync(id, input.AsignaturaAprobadaId, input.AsignaturaReceptoraId, ct)));

    /// <summary>Calcula RN-05 externo (≥ 2 períodos, ≥ 2 asignaturas homologables, sin cambios previos) con los análisis guardados.</summary>
    /// <remarks>`estado`: Cumple, NoCumple o Incompleto. Datos no declarados o análisis pendientes se listan en `faltantes`; solo un dato
    /// declarado bajo el mínimo es incumplimiento. No verifica gratuidad ni autoriza el cambio de carrera.</remarks>
    [HttpGet("requisitos-externos"), ProducesResponseType(typeof(ResultadoRequisitosExternos), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public Task<IActionResult> Requisitos(Guid id, CancellationToken ct) => Responder(async () => await service.RequisitosAsync(id, ct));

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

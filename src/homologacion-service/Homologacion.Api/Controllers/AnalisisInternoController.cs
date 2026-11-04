using Homologacion.Api.Contracts;
using Homologacion.Application.AnalisisInterno;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

[ApiController, Authorize(Roles = "Docente")]
[Route("api/homologaciones/solicitudes/{id:guid}/analisis-interno")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AnalisisInternoController(AnalizarSolicitudInterna service) : ControllerBase
{
    /// <summary>Evalúa una asignatura registrada contra una receptora, con autorización del docente en Solicitudes.</summary>
    /// <remarks>El motor conserva porcentajes y sugerencia. El resultado nulo con estado Incompleto/Fallido no es una recomendación exitosa. No persiste resultados ni evalúa mínimos globales sin datos.</remarks>
    [HttpPost, RequestSizeLimit(4096), ProducesResponseType(typeof(AnalisisInternoResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> Analizar(Guid id, AnalisisInternoRequest input, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var docente) || docente == Guid.Empty) return Forbid();
        try { return Ok(AnalisisInternoResponse.Desde(await service.EjecutarAsync(id, input.AsignaturaAprobadaId, input.AsignaturaReceptoraId, ct))); }
        catch (SolicitudAnalisisNoEncontradaException error) { return Problem(statusCode: 404, title: error.Message); }
        catch (DatosAnalisisSolicitudException error) { return Problem(statusCode: 400, title: error.Message); }
        catch (FuenteAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }
}

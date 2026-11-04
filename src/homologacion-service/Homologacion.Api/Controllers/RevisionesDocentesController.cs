using System.Text.Json.Serialization;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.RevisionDocente;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

/// <summary>Postura: DeAcuerdo o Ajuste. Decisión: Homologable o NoHomologable. La identidad del docente sale del token.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RevisionDocenteRequest(Guid AsignaturaAprobadaId, Guid AsignaturaReceptoraId, string? Postura, string? Decision, string? Observacion);

/// <summary>HU-31: análisis propio de cada docente asignado por pareja, junto al del agente (HU-18/HU-19).</summary>
[ApiController, Authorize(Roles = "Docente")]
[Route("api/homologaciones/solicitudes/{id:guid}/revisiones-docentes")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RevisionesDocentesController(RegistrarRevisionDocente service) : ControllerBase
{
    /// <summary>Lista los análisis registrados por los docentes asignados a la solicitud.</summary>
    [HttpGet, ProducesResponseType(typeof(IReadOnlyList<RevisionDocenteRegistrada>), 200)]
    [ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public Task<IActionResult> Listar(Guid id, CancellationToken ct) => Responder(async _ => (object)await service.ListarAsync(id, ct));

    /// <summary>Registra o corrige el análisis propio del docente autenticado para una pareja.</summary>
    /// <remarks>«DeAcuerdo» solo con un análisis completo del agente y guarda su sugerencia; «Ajuste» exige decisión y observación
    /// y se admite aunque el agente no haya completado su análisis. Guarda el estado y la sugerencia del agente vigentes.
    /// Corregir reemplaza el registro propio. No es una resolución oficial.</remarks>
    [HttpPut, RequestSizeLimit(8192), ProducesResponseType(typeof(RevisionDocenteRegistrada), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public Task<IActionResult> Registrar(Guid id, RevisionDocenteRequest input, CancellationToken ct) => Responder(async docente =>
        await service.RegistrarAsync(id, docente, User.FindFirst("name")?.Value ?? docente.ToString(),
            new(input.AsignaturaAprobadaId, input.AsignaturaReceptoraId, input.Postura, input.Decision, input.Observacion), ct));

    private async Task<IActionResult> Responder(Func<Guid, Task<object>> accion)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var docente) || docente == Guid.Empty) return Forbid();
        try { return Ok(await accion(docente)); }
        catch (SolicitudAnalisisNoEncontradaException error) { return Problem(statusCode: 404, title: error.Message); }
        catch (DatosAnalisisSolicitudException error) { return Problem(statusCode: 400, title: error.Message); }
        catch (Homologacion.Application.Informe.InformeYaGeneradoException error) { return Problem(statusCode: 409, title: error.Message); }
        catch (FuenteAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }
}

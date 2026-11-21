using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solicitudes.Application;

namespace Solicitudes.Api;

/// <summary>Lectura interna mínima de HU-18, limitada al docente asignado por HU-29.</summary>
[ApiController, Authorize(Roles = "Docente")]
[Route("api/solicitudes/{id:guid}/contexto-analisis-interno")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ContextoAnalisisInternoController(ISolicitudRepository repository) : ControllerBase
{
    /// <summary>Obtiene aprobaciones y referencias académicas sin datos personales del estudiante.</summary>
    [HttpGet, ProducesResponseType(typeof(SolicitudContextoInterno), 200)]
    [ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var docente) || docente == Guid.Empty) return Forbid();
        var resultado = await ConsultarContextoAnalisisInterno.EjecutarAsync(repository, id, docente, ct);
        return resultado is null
            ? NotFound(new ProblemDetails { Status = 404, Title = "Solicitud no encontrada entre sus asignaciones." })
            : Ok(resultado);
    }
}

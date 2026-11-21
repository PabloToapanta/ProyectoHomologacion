using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solicitudes.Application;

namespace Solicitudes.Api;

/// <summary>Lectura interna de HU-19 para Homologación, limitada al docente asignado por HU-29. No se publica en el Gateway.</summary>
[ApiController, Authorize(Roles = "Docente")]
[Route("api/solicitudes/{id:guid}/contexto-analisis-externo")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ContextoAnalisisExternoController(ISolicitudRepository repository) : ControllerBase
{
    /// <summary>Obtiene la universidad de origen, los datos de RN-05 y las asignaturas externas sin datos personales del estudiante.</summary>
    [HttpGet, ProducesResponseType(typeof(SolicitudContextoExterno), 200)]
    [ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var docente) || docente == Guid.Empty) return Forbid();
        var resultado = await ConsultarContextoAnalisisExterno.EjecutarAsync(repository, id, docente, ct);
        return resultado is null
            ? NotFound(new ProblemDetails { Status = 404, Title = "Solicitud no encontrada entre sus asignaciones." })
            : Ok(resultado);
    }
}

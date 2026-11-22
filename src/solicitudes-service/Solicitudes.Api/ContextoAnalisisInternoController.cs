using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solicitudes.Application;

namespace Solicitudes.Api;

/// <summary>Lectura interna mínima para análisis docente y revisión de informes por Coordinación.</summary>
[ApiController, Authorize(Roles = "Docente,Coordinador")]
[Route("api/solicitudes/{id:guid}/contexto-analisis-interno")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ContextoAnalisisInternoController(ISolicitudRepository repository) : ControllerBase
{
    /// <summary>Obtiene el contexto mínimo sin datos personales. Docentes: solo asignados. Coordinadores: solicitudes FISEI existentes.</summary>
    [HttpGet, ProducesResponseType(typeof(SolicitudContextoInterno), 200)]
    [ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var usuario) || usuario == Guid.Empty) return Forbid();
        var resultado = User.IsInRole("Coordinador")
            ? await ConsultarContextoAnalisisInterno.EjecutarParaCoordinadorAsync(repository, id, ct)
            : await ConsultarContextoAnalisisInterno.EjecutarAsync(repository, id, usuario, ct);
        return resultado is null
            ? NotFound(new ProblemDetails { Status = 404, Title = "Solicitud no encontrada entre sus asignaciones." })
            : Ok(resultado);
    }
}

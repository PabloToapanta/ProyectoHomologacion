using Catalogo.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(typeof(ProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
public sealed class ComparacionCatalogoController(ComparacionCatalogoService service) : ControllerBase
{
    /// <summary>Consulta el plan y unidades de medida registrados una sola vez en catálogo.</summary>
    [HttpGet("api/asignaturas/{id:guid}/perfil-comparacion")]
    [ProducesResponseType(typeof(PerfilComparacionInput), 200)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Perfil(Guid id, CancellationToken ct)
    { var p = await service.PerfilAsync(id, ct); return p is null ? NoContent() : Ok(p); }
    /// <summary>Registra o actualiza el perfil académico; no acredita aprobación de estudiantes.</summary>
    [HttpPut("api/asignaturas/{id:guid}/perfil-comparacion")]
    [ProducesResponseType(typeof(PerfilComparacionInput), 200)]
    public async Task<IActionResult> GuardarPerfil(Guid id, PerfilComparacionInput input, CancellationToken ct) => Ok(await service.GuardarPerfilAsync(id, input, ct));
    /// <summary>Consulta correspondencias explícitas y si siguen vigentes respecto de ambos programas.</summary>
    [HttpGet("api/asignaturas/{id:guid}/correspondencias/{receptoraId:guid}")]
    [ProducesResponseType(typeof(RevisionComparacionDto), 200)]
    public async Task<IActionResult> Revision(Guid id, Guid receptoraId, CancellationToken ct) => Ok(await service.RevisionAsync(id, receptoraId, ct));
    /// <summary>Guarda una revisión direccional con evidencia. Las listas vacías acreditan una revisión sin correspondencias.</summary>
    [HttpPut("api/asignaturas/{id:guid}/correspondencias/{receptoraId:guid}")]
    [ProducesResponseType(typeof(RevisionComparacionDto), 200)]
    public async Task<IActionResult> GuardarRevision(Guid id, Guid receptoraId, RevisionComparacionInput input, CancellationToken ct) => Ok(await service.GuardarRevisionAsync(id, receptoraId, input, ct));
}

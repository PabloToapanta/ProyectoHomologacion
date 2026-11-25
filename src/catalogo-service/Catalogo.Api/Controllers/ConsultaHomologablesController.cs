using Catalogo.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Controllers;

/// <summary>HU-09: lectura interna para Homologación (JWT reenviado y clave interna). No se publica en el Gateway.</summary>
[ApiController, Authorize(Roles = "Docente,Coordinador"), Route("api/consulta-homologables/carreras")]
[ProducesResponseType(401), ProducesResponseType(403)]
public sealed class ConsultaHomologablesController(ConsultaHomologablesService service) : ControllerBase
{
    /// <summary>Carreras FISEI disponibles para la consulta.</summary>
    [HttpGet, ProducesResponseType(typeof(IReadOnlyList<CarreraDto>), 200)]
    public async Task<IActionResult> Carreras(CancellationToken ct) => Ok(await service.CarrerasAsync(ct));

    /// <summary>Pares con revisión registrada entre dos carreras, con el contexto académico de cada asignatura.</summary>
    /// <remarks>El origen incluye todas las versiones de malla; la receptora, solo la vigente. No crea correspondencias.</remarks>
    [HttpGet("{origenId:guid}/{receptoraId:guid}/contexto"), ProducesResponseType(typeof(ContextoConsultaHomologables), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Contexto(Guid origenId, Guid receptoraId, CancellationToken ct) =>
        Ok(await service.ContextoAsync(origenId, receptoraId, ct));
}

using Catalogo.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Controllers;

/// <summary>Contrato de solo lectura para HU-18. No concede permisos CRUD al docente.</summary>
[ApiController, Authorize(Roles = "Docente")]
[Route("api/asignaturas/{id:guid}/analisis-interno")]
[ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 404)]
public sealed class ContextoAnalisisInternoController(AsignaturaService asignaturas, CarreraService carreras,
    IUnidadRepository unidades, ComparacionCatalogoService comparaciones) : ControllerBase
{
    /// <summary>Lista las receptoras de la carrera ya verificada en la solicitud.</summary>
    [HttpGet("/api/carreras/{id:guid}/analisis-interno/asignaturas"), ProducesResponseType(typeof(IReadOnlyList<AsignaturaDto>), 200)]
    public async Task<IActionResult> Receptoras(Guid id, CancellationToken ct) => Ok(await asignaturas.ListarAsync(id, ct));
    /// <summary>Lee el programa actual, sus unidades de medida y textos académicos.</summary>
    [HttpGet("contexto"), ProducesResponseType(typeof(AsignaturaContextoInterno), 200)]
    public async Task<IActionResult> Contexto(Guid id, CancellationToken ct)
    {
        var asignatura = await asignaturas.ObtenerAsync(id, ct);
        var carrera = await carreras.ObtenerAsync(asignatura.CarreraId, ct);
        var perfil = await comparaciones.PerfilAsync(id, ct);
        var programa = await unidades.ListarAsync(id, ct);
        return Ok(AsignaturaContextoInterno.Desde(asignatura, carrera, perfil, programa.Select(UnidadDto.From).ToArray()));
    }
    /// <summary>Lee correspondencias explícitas y su vigencia, sin crear coincidencias.</summary>
    [HttpGet("correspondencias/{receptoraId:guid}"), ProducesResponseType(typeof(RevisionComparacionDto), 200)]
    public async Task<IActionResult> Correspondencias(Guid id, Guid receptoraId, CancellationToken ct) =>
        Ok(await comparaciones.RevisionAsync(id, receptoraId, ct));
}

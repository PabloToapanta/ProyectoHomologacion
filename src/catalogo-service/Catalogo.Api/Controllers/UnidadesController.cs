using System.ComponentModel.DataAnnotations;
using Catalogo.Application;
using Catalogo.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Controllers;

public sealed record UnidadRequest(
    [Required, Range(1, int.MaxValue)] int? Numero,
    [Required, StringLength(200)] string Nombre,
    [Required, Range(1, int.MaxValue)] int? Horas,
    [Required, StringLength(4000)] string ResultadoAprendizaje,
    [MinLength(1), MaxLength(200)] string[]? Contenidos = null,
    [MinLength(1), MaxLength(100)] TematicaDatos[]? Tematicas = null);

[ApiController]
[Authorize(Roles = "Administrador")]
public sealed class UnidadesController(UnidadService service) : ControllerBase
{
    /// <summary>Corrige una unidad y sus contenidos conservando la identidad de la unidad.</summary>
    [HttpPut("api/asignaturas/{id:guid}/unidades/{unidadId:guid}")]
    [ProducesResponseType(typeof(UnidadDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<UnidadDto>> Actualizar(Guid id, Guid unidadId, UnidadRequest request, CancellationToken ct) =>
        Ok(await service.ActualizarAsync(id, unidadId, new UnidadInput(request.Numero!.Value, request.Nombre, request.Horas!.Value, request.ResultadoAprendizaje, request.Contenidos, request.Tematicas), ct));

    /// <summary>Registra una unidad con sus contenidos ordenados y resultado de aprendizaje (HU-06 / Issue #4).</summary>
    [HttpPost("api/asignaturas/{id:guid}/unidades")]
    [ProducesResponseType(typeof(UnidadDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<UnidadDto>> Crear(Guid id, UnidadRequest request, CancellationToken ct)
    {
        var result = await service.CrearAsync(id, new UnidadInput(request.Numero!.Value, request.Nombre, request.Horas!.Value, request.ResultadoAprendizaje, request.Contenidos, request.Tematicas), ct);
        return Created($"/api/asignaturas/{id}", result);
    }
}

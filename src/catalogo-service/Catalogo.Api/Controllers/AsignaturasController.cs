using System.ComponentModel.DataAnnotations;
using Catalogo.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Controllers;

public sealed record AsignaturaRequest(
    [Required] Guid? CarreraId,
    [Required, StringLength(50)] string Codigo,
    [Required, StringLength(200)] string Nombre,
    [Required] int? Nivel,
    [Required] decimal? Creditos,
    [Required] int? Horas)
{
    public AsignaturaInput ToInput() => new(CarreraId!.Value, Codigo, Nombre, Nivel!.Value, Creditos!.Value, Horas!.Value);
}

[ApiController]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
public sealed class AsignaturasController(AsignaturaService service) : ControllerBase
{
    /// <summary>Registra una asignatura vinculada a una carrera existente.</summary>
    /// <remarks>Nivel y horas: enteros positivos. Créditos: decimal positivo (hasta dos decimales). Código único por carrera, ignorando caso/espacios y normalizando Unicode.</remarks>
    [HttpPost("api/asignaturas")]
    [ProducesResponseType(typeof(AsignaturaDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<AsignaturaDto>> Crear(AsignaturaRequest request, CancellationToken ct)
    {
        var result = await service.CrearAsync(request.ToInput(), ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.Id }, result);
    }

    /// <summary>Consulta los datos académicos de una asignatura por UUID.</summary>
    [HttpGet("api/asignaturas/{id:guid}")]
    [ProducesResponseType(typeof(AsignaturaDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<AsignaturaDto>> Obtener(Guid id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    /// <summary>Lista las asignaturas de una carrera, ordenadas por nivel y código.</summary>
    /// <remarks>Devuelve [] para una carrera sin asignaturas y 404 cuando la carrera no existe.</remarks>
    [HttpGet("api/carreras/{id:guid}/asignaturas")]
    [ProducesResponseType(typeof(IReadOnlyList<AsignaturaDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<IReadOnlyList<AsignaturaDto>>> Listar(Guid id, CancellationToken ct) => Ok(await service.ListarAsync(id, ct));
}

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
    public Guid? MallaId { get; init; }
    public AsignaturaInput ToInput() => new(CarreraId!.Value, Codigo, Nombre, Nivel!.Value, Creditos!.Value, Horas!.Value) { MallaId = MallaId };
}

[ApiController]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
public sealed class AsignaturasController(AsignaturaService service, IUnidadRepository unidades) : ControllerBase
{
    /// <summary>Registra una asignatura en una versión de malla de su carrera; sin mallaId usa la vigente.</summary>
    /// <remarks>Nivel y horas: enteros positivos. Créditos: decimal positivo (hasta dos decimales). Código único por versión de malla, ignorando caso/espacios y normalizando Unicode.</remarks>
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
    [ProducesResponseType(typeof(AsignaturaDetalleDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct)
    {
        var a = await service.ObtenerAsync(id, ct);
        var detalle = new AsignaturaDetalleDto(a.Id, a.CarreraId, a.Codigo, a.Nombre, a.Nivel, a.Creditos, a.Horas,
            (await unidades.ListarAsync(id, ct)).Select(UnidadDto.From).ToList());
        return Ok(detalle with { MallaId = a.MallaId, Malla = a.Malla });
    }

    /// <summary>Elimina una asignatura registrada por error. Solo Administrador.</summary>
    /// <remarks>Elimina también sus unidades, temáticas, contenidos, perfil de comparación, programa analítico y
    /// correspondencias revisadas con otras asignaturas. El código queda libre. Las solicitudes y los análisis ya
    /// guardados conservan sus datos registrados.</remarks>
    [HttpDelete("api/asignaturas/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await service.EliminarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Lista las asignaturas de una carrera, ordenadas por nivel y código.</summary>
    /// <remarks>Filtro mallaId opcional. Sin filtro conserva todas las versiones. Devuelve [] para una carrera sin asignaturas y 404 cuando la carrera no existe.</remarks>
    [HttpGet("api/carreras/{id:guid}/asignaturas")]
    [ProducesResponseType(typeof(IReadOnlyList<AsignaturaDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<IReadOnlyList<AsignaturaDto>>> Listar(Guid id, CancellationToken ct, [FromQuery] Guid? mallaId = null) => Ok(await service.ListarAsync(id, ct, mallaId));
}

public record AsignaturaDetalleDto(Guid Id, Guid CarreraId, string Codigo, string Nombre, int Nivel, decimal Creditos, int Horas, IReadOnlyList<UnidadDto> Unidades)
{
    public Guid MallaId { get; init; }
    public MallaDto? Malla { get; init; }
}

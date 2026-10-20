using System.ComponentModel.DataAnnotations;
using Catalogo.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Controllers;

public sealed record CarreraRequest(
    [Required, StringLength(200)] string Nombre,
    [Required, StringLength(200)] string Facultad,
    [Required] bool? Vigencia,
    [StringLength(100)] string? Modalidad = null)
{
    public CarreraInput ToInput() => new(Nombre, Facultad, Vigencia!.Value, Modalidad);
}

[ApiController]
[Route("api/carreras")]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
public sealed class CarrerasController(CarreraService service) : ControllerBase
{
    /// <summary>Lista las carreras vigentes y no vigentes, ordenadas por nombre y facultad.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CarreraDto>), 200)]
    public async Task<ActionResult<IReadOnlyList<CarreraDto>>> Listar(CancellationToken ct) => Ok(await service.ListarAsync(ct));

    /// <summary>Consulta una carrera por su UUID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CarreraDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<CarreraDto>> Obtener(Guid id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    /// <summary>Crea una carrera receptora de FISEI. Nombre: 1–200 caracteres; modalidad opcional: hasta 100.</summary>
    /// <remarks>Facultad es obligatoria y solo admite FISEI, ignorando mayúsculas y espacios externos; otra facultad devuelve 400. Vigencia es obligatoria (true/false). Nombre único normalizado en FISEI.</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CarreraDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<CarreraDto>> Crear(CarreraRequest request, CancellationToken ct)
    {
        var dto = await service.CrearAsync(request.ToInput(), ct);
        return CreatedAtAction(nameof(Obtener), new { id = dto.Id }, dto);
    }

    /// <summary>Reemplaza los datos editables de una carrera FISEI manteniendo su UUID.</summary>
    /// <remarks>Facultad obligatoria: FISEI. Otra facultad devuelve 400 sin modificar datos. No se convierten automáticamente registros anteriores.</remarks>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CarreraDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<ActionResult<CarreraDto>> Actualizar(Guid id, CarreraRequest request, CancellationToken ct) =>
        Ok(await service.ActualizarAsync(id, request.ToInput(), ct));

    /// <summary>Elimina una carrera sin asignaturas. Devuelve 409 si existen relaciones.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await service.EliminarAsync(id, ct);
        return NoContent();
    }
}

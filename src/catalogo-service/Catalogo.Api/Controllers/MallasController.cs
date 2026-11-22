using System.ComponentModel.DataAnnotations;
using Catalogo.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalogo.Api.Controllers;

public sealed record MallaRequest([Required, StringLength(100)] string Nombre, [Required] bool? Vigente);
public sealed record ActivarMallaRequest([Required] bool? Vigente);

/// <summary>HU-07: versiones de malla por carrera. Solo el Administrador las gestiona.</summary>
[ApiController, Authorize(Roles = "Administrador")]
[ProducesResponseType(typeof(ProblemDetails), 401), ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
public sealed class MallasController(MallaService service) : ControllerBase
{
    /// <summary>Lista todas las versiones sin ocultar las no vigentes.</summary>
    [HttpGet("api/carreras/{carreraId:guid}/mallas")]
    [ProducesResponseType(typeof(IReadOnlyList<MallaDto>), 200)]
    public async Task<IActionResult> Listar(Guid carreraId, CancellationToken ct) => Ok(await service.ListarAsync(carreraId, ct));

    /// <summary>Consulta una versión de malla por identificador.</summary>
    [HttpGet("api/mallas/{id:guid}")]
    [ProducesResponseType(typeof(MallaDto), 200)]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    /// <summary>Crea una versión; si es vigente, desactiva la anterior atómicamente.</summary>
    [HttpPost("api/carreras/{carreraId:guid}/mallas")]
    [ProducesResponseType(typeof(MallaDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Crear(Guid carreraId, MallaRequest request, CancellationToken ct)
    {
        var malla = await service.CrearAsync(carreraId, request.Nombre, request.Vigente!.Value, ct);
        return CreatedAtAction(nameof(Obtener), new { id = malla.Id }, malla);
    }

    /// <summary>Activa la versión seleccionada sin mover asignaturas ni borrar las anteriores.</summary>
    [HttpPut("api/mallas/{id:guid}/vigencia")]
    [ProducesResponseType(typeof(MallaDto), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Activar(Guid id, ActivarMallaRequest request, CancellationToken ct)
    {
        if (request.Vigente != true) return Problem(statusCode: 400, detail: "Para cambiar la vigente, activa la versión de destino con vigente: true.");
        return Ok(await service.ActivarAsync(id, ct));
    }
}

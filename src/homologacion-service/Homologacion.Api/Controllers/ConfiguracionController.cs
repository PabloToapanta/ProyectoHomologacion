using Homologacion.Api.Contracts;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Configuracion;
using Homologacion.Domain.Configuracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

[ApiController]
[Authorize(Roles = "Coordinador,Administrador")]
[Route("api/homologaciones/configuracion")]
public sealed class ConfiguracionController(GestionarConfiguracionHomologacion gestion) : ControllerBase
{
    /// <summary>Consulta la versión vigente de parámetros académicos de HU-03.</summary>
    /// <remarks>Coordinador y Administrador (HU-38). El ETag identifica la versión que debe enviarse en If-Match al actualizar.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ParametrosHomologacion), 200)]
    [ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<ActionResult<ParametrosHomologacion>> Obtener(CancellationToken ct)
    {
        try
        {
            var actual = await gestion.ObtenerAsync(ct);
            Response.Headers.ETag = ETag(actual.VersionId);
            return Ok(actual);
        }
        catch (AlmacenAnalisisNoDisponibleException) { return NoDisponible(); }
    }

    /// <summary>Guarda una nueva versión completa con auditoría y concurrencia If-Match.</summary>
    /// <remarks>Exige un ETag fuerte de GET, motivo y respaldo. Si la versión cambió, responde 409 sin guardar.</remarks>
    [HttpPut]
    [ProducesResponseType(typeof(ParametrosHomologacion), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409), ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<ActionResult<ParametrosHomologacion>> Actualizar([FromBody] ConfiguracionRequest request, CancellationToken ct)
    {
        var raw = Request.Headers.IfMatch.ToString();
        if (raw.Length != 38 || raw[0] != '"' || raw[^1] != '"' || !Guid.TryParse(raw[1..^1], out var version))
            return Problem(statusCode: 400, title: "If-Match inválido", detail: "Consulte la configuración y envíe su ETag fuerte en If-Match.");
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var actor) || actor == Guid.Empty) return Forbid();
        try
        {
            var nueva = await gestion.ActualizarAsync(version, request.ToApplication(), actor, ct);
            Response.Headers.ETag = ETag(nueva.VersionId);
            return Ok(nueva);
        }
        catch (ConfiguracionInvalidaException error)
        { return Problem(statusCode: 400, title: "Configuración inválida", detail: error.Message); }
        catch (ConfiguracionObsoletaException)
        { return Problem(statusCode: 409, title: "Configuración modificada", detail: "Consulte la configuración vigente antes de intentar otro cambio."); }
        catch (AlmacenAnalisisNoDisponibleException) { return NoDisponible(); }
    }

    /// <summary>Lista versiones de configuración con valores anteriores y nuevos.</summary>
    /// <remarks>Historial de solo lectura. Cursor es el identificador devuelto como cursorSiguiente.</remarks>
    [HttpGet("historial")]
    [ProducesResponseType(typeof(PaginaHistorialConfiguracion), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<ActionResult<PaginaHistorialConfiguracion>> Historial([FromQuery] Guid? cursor,
        [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        try { return Ok(await gestion.HistorialAsync(cursor, limit, ct)); }
        catch (ConfiguracionInvalidaException error)
        { return Problem(statusCode: 400, title: "Paginación inválida", detail: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException) { return NoDisponible(); }
    }

    private static string ETag(Guid version) => $"\"{version:D}\"";
    private ObjectResult NoDisponible() => Problem(statusCode: 503, title: "Configuración no disponible",
        detail: "No fue posible acceder a la configuración de homologación.");
}

using Homologacion.Api.Contracts;
using Homologacion.Application.Antiguedad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/homologaciones")]
public sealed class AntiguedadController : ControllerBase
{
    /// <summary>Valida aprobación y antigüedad máxima de diez años (HU-15 / T11 / Issue #6).</summary>
    /// <remarks>Fechas de calendario ISO 8601. Evaluación explícita para resultados reproducibles.
    /// No calcula equivalencia ni emite una decisión oficial de homologación.</remarks>
    [HttpPost("validar-antiguedad")]
    [ProducesResponseType(typeof(ValidarAntiguedadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<ValidarAntiguedadResponse> Validar([FromBody] ValidarAntiguedadRequest request,
        [FromServices] ValidarAntiguedad casoDeUso)
    {
        try { return Ok(ValidarAntiguedadResponse.FromDomain(casoDeUso.Ejecutar(request.ToApplication()))); }
        catch (ArgumentException error)
        { return Problem(statusCode: 400, title: "Datos de antigüedad inválidos", detail: error.Message); }
    }
}

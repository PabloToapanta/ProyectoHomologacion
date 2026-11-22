using Homologacion.Api.Contracts;
using Homologacion.Application.Antiguedad;
using Homologacion.Application.Configuracion;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/homologaciones")]
public sealed class AntiguedadController(IParametrosHomologacionProvider parametrosProvider) : ControllerBase
{
    /// <summary>Valida aprobación y antigüedad máxima de diez años (HU-15 / T11 / Issue #6).</summary>
    /// <remarks>Fechas de calendario ISO 8601. Evaluación explícita para resultados reproducibles.
    /// No calcula equivalencia ni emite una decisión oficial de homologación.</remarks>
    [HttpPost("validar-antiguedad")]
    [ProducesResponseType(typeof(ValidarAntiguedadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ValidarAntiguedadResponse>> Validar([FromBody] ValidarAntiguedadRequest request,
        [FromServices] ValidarAntiguedad casoDeUso, CancellationToken cancellationToken)
    {
        var hoyEcuador = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).DateTime);
        if (request.FechaEvaluacion > hoyEcuador)
            return Problem(statusCode: 400, title: "Datos de antigüedad inválidos",
                detail: "La fecha de evaluación no puede ser posterior a la fecha actual de Ecuador.");
        try
        {
            var parametros = await parametrosProvider.ObtenerVigentesAsync(cancellationToken);
            return Ok(ValidarAntiguedadResponse.FromDomain(casoDeUso.Ejecutar(request.ToApplication(), parametros)));
        }
        catch (ArgumentException error)
        { return Problem(statusCode: 400, title: "Datos de antigüedad inválidos", detail: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException)
        { return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Configuración no disponible", detail: "No fue posible obtener la configuración vigente."); }
    }
}

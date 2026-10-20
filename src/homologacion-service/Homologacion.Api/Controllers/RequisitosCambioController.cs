using Homologacion.Api.Contracts;
using Homologacion.Application.RequisitosCambio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/homologaciones")]
public sealed class RequisitosCambioController : ControllerBase
{
    /// <summary>Valida mínimos de cambio interno o externo (HU-16 / T12 / Issue #7).</summary>
    /// <remarks>Solo evalúa períodos, cantidad de asignaturas homologables y cambios previos.
    /// No verifica gratuidad ni apostilla y no autoriza el cambio de carrera.
    /// La cantidad de homologables debe provenir de evaluaciones completas verificadas.</remarks>
    [HttpPost("validar-requisitos")]
    [ProducesResponseType(typeof(ValidarRequisitosResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<ValidarRequisitosResponse> Validar([FromBody] ValidarRequisitosRequest request,
        [FromServices] ValidarRequisitosCambio casoDeUso)
    {
        try
        {
            var resultado = casoDeUso.Ejecutar(request.ToApplication());
            return Ok(new ValidarRequisitosResponse(resultado.Cumple, resultado.Incumplimientos));
        }
        catch (ArgumentException error)
        { return Problem(statusCode: 400, title: "Datos de requisitos inválidos", detail: error.Message); }
    }
}

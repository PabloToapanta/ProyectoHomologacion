using Homologacion.Api.Contracts;
using Homologacion.Application.RequisitosCambio;
using Homologacion.Application.Configuracion;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/homologaciones")]
public sealed class RequisitosCambioController(IParametrosHomologacionProvider parametrosProvider) : ControllerBase
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
    public async Task<ActionResult<ValidarRequisitosResponse>> Validar([FromBody] ValidarRequisitosRequest request,
        [FromServices] ValidarRequisitosCambio casoDeUso, CancellationToken cancellationToken)
    {
        try
        {
            var parametros = await parametrosProvider.ObtenerVigentesAsync(cancellationToken);
            var resultado = casoDeUso.Ejecutar(request.ToApplication(), parametros);
            return Ok(new ValidarRequisitosResponse(resultado.Cumple, resultado.Incumplimientos));
        }
        catch (ArgumentException error)
        { return Problem(statusCode: 400, title: "Datos de requisitos inválidos", detail: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException)
        { return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Configuración no disponible", detail: "No fue posible obtener la configuración vigente."); }
    }
}

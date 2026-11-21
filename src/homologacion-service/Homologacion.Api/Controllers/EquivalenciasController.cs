using Homologacion.Api.Contracts;
using Homologacion.Application.Equivalencias;
using Homologacion.Application.Configuracion;
using Homologacion.Application.AnalisisInterno;
using Homologacion.Infrastructure.AnalisisInterno;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/homologaciones")]
public sealed class EquivalenciasController(EvaluarEquivalencia evaluar, IParametrosHomologacionProvider parametrosProvider) : ControllerBase
{
    /// <summary>Evalúa compatibilidad académica, horaria y de créditos (HU-14 / T9–T10 / Issue #5).</summary>
    /// <remarks>La cobertura combina contenidos y resultados receptores con igual peso por elemento y evidencia explícita.
    /// Para varios orígenes no se agregan horas ni créditos sin una regla aprobada: información incompleta.
    /// Recibe constancia de aprobación; no determina aprobación, antigüedad ni requisitos de cambio. No persiste resultados.</remarks>
    [HttpPost("evaluar")]
    [RequestSizeLimit(1_048_576)]
    [ProducesResponseType(typeof(EvaluarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EvaluarResponse>> Evaluar([FromBody] EvaluarRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var parametros = await parametrosProvider.ObtenerVigentesAsync(cancellationToken);
            return Ok(EvaluarResponse.FromApplication(evaluar.Ejecutar(request.ToApplication(), parametros)));
        }
        catch (DatosEvaluacionInvalidosException error)
        { return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Datos de evaluación inválidos", detail: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException)
        { return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Configuración no disponible", detail: "No fue posible obtener la configuración vigente."); }
    }
}

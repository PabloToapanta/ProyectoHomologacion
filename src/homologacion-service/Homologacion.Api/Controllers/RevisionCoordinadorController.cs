using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Homologacion.Domain.Auditoria;
using Homologacion.Domain.Finalizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

/// <summary>HU-33: el coordinador revisa el informe remitido, decide las parejas «Sin acuerdo» y finaliza la solicitud.</summary>
/// <remarks>Cualquier Coordinador revisa las solicitudes FISEI (confirmación de la PO del 15/11/2026). La decisión en «Sin acuerdo» es
/// propia y motivada; no elige automáticamente entre los docentes. Regla del flujo del proyecto (20/11/2026), pendiente de confirmación
/// institucional. El informe no es una resolución oficial.</remarks>
[ApiController, Authorize(Roles = "Coordinador")]
[Route("api/homologaciones/solicitudes/{id:guid}/informe")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RevisionCoordinadorController(RevisionCoordinadorInforme service) : ControllerBase
{
    /// <summary>Informe remitido con todas las parejas, el análisis del agente y ambos docentes, más las decisiones del coordinador.</summary>
    /// <remarks>404 si el informe no existe o aún no ha sido remitido. `decisionesPendientes` cuenta las parejas «Sin acuerdo» sin decisión;
    /// `finalizacion` es null mientras la solicitud no se finaliza.</remarks>
    [HttpGet("revision"), ProducesResponseType(typeof(RevisionInformeCoordinador), 200)]
    [ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct)
    {
        try { return Ok(await service.ConsultarAsync(id, ct)); }
        catch (InformeNoRemitidoException error) { return Problem(statusCode: 404, title: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }

    /// <summary>Registra o corrige la decisión del coordinador en una pareja «Sin acuerdo», hasta finalizar.</summary>
    /// <remarks>`resultado`: Convalidar o NoConvalidar; `justificacion` obligatoria (hasta 2000 caracteres). Guarda el coordinador del
    /// token y la fecha. 400 si el informe no fue remitido, la pareja no pertenece al informe o no es «Sin acuerdo», o falta la
    /// justificación; 409 si la solicitud ya fue finalizada.</remarks>
    [HttpPut("decisiones"), RequestSizeLimit(16_384), ProducesResponseType(typeof(DecisionCoordinador), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 409), ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> Decidir(Guid id, DecisionCoordinadorInput input, CancellationToken ct)
    {
        if (CoordinadorId() is not Guid coordinador) return Forbid();
        try { return Ok(await service.DecidirAsync(id, input, coordinador, User.FindFirst("name")?.Value ?? coordinador.ToString(), ct)); }
        catch (DecisionCoordinadorInvalidaException error) { return Problem(statusCode: 400, title: error.Message); }
        catch (DatosAnalisisSolicitudException error) { return Problem(statusCode: 400, title: error.Message); }
        catch (SolicitudYaFinalizadaException error) { return Problem(statusCode: 409, title: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }

    /// <summary>Finaliza la solicitud: registra la fecha y el coordinador y la deja «Finalizada»; una sola vez.</summary>
    /// <remarks>Inserta el evento «Finalizado» del historial (HU-25) y pide a Solicitudes el estado «Finalizada». 400 sin informe
    /// remitido o si falta la decisión en alguna pareja «Sin acuerdo»; 409 si ya fue finalizada o el estado de la solicitud no lo
    /// permite; 503 si Solicitudes o la base no responden, sin finalización registrada. Después no admite decisiones (409).</remarks>
    [HttpPost("finalizacion"), ProducesResponseType(typeof(FinalizacionInforme), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 409), ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> Finalizar(Guid id, CancellationToken ct)
    {
        if (CoordinadorId() is not Guid coordinador) return Forbid();
        try { return Ok(await service.FinalizarAsync(id, coordinador, User.FindFirst("name")?.Value ?? coordinador.ToString(), ct)); }
        catch (DatosAnalisisSolicitudException error) { return Problem(statusCode: 400, title: error.Message); }
        catch (SolicitudAnalisisNoEncontradaException error) { return Problem(statusCode: 404, title: error.Message); }
        catch (SolicitudYaFinalizadaException error) { return Problem(statusCode: 409, title: error.Message); }
        catch (EstadoSolicitudNoPermiteFinalizarException error) { return Problem(statusCode: 409, title: error.Message); }
        catch (EventoInformeInvalidoException error) { return Problem(statusCode: 409, title: error.Message); }
        catch (FuenteAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }

    private Guid? CoordinadorId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) && id != Guid.Empty ? id : null;
}

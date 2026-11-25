using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.ConsultaHomologables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

/// <summary>HU-09: consulta de materias homologables entre carreras de la FISEI.</summary>
[ApiController, Authorize(Roles = "Docente,Coordinador"), Route("api/homologaciones/consulta-homologables")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class ConsultaHomologablesController(ConsultarHomologables service) : ControllerBase
{
    /// <summary>Carreras FISEI disponibles para la consulta.</summary>
    [HttpGet("carreras"), ProducesResponseType(typeof(IReadOnlyList<CarreraConsulta>), 200)]
    public Task<IActionResult> Carreras(CancellationToken ct) => Responder(async () => await service.CarrerasAsync(ct));

    /// <summary>Aplica el motor determinista a los pares con correspondencias revisadas entre dos carreras FISEI.</summary>
    /// <remarks>Informativa: no crea ni modifica correspondencias, no guarda resultados y no llama al agente. Los pares con revisión
    /// desactualizada o datos faltantes aparecen como InformacionIncompleta. Origen: todas las versiones de malla; receptora: la vigente.</remarks>
    [HttpGet, ProducesResponseType(typeof(ResultadoConsultaHomologables), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(typeof(ProblemDetails), 404)]
    public Task<IActionResult> Consultar([FromQuery] Guid origen, [FromQuery] Guid receptora, CancellationToken ct) =>
        Responder(async () => await service.EjecutarAsync(origen, receptora, ct));

    private async Task<IActionResult> Responder(Func<Task<object>> accion)
    {
        try { return Ok(await accion()); }
        catch (DatosAnalisisSolicitudException error) { return Problem(statusCode: 400, title: error.Message); }
        catch (CarreraConsultaNoEncontradaException error) { return Problem(statusCode: 404, title: error.Message); }
        catch (FuenteAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }
}

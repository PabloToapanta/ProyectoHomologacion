using Homologacion.Application.AnalisisInterno;
using Homologacion.Application.Informe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

/// <summary>Informe de homologación: lo generan los docentes asignados; docentes asignados y Coordinación pueden consultarlo.</summary>
[ApiController, Authorize(Roles = "Docente,Coordinador")]
[Route("api/homologaciones/solicitudes/{id:guid}/informe")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InformeController(GenerarInformeHomologacion service) : ControllerBase
{
    /// <summary>Consulta un informe generado. Docentes: solo solicitudes asignadas. Coordinadores: solicitudes FISEI existentes.</summary>
    [HttpGet, ProducesResponseType(typeof(InformeHomologacion), 200)]
    [ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct)
    {
        if (UsuarioId() is null) return Forbid();
        try
        {
            var informe = await service.ConsultarAsync(id, ct);
            return informe is null ? Problem(statusCode: 404, title: "El informe de esta solicitud aún no se ha generado.") : Ok(informe);
        }
        catch (SolicitudAnalisisNoEncontradaException error) { return Problem(statusCode: 404, title: error.Message); }
        catch (FuenteAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }

    /// <summary>Genera el informe fijo de la solicitud; cualquiera de los dos docentes asignados puede hacerlo.</summary>
    /// <remarks>Cada pareja con análisis docente debe tener el de ambos docentes. Sugerencia final: Convalidar, NoConvalidar o SinAcuerdo
    /// (decisiones distintas: decide el coordinador). Una segunda generación da 409 y, desde ese momento, los análisis docentes quedan
    /// cerrados. No remite al coordinador (HU-32) ni es una resolución oficial.</remarks>
    [HttpPost, Authorize(Roles = "Docente"), ProducesResponseType(typeof(InformeHomologacion), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403)]
    [ProducesResponseType(typeof(ProblemDetails), 404), ProducesResponseType(typeof(ProblemDetails), 409), ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> Generar(Guid id, CancellationToken ct)
    {
        if (UsuarioId() is not Guid docente) return Forbid();
        try
        {
            var informe = await service.GenerarAsync(id, docente, User.FindFirst("name")?.Value ?? docente.ToString(), ct);
            return Created($"/api/homologaciones/solicitudes/{id}/informe", informe);
        }
        catch (SolicitudAnalisisNoEncontradaException error) { return Problem(statusCode: 404, title: error.Message); }
        catch (DatosAnalisisSolicitudException error) { return Problem(statusCode: 400, title: error.Message); }
        catch (InformeYaGeneradoException error) { return Problem(statusCode: 409, title: error.Message); }
        catch (Homologacion.Domain.Auditoria.EventoInformeInvalidoException error) { return Problem(statusCode: 409, title: error.Message); }
        catch (FuenteAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }

    private Guid? UsuarioId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) && id != Guid.Empty ? id : null;
}

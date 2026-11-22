using Homologacion.Application.AnalisisInterno;
using Homologacion.Api.Contracts;
using Homologacion.Application.Informe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homologacion.Api.Controllers;

/// <summary>HU-25: historial y auditoría del informe de una solicitud. Solo lectura: no existen operaciones para modificar ni eliminar eventos.</summary>
[ApiController, Authorize(Roles = "Coordinador")]
[Route("api/homologaciones/solicitudes/{id:guid}/informe/historial")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HistorialInformeController(ConsultarHistorialInforme service) : ControllerBase
{
    /// <summary>Eventos del informe en orden cronológico, con fecha, usuario y rol.</summary>
    /// <remarks>Una solicitud sin informe generado devuelve una lista vacía. «Remitido» lo registra la remisión (HU-32)
    /// y «Finalizado», la finalización del coordinador (HU-33).</remarks>
    [HttpGet, ProducesResponseType(typeof(IReadOnlyList<EventoInformeResponse>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400), ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 503)]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct)
    {
        try
        {
            var eventos = await service.EjecutarAsync(id, ct);
            return Ok(eventos.Select(e => new EventoInformeResponse(e.Id, e.Tipo.ToString(), e.Fecha, e.UsuarioId, e.Usuario, e.Rol)).ToArray());
        }
        catch (DatosAnalisisSolicitudException error) { return Problem(statusCode: 400, title: error.Message); }
        catch (AlmacenAnalisisNoDisponibleException error) { return Problem(statusCode: 503, title: error.Message); }
    }
}

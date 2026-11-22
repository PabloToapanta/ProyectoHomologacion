using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solicitudes.Application;
namespace Solicitudes.Api;
/// <summary>HU-33 / Issue #64: cambio de estado a «Finalizada». Ruta interna para Homologación; no se publica en el Gateway.</summary>
[ApiController,Route("api/solicitudes/{id:guid}/finalizacion"),Authorize(Roles="Coordinador")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class FinalizacionSolicitudController(SolicitudService service) : ControllerBase {
 /// <summary>Marca la solicitud como «Finalizada» cuando Homologación registra la finalización del coordinador.</summary>
 /// <remarks>La invoca Homologación con el token del coordinador y la clave interna, después de comprobar el informe remitido y las
 /// decisiones en «Sin acuerdo». Solo rol Coordinador. Transición única «Remitida» → «Finalizada»; repetirla responde 204 sin cambios
 /// para admitir reintentos; desde otro estado, 409; si no existe, 404. Una solicitud finalizada no admite registrar asignaturas (409).</remarks>
 [HttpPost,ProducesResponseType(204),ProducesResponseType(401),ProducesResponseType(403),ProducesResponseType(typeof(ProblemDetails),404),ProducesResponseType(typeof(ProblemDetails),409)]
 public async Task<IActionResult> Finalizar(Guid id,CancellationToken ct) {
  await service.MarcarFinalizada(id,ct);
  return NoContent();
 }
}
